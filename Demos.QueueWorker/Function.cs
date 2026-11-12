using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.Serialization.SystemTextJson;
using Demos.LostProperty;
using Featurewise;
using Microsoft.Extensions.DependencyInjection;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Demos.QueueWorker;

// The demos' queue worker: the workers their events trigger, over the shared QueueWorker.
public class Function
{
    private readonly Functions.QueueWorker _worker;

    public Function() : this(Aws.Registrations.Register)
    {
    }

    public Function(Action<IServiceCollection> registerDependencies)
    {
        _worker = new Functions.QueueWorker(Service.Id, features => features
            .RegisterFeature<HandleItemHandedIn, AnySystemCaller<HandleItemHandedIn.Args>>(),
            registerDependencies,
            LinkFor);
    }

    // The worker each event triggers, by the event's type name, caused by what raised the event.
    private static Link LinkFor(string @event, string body, string? causedBy) =>
        @event switch
        {
            nameof(ItemHandedIn) => new HandleItemHandedIn.Args(Functions.QueueWorker.Read<ItemHandedIn>(body).ItemId) { CausedBy = causedBy },
            _ => throw new JsonException($"no worker handles {@event}")
        };

    public Task<StreamsEventResponse> Handle(DynamoDBEvent streamEvent, ILambdaContext context) =>
        _worker.Handle(streamEvent, context);
}
