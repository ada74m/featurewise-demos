using System.Collections.Concurrent;

namespace Demos.Local;

// One sandbox's copy of a table, typed.
public class InMemoryTable<T>(ConcurrentDictionary<string, object> rows) where T : notnull
{
    public T? this[string key] => rows.TryGetValue(key, out var row) ? (T)row : default;

    public bool Contains(string key) => rows.ContainsKey(key);

    public IReadOnlyList<T> All() => rows.Values.Cast<T>().ToList();

    public void Put(string key, T value) => rows[key] = value;
}
