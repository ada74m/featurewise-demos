using Demos.BoxOffice;

namespace Demos.Local.BoxOffice;

// The production's performances, in the invocation's sandbox.
public class InMemoryPerformances(SandboxedTables tables, InMemoryChanges changes, TimeProvider clock) : IPerformances
{
    private InMemoryTable<Performance> Table =>
        tables.Table("box-office/performances", () => Seed.Performances(clock.GetUtcNow()).Select(performance => (performance.Id, performance)));

    public Task<Performance?> Find(string id, CancellationToken cancellationToken) => Task.FromResult(Table[id]);

    public Task<IReadOnlyList<Performance>> All(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Performance>>(Table.All().OrderBy(performance => performance.StartsAt).ToList());

    // A performance is a record, so "nobody has changed it" is its row still equalling what was read.
    public void Replace(Performance read, Performance changed)
    {
        var table = Table;
        changes.Stage(() => table[read.Id] == read, () => table.Put(read.Id, changed));
    }
}
