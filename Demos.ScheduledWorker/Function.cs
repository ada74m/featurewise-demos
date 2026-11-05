using System.Text.Json;
using Amazon.Lambda.Core;
using Demos.LostProperty;
using Featurewise;
using Microsoft.Extensions.DependencyInjection;

namespace Demos.ScheduledWorker;

// Runs the demos' scheduled jobs. Each EventBridge schedule's input names the job as a link, so one
// function serves every schedule. A job runs once in every sandbox that's in use, as a caller working in
// that sandbox, so each visitor's demos move on by themselves while the features know nothing of sandboxes.
public class Function
{
    private readonly Features _features = new(Service.Id);
    private readonly JsonSerializerOptions _json;
    private readonly Func<CancellationToken, Task<IReadOnlyList<string>>> _sandboxes;

    public Function() : this(Aws.Registrations.Register, LiveSandboxes())
    {
    }

    // The sandboxes in use, which each run of a job visits in turn.
    public Function(Action<IServiceCollection> registerDependencies, Func<CancellationToken, Task<IReadOnlyList<string>>> sandboxes)
    {
        _sandboxes = sandboxes;
        _features
            .RegisterFeature<HandleCharityCollectionDue, AnySystemCaller<HandleCharityCollectionDue.Args>>()
            .RegisterDependencies(registerDependencies)
            .Build();

        _json = _features.ConfigureJson(new JsonSerializerOptions());
    }

    private static Func<CancellationToken, Task<IReadOnlyList<string>>> LiveSandboxes()
    {
        var sandboxes = new Aws.DynamoSandboxes(new Amazon.DynamoDBv2.AmazonDynamoDBClient(), TimeProvider.System, Aws.Registrations.Settings());
        return sandboxes.Live;
    }

    // Reads the input itself, as a stream, because Lambda's serializer can't be given options that know
    // the registered features, so a link to any other feature can't be read. A sandbox whose run fails doesn't stop the others; the function fails
    // afterwards, so the failure is seen.
    public async Task Handle(Stream input, ILambdaContext context)
    {
        var scheduled = await JsonSerializer.DeserializeAsync<ScheduledInvocation>(input, _json);
        if (scheduled?.Link is null || string.IsNullOrWhiteSpace(scheduled.Schedule))
            throw new JsonException("the input must have a Schedule and a Link");

        using var timeout = new CancellationTokenSource(context.RemainingTime - TimeSpan.FromSeconds(1));
        var failures = new List<Exception>();
        foreach (var sandbox in await _sandboxes(timeout.Token))
        {
            try
            {
                await _features.Invoke(
                    scheduled.Link,
                    SystemCaller.For(scheduled.ScheduleName()).In(sandbox),
                    scheduled.Execution is { } execution ? $"{execution}/{sandbox}" : null,
                    timeout.Token);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                failures.Add(e);
            }
        }

        if (failures.Count > 0)
            throw new AggregateException($"the job failed in {failures.Count} sandboxes", failures);
    }
}
