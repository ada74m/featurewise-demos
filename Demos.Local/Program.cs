using System.Text;
using System.Text.Json.Nodes;
using Amazon.Lambda.APIGatewayEvents;
using Demos.Local;
using Demos.LostProperty.Functions;
using Microsoft.Extensions.FileProviders;
using BoxOfficeBackend = Demos.UiBackend.Function;
using LostProperty = Demos.LostProperty;

// Runs every demo in one process, each a service over facilities in memory of its own, so they can be
// used without deploying them. Each request becomes the event API Gateway would send the deployed
// function, and goes to the same class, so the code under test locally is the code that's deployed:
//
//   POST /api/<service>/{feature}        a service's UI backend, as the UI shell reaches it through CloudFront
//   GET  /api/<service>/{feature}?link=  the same, for queries
//   POST /jobs                           runs the lost property office's job whose link is the body, as a
//                                        schedule would
//
// The services are lost-property and, until the box office becomes a service of its own, demos. It also
// serves the UI, as CloudFront will in AWS, all from one origin, so there's no CORS:
//
//   GET  /services.json                  the shell's configuration, with each demo's description
//   GET  /modules/<service>/...          each service's module, from its dist folder
//   GET  anything else                   the shell, from a sibling checkout of featurewise-ui, ../ui/dist
//
// Events committed to the outbox go to the queue worker as soon as they arrive, as DynamoDB's stream
// would deliver them, and the charity collects every minute rather than every day, so its effect shows.
// Callers sign in with a bearer token that isn't checked; see UnsignedJwt. Without a sandbox claim, as the
// shell's dev users have none, everyone shares the sandbox "local".
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Where the built shell and modules are; the setting ShellFiles can change the shell's.
var root = builder.Environment.ContentRootPath;
var shellDirectory = Path.GetFullPath(builder.Configuration["ShellFiles"] ?? Path.Combine(root, "../../ui/dist"));
var modules = new Dictionary<string, string>
{
    [LostProperty.Service.Id] = Path.GetFullPath(Path.Combine(root, "../LostProperty/Demos.LostProperty.Ui/dist")),
    [Demos.Service.Id] = Path.GetFullPath(Path.Combine(root, "../Demos.Ui/dist"))
};

foreach (var (service, directory) in modules.Where(module => Directory.Exists(module.Value)))
    app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(directory), RequestPath = $"/modules/{service}" });
if (Directory.Exists(shellDirectory))
    app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(shellDirectory) });

// The demos, with their text and personas, each signed in with an unsigned token, as each describes them.
var demos = new JsonArray(JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(root, "../LostProperty/demo.json"))));
foreach (var demo in JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(root, "../demos.json")))!.AsArray())
    demos.Add(demo!.DeepClone());

// Each service has a store and an outbox of its own, as each has a table of its own in AWS.
var lostPropertyStore = new InMemoryStore();
var outbox = new InMemoryOutbox();
var registerLostProperty = LostProperty.Local.Registrations.Over(lostPropertyStore, outbox);
var lostPropertyBackend = new UiBackend(registerLostProperty);
var scheduledWorker = new ScheduledWorker(registerLostProperty, _ => Task.FromResult(lostPropertyStore.Sandboxes));
var queueWorker = new QueueWorker(registerLostProperty);
var boxOfficeBackend = new BoxOfficeBackend(Registrations.Over(new InMemoryStore(), new InMemoryOutbox()));

app.MapMethods($"/api/{LostProperty.Service.Id}/{{feature}}", ["POST", "GET"], async (HttpContext http, string feature) =>
{
    var request = await ToEvent(http.Request, $"{http.Request.Method} /{{feature}}", new() { ["feature"] = feature });
    await Write(http.Response, await lostPropertyBackend.Handle(request, new LocalLambdaContext()));
});

app.MapMethods($"/api/{Demos.Service.Id}/{{feature}}", ["POST", "GET"], async (HttpContext http, string feature) =>
{
    var request = await ToEvent(http.Request, $"{http.Request.Method} /{{feature}}", new() { ["feature"] = feature });
    await Write(http.Response, await boxOfficeBackend.Handle(request, new LocalLambdaContext()));
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
    @default = $"/{LostProperty.Service.Id}/unclaimed-items",
    services = modules.Keys.ToDictionary(service => service, service => (object)new { module = $"/modules/{service}/index.js", bff = $"/api/{service}" }),
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
            await RunJob(new JsonObject { ["$feature"] = $"{LostProperty.Service.Id}/HandleCharityCollectionDue" });
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
