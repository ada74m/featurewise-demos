namespace Demos.Local;

// Each demo's tables are named <demo>/<table>, so forgetting a demo is dropping the sandbox's copies of
// the tables under its name, as part of the invocation's commit.
public class InMemoryDemoData(SandboxedTables tables, InMemoryChanges changes) : IDemoData
{
    public Task Reset(string demo, CancellationToken cancellationToken)
    {
        changes.Stage(() => true, () => tables.Drop($"{demo}/"));
        return Task.CompletedTask;
    }
}
