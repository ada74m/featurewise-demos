using Demos.Aws;

namespace Demos.BoxOffice.Aws;

// The production's performances, in the invocation's sandbox.
public class DynamoPerformances(DynamoTables tables, TimeProvider clock) : IPerformances
{
    private DynamoTable<Performance> Table =>
        tables.Table("box-office/performances", () => Seed.Performances(clock.GetUtcNow()).Select(performance => (performance.Id, performance)));

    public Task<Performance?> Find(string id, CancellationToken cancellationToken) => Table.Find(id, cancellationToken);

    public async Task<IReadOnlyList<Performance>> All(CancellationToken cancellationToken) =>
        (await Table.All(cancellationToken)).OrderBy(performance => performance.StartsAt).ToList();

    // Only sales, cancelling and releasing house seats change a performance, so "nobody has changed it" is
    // the fields they change still being as they were read.
    public void Replace(Performance read, Performance changed) =>
        Table.Replace(read.Id, changed,
            DynamoCondition.FieldEquals(nameof(Performance.SeatsLeft), read.SeatsLeft)
                .And(DynamoCondition.FieldEquals(nameof(Performance.HouseSeats), read.HouseSeats))
                .And(DynamoCondition.FieldEquals(nameof(Performance.Cancelled), read.Cancelled)));
}
