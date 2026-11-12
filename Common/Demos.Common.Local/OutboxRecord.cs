using Amazon.Lambda.DynamoDBEvents;

namespace Demos.Local;

// An event as the outbox keeps it: the sandbox it was raised in, its type's name, its JSON, and the
// invocation that raised it, so that the worker's invocation is linked to its cause.
public record OutboxRecord(string Id, string Sandbox, string Event, string Body, string? CausedBy = null)
{
    // The record as DynamoDB's stream would deliver it to the queue worker, once it's inserted.
    public DynamoDBEvent.DynamodbStreamRecord ToStreamRecord() =>
        new()
        {
            EventID = Id,
            EventName = "INSERT",
            Dynamodb = new DynamoDBEvent.StreamRecord
            {
                SequenceNumber = Id,
                NewImage = Image()
            }
        };

    private Dictionary<string, DynamoDBEvent.AttributeValue> Image()
    {
        var image = new Dictionary<string, DynamoDBEvent.AttributeValue>
        {
            ["Sandbox"] = new() { S = Sandbox },
            ["Event"] = new() { S = Event },
            ["Body"] = new() { S = Body }
        };
        if (CausedBy is not null)
            image["CausedBy"] = new() { S = CausedBy };
        return image;
    }

    public static DynamoDBEvent ToStreamEvent(IEnumerable<OutboxRecord> records) =>
        new() { Records = records.Select(record => record.ToStreamRecord()).ToList() };
}
