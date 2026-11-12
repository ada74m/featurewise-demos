using Demos.Aws;

namespace Demos.BoxOffice.Aws;

// Who is waiting for returns, in the invocation's sandbox. Nobody is, at first.
public class DynamoReturnsQueue(DynamoTables tables) : IReturnsQueue
{
    private DynamoTable<QueuePlace> Table => tables.Table<QueuePlace>("box-office/returns-queue", () => []);

    public async Task<IReadOnlyList<QueuePlace>> All(CancellationToken cancellationToken) =>
        (await Table.All(cancellationToken)).OrderBy(place => place.JoinedAt).ToList();

    public void Join(QueuePlace place) => Table.Add(KeyOf(place), place);

    public void Leave(QueuePlace place) => Table.Delete(KeyOf(place));

    private static string KeyOf(QueuePlace place) => $"{place.PerformanceId}/{place.Theatregoer}";
}
