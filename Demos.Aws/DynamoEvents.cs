using System.Diagnostics;
using System.Text.Json;
using Amazon.DynamoDBv2.Model;

namespace Demos.Aws;

// Raises events into the outbox, rows in the demos' table under outbox#<id>, as part of the invocation's
// commit. DynamoDB's stream delivers each to the queue worker, with the sandbox it was raised in and the
// invocation that raised it, and TTL removes it a day later.
public class DynamoEvents(DynamoChanges changes, DynamoTables tables, TimeProvider clock, DynamoSettings settings) : IEvents
{
    public void Raise<TEvent>(TEvent @event) where TEvent : notnull
    {
        var row = new Dictionary<string, AttributeValue>
        {
            ["Sandbox"] = new() { S = tables.Sandbox },
            ["Key"] = new() { S = $"outbox#{Guid.NewGuid():N}" },
            ["Event"] = new() { S = typeof(TEvent).Name },
            ["Body"] = new() { S = JsonSerializer.Serialize(@event) },
            ["Expires"] = new() { N = (clock.GetUtcNow() + settings.OutboxLifetime).ToUnixTimeSeconds().ToString() }
        };
        if (Activity.Current?.Id is { } causedBy)
            row["CausedBy"] = new() { S = causedBy };

        changes.Stage(() => new TransactWriteItem { Put = new Put { TableName = settings.TableName, Item = row } });
    }
}
