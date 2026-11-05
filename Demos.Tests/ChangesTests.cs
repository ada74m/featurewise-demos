using Demos.Local;

namespace Demos.Tests;

// The in-memory unit of work, which the local host and every other test rely on to behave as DynamoDB's does.
public class ChangesTests
{
    private readonly InMemoryTable<string> _table = new(new());

    [Fact]
    public async Task NothingIsWrittenUntilTheCommit()
    {
        using var changes = new InMemoryChanges();

        changes.Stage(() => true, () => _table.Put("a", "1"));
        Assert.False(_table.Contains("a"));

        Assert.True(await changes.Commit(CancellationToken.None));
        Assert.Equal("1", _table["a"]);
    }

    [Fact]
    public async Task ACommitWhoseConditionNoLongerHoldsWritesNothing()
    {
        using var changes = new InMemoryChanges();

        changes.Stage(() => true, () => _table.Put("a", "1"));
        changes.Stage(() => false, () => _table.Put("b", "2"));

        Assert.False(await changes.Commit(CancellationToken.None));
        Assert.False(_table.Contains("a"));
    }

    [Fact]
    public void AnInvocationThatNeverCommitsItsWritesFails()
    {
        var changes = new InMemoryChanges();
        changes.Stage(() => true, () => _table.Put("a", "1"));

        Assert.Throws<InvalidOperationException>(changes.Dispose);
        Assert.False(_table.Contains("a"));
    }
}
