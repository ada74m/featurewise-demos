using Amazon.Lambda.Core;
using Featurewise;
using Microsoft.Extensions.DependencyInjection;

namespace Demos.LostProperty.Functions;

// The office's scheduled worker: its scheduled jobs.
public class ScheduledWorker
{
    private readonly Demos.Functions.ScheduledWorker _worker;

    public ScheduledWorker() : this(Aws.Registrations.Register, LiveSandboxes())
    {
    }

    public ScheduledWorker(Action<IServiceCollection> registerDependencies, Func<CancellationToken, Task<IReadOnlyList<string>>> sandboxes)
    {
        _worker = new Demos.Functions.ScheduledWorker(Service.Id, features => features
            .RegisterFeature<HandleCharityCollectionDue, AnySystemCaller<HandleCharityCollectionDue.Args>>(),
            registerDependencies,
            sandboxes);
    }

    private static Func<CancellationToken, Task<IReadOnlyList<string>>> LiveSandboxes()
    {
        var sandboxes = new Demos.Aws.DynamoSandboxes(new Amazon.DynamoDBv2.AmazonDynamoDBClient(), TimeProvider.System, Aws.Registrations.Settings());
        return sandboxes.Live;
    }

    public Task Handle(Stream input, ILambdaContext context) => _worker.Handle(input, context);
}
