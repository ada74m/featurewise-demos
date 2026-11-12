using Demos.Local;

namespace Demos.BoxOffice.Local;

// Who is waiting for returns, in the invocation's sandbox. Nobody is, at first.
public class InMemoryReturnsQueue(SandboxedTables tables, InMemoryChanges changes) : IReturnsQueue
{
    private InMemoryTable<QueuePlace> Table => tables.Table<QueuePlace>("box-office/returns-queue", () => []);

    public Task<IReadOnlyList<QueuePlace>> All(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<QueuePlace>>(Table.All().OrderBy(place => place.JoinedAt).ToList());

    public void Join(QueuePlace place)
    {
        var table = Table;
        changes.Stage(() => !table.Contains(KeyOf(place)), () => table.Put(KeyOf(place), place));
    }

    public void Leave(QueuePlace place)
    {
        var table = Table;
        changes.Stage(() => true, () => table.Remove(KeyOf(place)));
    }

    private static string KeyOf(QueuePlace place) => $"{place.PerformanceId}/{place.Theatregoer}";
}
