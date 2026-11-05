using System.Diagnostics;
using System.Text.Json;

namespace Demos.Local;

// Raises events into the outbox, as part of the invocation's commit, each recording the invocation that
// raised it.
public class InMemoryEvents(InMemoryOutbox outbox, SandboxedTables tables, InMemoryChanges changes) : IEvents
{
    public void Raise<TEvent>(TEvent @event) where TEvent : notnull
    {
        var record = new OutboxRecord(
            Guid.NewGuid().ToString("N"), tables.Sandbox, typeof(TEvent).Name, JsonSerializer.Serialize(@event), Activity.Current?.Id);
        changes.Stage(() => true, () => outbox.Add(record));
    }
}
