namespace Demos.Local;

// Forgetting a demo is dropping the sandbox's copies of its tables, as part of the invocation's commit.
public class InMemoryDemoData(SandboxedTables tables, InMemoryChanges changes) : IDemoData
{
    public Task Reset(CancellationToken cancellationToken)
    {
        changes.Stage(() => true, () => tables.Drop(""));
        return Task.CompletedTask;
    }
}
