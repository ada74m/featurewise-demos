using System.Collections.Concurrent;

namespace Demos.Local;

// The events committed but not yet delivered, in place of DynamoDB's outbox and its stream. The local host
// relays them to the queue worker as they arrive; the tests relay them when they choose.
public class InMemoryOutbox
{
    private readonly ConcurrentQueue<OutboxRecord> _records = new();
    private readonly SemaphoreSlim _arrived = new(0);

    public void Add(OutboxRecord record)
    {
        _records.Enqueue(record);
        _arrived.Release();
    }

    // Everything committed so far, taken out of the outbox.
    public IReadOnlyList<OutboxRecord> Take()
    {
        var taken = new List<OutboxRecord>();
        while (_records.TryDequeue(out var record))
            taken.Add(record);
        return taken;
    }

    public Task WaitForMore(CancellationToken cancellationToken) => _arrived.WaitAsync(cancellationToken);
}
