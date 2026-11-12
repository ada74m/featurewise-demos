using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Featurewise;
using Microsoft.Extensions.DependencyInjection;

namespace Demos.Functions;

// Invokes the workers that a demo's events trigger. DynamoDB's stream delivers each event committed to
// the outbox; this turns it into its worker's link and invokes it, as a caller working in the sandbox the
// event was raised in. The link records the invocation that raised the event as its cause, so a story's
// trace runs on through the outbox. Delivery is at least once, so the workers are idempotent. Each demo's
// QueueWorker function holds one, given the demo's workers, facilities, and the worker each event triggers.
public class QueueWorker
{
    private readonly Features _features;
    private readonly Func<string, string, string?, Link> _linkFor;

    // linkFor gives the worker an event triggers, from the event's type name and its body as JSON, caused
    // by what raised the event.
    public QueueWorker(
        string service, Func<Features, Features> registerFeatures, Action<IServiceCollection> registerDependencies,
        Func<string, string, string?, Link> linkFor)
    {
        _linkFor = linkFor;
        _features = registerFeatures(new Features(service));
        _features.RegisterDependencies(registerDependencies).Build();
    }

    // An event's body, as the event it is.
    public static T Read<T>(string body) =>
        JsonSerializer.Deserialize<T>(body) ?? throw new JsonException("the event's body is null");

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
                var link = _linkFor(image["Event"].S, image["Body"].S, image.TryGetValue("CausedBy", out var cause) ? cause.S : null);
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
}
