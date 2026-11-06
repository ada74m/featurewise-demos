using Featurewise;

namespace Demos.BoxOffice;

// The caller's tickets, and the performances they're waiting for returns to. A place in a queue offers a
// link to buy seats as soon as there are some, so the theatregoer needn't go looking.
public class YourTickets(IBookings bookings, IReturnsQueue queue, IPerformances performances, Invocation invocation)
    : Query<YourTickets, YourTickets.Args, YourTickets.Result>
{
    public record Args : Link<YourTickets, Result>;

    public record Result(IReadOnlyList<Tickets> Tickets, IReadOnlyList<Waiting> ReturnsQueues, Performances.Args LinkToPerformances);

    public record Tickets(Booking Booking, Performance Performance);

    public record Waiting(Performance Performance, BuySeats.Args? LinkToBuySeats);

    public override async Task<Result> Invoke(Args args, CancellationToken cancellationToken)
    {
        var caller = invocation.Caller.Identity?.Name;
        var byId = (await performances.All(cancellationToken)).ToDictionary(performance => performance.Id);

        var tickets = (await bookings.All(cancellationToken))
            .Where(booking => booking.Theatregoer == caller && byId.ContainsKey(booking.PerformanceId))
            .OrderBy(booking => byId[booking.PerformanceId].StartsAt)
            .Select(booking => new Tickets(booking, byId[booking.PerformanceId]))
            .ToList();

        var waiting = (await queue.All(cancellationToken))
            .Where(place => place.Theatregoer == caller && byId.ContainsKey(place.PerformanceId))
            .Select(place => byId[place.PerformanceId])
            .OrderBy(performance => performance.StartsAt)
            .Select(performance => new Waiting(
                performance,
                !performance.Cancelled && performance.SeatsLeft > 0 ? new BuySeats.Args(performance.Id, null) : null))
            .ToList();

        return new Result(tickets, waiting, new Performances.Args());
    }
}
