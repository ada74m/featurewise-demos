using Demos.BoxOffice;

namespace Demos.Aws.BoxOffice;

// The production's bookings, in the invocation's sandbox.
public class DynamoBookings(DynamoTables tables, TimeProvider clock) : IBookings
{
    private DynamoTable<Booking> Table =>
        tables.Table("box-office/bookings", () => Seed.Bookings(clock.GetUtcNow()).Select(booking => (booking.Reference, booking)));

    public Task<IReadOnlyList<Booking>> All(CancellationToken cancellationToken) => Table.All(cancellationToken);

    public void Add(Booking booking) => Table.Add(booking.Reference, booking);
}
