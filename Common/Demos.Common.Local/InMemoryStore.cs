using System.Collections.Concurrent;

namespace Demos.Local;

// Keeps every demo's data in memory, in place of DynamoDB's table, for the local host and the tests. Each
// sandbox has its own copy of each table, made from the demo's seed the first time the sandbox uses it.
public class InMemoryStore
{
    private readonly ConcurrentDictionary<(string Sandbox, string Table), Lazy<ConcurrentDictionary<string, object>>> _tables = new();

    // The sandboxes that have used any table, which scheduled jobs run in.
    public IReadOnlyList<string> Sandboxes => _tables.Keys.Select(key => key.Sandbox).Distinct().Order().ToList();

    // Forgets the sandbox's copies of the tables whose names start with the prefix, so they're seeded again.
    public void Drop(string sandbox, string prefix)
    {
        foreach (var key in _tables.Keys.Where(key => key.Sandbox == sandbox && key.Table.StartsWith(prefix, StringComparison.Ordinal)))
            _tables.TryRemove(key, out _);
    }

    public ConcurrentDictionary<string, object> Table(string sandbox, string table, Func<IEnumerable<(string Key, object Value)>> seed) =>
        _tables.GetOrAdd(
            (sandbox, table),
            _ => new(() => new ConcurrentDictionary<string, object>(seed().Select(row => KeyValuePair.Create(row.Key, row.Value))))).Value;
}
