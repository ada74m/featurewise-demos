using Amazon.Lambda.Core;
using Demos.LostProperty;
using Featurewise;
using Microsoft.Extensions.DependencyInjection;

namespace Demos.ScheduledWorker;

// The demos' scheduled worker: their scheduled jobs, over the shared ScheduledWorker.
public class Function
{
    private readonly Functions.ScheduledWorker _worker;

    public Function() : this(Aws.Registrations.Register, LiveSandboxes())
    {
    }

    public Function(Action<IServiceCollection> registerDependencies, Func<CancellationToken, Task<IReadOnlyList<string>>> sandboxes)
    {
        _worker = new Functions.ScheduledWorker(Service.Id, features => features
            .RegisterFeature<HandleCharityCollectionDue, AnySystemCaller<HandleCharityCollectionDue.Args>>(),
            registerDependencies,
            sandboxes);
    }

    private static Func<CancellationToken, Task<IReadOnlyList<string>>> LiveSandboxes()
    {
        var sandboxes = new Aws.DynamoSandboxes(new Amazon.DynamoDBv2.AmazonDynamoDBClient(), TimeProvider.System, Aws.Registrations.Settings());
        return sandboxes.Live;
    }

    public Task Handle(Stream input, ILambdaContext context) => _worker.Handle(input, context);
}
