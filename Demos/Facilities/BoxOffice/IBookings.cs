namespace Demos.BoxOffice;

// Every booking for the production. Writes are staged in the invocation's IChanges.
public interface IBookings
{
    // A demo-sized theatre can afford to scan.
    Task<IReadOnlyList<Booking>> All(CancellationToken cancellationToken);

    // Stages keeping a new booking, on condition that no booking has its reference.
    void Add(Booking booking);
}
