using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.Serialization.SystemTextJson;
using Demos.LostProperty;
using Featurewise;
using Microsoft.Extensions.DependencyInjection;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Demos.QueueWorker;

// Invokes the workers that the demos' events trigger. DynamoDB's stream delivers each event committed to
// the outbox; this turns it into its worker's link and invokes it, as a caller working in the sandbox the
// event was raised in. The link records the invocation that raised the event as its cause, so a story's
// trace runs on through the outbox. Delivery is at least once, so the workers are idempotent.
public class Function
{
    private readonly Features _features = new(Service.Id);

    public Function(Action<IServiceCollection> registerDependencies)
    {
        _features
            .RegisterFeature<HandleItemHandedIn, AnySystemCaller<HandleItemHandedIn.Args>>()
            .RegisterDependencies(registerDependencies)
            .Build();
    }

    // The worker each event triggers, by the event's type name, caused by what raised the event.
    private static Link LinkFor(string @event, string body, string? causedBy) =>
        @event switch
        {
            nameof(ItemHandedIn) => new HandleItemHandedIn.Args(Read<ItemHandedIn>(body).ItemId) { CausedBy = causedBy },
            _ => throw new JsonException($"no worker handles {@event}")
        };

    // A stream's records must be handled in order, so the first that fails, and everything after it, is
    // reported back for the stream to deliver again. The event source mapping must have
    // ReportBatchItemFailures turned on.
    public async Task<StreamsEventResponse> Handle(DynamoDBEvent streamEvent, ILambdaContext context)
    {
        using var timeout = new CancellationTokenSource(context.RemainingTime - TimeSpan.FromSeconds(1));
        foreach (var record in streamEvent.Records ?? [])
        {
            if (record.EventName != "INSERT")
                continue;

            try
            {
                var image = record.Dynamodb.NewImage;
                var link = LinkFor(image["Event"].S, image["Body"].S, image.TryGetValue("CausedBy", out var cause) ? cause.S : null);
                var caller = SystemCaller.For("events").In(image["Sandbox"].S);
                await _features.Invoke(link, caller, record.EventID, timeout.Token);
            }
            catch (Exception e)
            {
                // Features logs failures inside an invocation; this also covers events that never got that
                // far, such as unreadable ones.
                context.Logger.LogError($"Event {record.EventID} failed: {e.Message}");
                return new StreamsEventResponse
                {
                    BatchItemFailures = [new StreamsEventResponse.BatchItemFailure { ItemIdentifier = record.Dynamodb.SequenceNumber }]
                };
            }
        }

        return new StreamsEventResponse { BatchItemFailures = [] };
    }

    private static T Read<T>(string body) =>
        JsonSerializer.Deserialize<T>(body) ?? throw new JsonException("the event's body is null");
}
