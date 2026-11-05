using Featurewise;

namespace Demos.Local;

// The tables of the invocation's sandbox, which the caller carries, so that a demo's facilities read and
// write as if there were only one visitor.
public class SandboxedTables(InMemoryStore store, Invocation invocation)
{
    public string Sandbox => Sandboxes.Of(invocation.Caller);

    public void Drop(string prefix) => store.Drop(Sandbox, prefix);

    public InMemoryTable<T> Table<T>(string name, Func<IEnumerable<(string Key, T Value)>> seed) where T : notnull =>
        new(store.Table(Sandbox, name, () => seed().Select(row => (row.Key, (object)row.Value))));
}
