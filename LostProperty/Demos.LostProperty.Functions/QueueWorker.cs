using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.Serialization.SystemTextJson;
using Featurewise;
using Microsoft.Extensions.DependencyInjection;

namespace Demos.LostProperty.Functions;

// The office's queue worker: the workers its events trigger.
public class QueueWorker
{
    private readonly Demos.Functions.QueueWorker _worker;

    public QueueWorker() : this(Aws.Registrations.Register)
    {
    }

    public QueueWorker(Action<IServiceCollection> registerDependencies)
    {
        _worker = new Demos.Functions.QueueWorker(Service.Id, features => features
            .RegisterFeature<HandleItemHandedIn, AnySystemCaller<HandleItemHandedIn.Args>>(),
            registerDependencies,
            LinkFor);
    }

    // The worker each event triggers, by the event's type name, caused by what raised the event.
    private static Link LinkFor(string @event, string body, string? causedBy) =>
        @event switch
        {
            nameof(ItemHandedIn) => new HandleItemHandedIn.Args(Demos.Functions.QueueWorker.Read<ItemHandedIn>(body).ItemId) { CausedBy = causedBy },
            _ => throw new JsonException($"no worker handles {@event}")
        };

    public Task<StreamsEventResponse> Handle(DynamoDBEvent streamEvent, ILambdaContext context) =>
        _worker.Handle(streamEvent, context);
}
