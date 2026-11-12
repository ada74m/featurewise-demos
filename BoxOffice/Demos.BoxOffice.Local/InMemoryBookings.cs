using Demos.Local;

namespace Demos.BoxOffice.Local;

// The production's bookings, in the invocation's sandbox.
public class InMemoryBookings(SandboxedTables tables, InMemoryChanges changes, TimeProvider clock) : IBookings
{
    private InMemoryTable<Booking> Table =>
        tables.Table("box-office/bookings", () => Seed.Bookings(clock.GetUtcNow()).Select(booking => (booking.Reference, booking)));

    public Task<IReadOnlyList<Booking>> All(CancellationToken cancellationToken) => Task.FromResult(Table.All());

    public void Add(Booking booking)
    {
        var table = Table;
        changes.Stage(() => !table.Contains(booking.Reference), () => table.Put(booking.Reference, booking));
    }
}
