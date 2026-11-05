using System.Text;
using System.Text.Json.Nodes;
using Amazon.Lambda.APIGatewayEvents;
using Demos;
using Demos.Local;
using Microsoft.Extensions.FileProviders;
using QueueWorker = Demos.QueueWorker.Function;
using ScheduledWorker = Demos.ScheduledWorker.Function;
using UiBackend = Demos.UiBackend.Function;

// Runs every demo in one process, over facilities in memory, so they can be used without deploying them.
// Each request becomes the event API Gateway would send the deployed function, and goes to the same
// Function class, so the code under test locally is the code that's deployed:
//
//   POST /api/demos/{feature}          the UI backend, as the UI shell will reach it through CloudFront
//   GET  /api/demos/{feature}?link=    the UI backend, for queries
//   POST /jobs                         runs the job whose link is the body, as a schedule would
//
// It also serves the UI, as CloudFront will in AWS, all from one origin, so there's no CORS:
//
//   GET  /services.json                the shell's configuration, with the demos in ../demos.json
//   GET  /modules/demos/...            the demos' module, built in Demos.Ui/dist
//   GET  anything else                 the shell, from a sibling checkout of featurewise-ui, ../ui/dist
//
// Events committed to the outbox go to the queue worker as soon as they arrive, as DynamoDB's stream
// would deliver them, and the charity collects every minute rather than every day, so its effect shows.
// Callers sign in with a bearer token that isn't checked; see UnsignedJwt. Without a sandbox claim, as the
// shell's dev users have none, everyone shares the sandbox "local".
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Where the built shell and module are, which the settings ShellFiles and ModuleFiles can change.
var shellDirectory = Path.GetFullPath(builder.Configuration["ShellFiles"] ?? Path.Combine(builder.Environment.ContentRootPath, "../../ui/dist"));
var moduleDirectory = Path.GetFullPath(builder.Configuration["ModuleFiles"] ?? Path.Combine(builder.Environment.ContentRootPath, "../Demos.Ui/dist"));

if (Directory.Exists(moduleDirectory))
    app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(moduleDirectory), RequestPath = "/modules/demos" });
if (Directory.Exists(shellDirectory))
    app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(shellDirectory) });

// The demos, with their text and personas, each signed in with an unsigned token, as demos.json lists them.
var demos = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(builder.Environment.ContentRootPath, "../demos.json")));

var store = new InMemoryStore();
var outbox = new InMemoryOutbox();
var register = Registrations.Over(store, outbox);
var uiBackend = new UiBackend(register);
var scheduledWorker = new ScheduledWorker(register, _ => Task.FromResult(store.Sandboxes));
var queueWorker = new QueueWorker(register);

app.MapMethods("/api/demos/{feature}", ["POST", "GET"], async (HttpContext http, string feature) =>
{
    var request = await ToEvent(http.Request, $"{http.Request.Method} /{{feature}}", new() { ["feature"] = feature });
    await Write(http.Response, await uiBackend.Handle(request, new LocalLambdaContext()));
});

app.MapPost("/jobs", async (HttpContext http) =>
{
    if (await JsonNode.ParseAsync(http.Request.Body) is not JsonObject link)
        return Results.BadRequest("the body must be a job's link");

    await RunJob(link);
    return Results.NoContent();
});

app.MapGet("/services.json", () => Results.Json(new
{
    @default = "/demos/lost-property/unclaimed-items",
    services = new Dictionary<string, object> { [Service.Id] = new { module = "/modules/demos/index.js", bff = "/api/demos" } },
    demos
}));

// Any other page is an address for the shell, which reads the URL itself.
app.MapFallback(async (HttpContext http) =>
{
    var index = Path.Combine(shellDirectory, "index.html");
    if (http.Request.Method != "GET" || !File.Exists(index))
    {
        http.Response.StatusCode = StatusCodes.Status404NotFound;
        await http.Response.WriteAsync(File.Exists(index) ? "" : $"The shell isn't built: run npm run build in {Path.GetDirectoryName(shellDirectory)}.");
        return;
    }
    http.Response.ContentType = "text/html";
    await http.Response.SendFileAsync(index);
});

var stopping = app.Lifetime.ApplicationStopping;
_ = Task.Run(() => RelayEvents(stopping));
_ = Task.Run(() => CollectForCharity(stopping));

app.Run();

// As DynamoDB's stream delivers the outbox's new records to the queue worker. Whatever the worker reports
// as failed is delivered again a second later.
async Task RelayEvents(CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        try
        {
            await outbox.WaitForMore(cancellationToken);
            var records = outbox.Take();
            if (records.Count == 0)
                continue;

            var response = await queueWorker.Handle(OutboxRecord.ToStreamEvent(records), new LocalLambdaContext());
            if (response.BatchItemFailures is [{ ItemIdentifier: var failed }, ..])
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                foreach (var record in records.SkipWhile(record => record.Id != failed))
                    outbox.Add(record);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
    }
}

// As the daily schedule runs the charity collection, but every minute.
async Task CollectForCharity(CancellationToken cancellationToken)
{
    using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
    try
    {
        while (await timer.WaitForNextTickAsync(cancellationToken))
            await RunJob(new JsonObject { ["$feature"] = $"{Service.Id}/HandleCharityCollectionDue" });
    }
    catch (OperationCanceledException)
    {
    }
}

// As EventBridge Scheduler invokes the scheduled worker, from a schedule named local.
async Task RunJob(JsonObject link)
{
    var input = new JsonObject { ["Schedule"] = "local", ["Link"] = link };
    await scheduledWorker.Handle(new MemoryStream(Encoding.UTF8.GetBytes(input.ToJsonString())), new LocalLambdaContext());
}

static async Task<APIGatewayHttpApiV2ProxyRequest> ToEvent(
    HttpRequest http, string routeKey, Dictionary<string, string> pathParameters)
{
    var body = await new StreamReader(http.Body).ReadToEndAsync();
    var claims = UnsignedJwt.Claims(http.Headers.Authorization);
    return new APIGatewayHttpApiV2ProxyRequest
    {
        RouteKey = routeKey,
        RawPath = http.Path,
        PathParameters = pathParameters,
        // API Gateway joins a parameter's repeated values with commas.
        QueryStringParameters = http.Query.ToDictionary(parameter => parameter.Key, parameter => parameter.Value.ToString()),
        // HTTP APIs give header names in lower case.
        Headers = http.Headers.ToDictionary(header => header.Key.ToLowerInvariant(), header => header.Value.ToString()),
        Body = body.Length == 0 ? null : body,
        RequestContext = new APIGatewayHttpApiV2ProxyRequest.ProxyRequestContext
        {
            Http = new APIGatewayHttpApiV2ProxyRequest.HttpDescription { Method = http.Method },
            Authorizer = claims is null
                ? null
                : new APIGatewayHttpApiV2ProxyRequest.AuthorizerDescription
                {
                    Jwt = new APIGatewayHttpApiV2ProxyRequest.AuthorizerDescription.JwtDescription { Claims = claims }
                }
        }
    };
}

static async Task Write(HttpResponse http, APIGatewayHttpApiV2ProxyResponse response)
{
    http.StatusCode = response.StatusCode;
    foreach (var (name, value) in response.Headers ?? new Dictionary<string, string>())
        http.Headers[name] = value;
    if (response.Body is not null)
        await http.WriteAsync(response.Body);
}

// Lets the tests start the host.
public partial class Program;
