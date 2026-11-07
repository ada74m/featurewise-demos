using Featurewise;

namespace Demos.BoxOffice;

// A theatregoer buys seats for a performance. A link with no number of seats, as Performances offers, asks
// for it, and hands the link back to fill in. Each way it can't be done says what can: buy the seats that are left, join the returns queue, or
// choose another performance.
public class BuySeats(IPerformances performances, IBookings bookings, IReturnsQueue queue, IChanges changes, Invocation invocation, TimeProvider clock)
    : Command<BuySeats.Args, BuySeats.Result>
{
    public record Args(string PerformanceId, int? Seats) : Link<BuySeats, Result>;

    public abstract record Result
    {
        private Result()
        {
        }

        public record Bought(Booking Booking, YourTickets.Args LinkToYourTickets) : Result;

        public record NeedsDetails(IReadOnlyList<string> Missing, Args LinkToBuySeats) : Result;

        public record NotEnoughSeats(int SeatsLeft, Args LinkToBuySeatsLeft) : Result;

        public record SoldOut(JoinReturnsQueue.Args LinkToJoinReturnsQueue) : Result;

        public record PerformanceCancelled(Performances.Args LinkToOtherPerformances) : Result;

        public record NotFound(string PerformanceId) : Result;
    }

    public override async Task<Result> Invoke(Args args, CancellationToken cancellationToken)
    {
        if (args.Seats is not > 0)
            return new Result.NeedsDetails([nameof(Args.Seats)], args);
        var seats = args.Seats.Value;
        var theatregoer = invocation.Caller.Identity!.Name!;

        // Someone else may buy seats or cancel the performance between reading it and committing, and then
        // the commit's condition fails: look again, with what they did.
        while (true)
        {
            var performance = await performances.Find(args.PerformanceId, cancellationToken);
            if (performance is null)
                return new Result.NotFound(args.PerformanceId);
            if (performance.Cancelled)
                return new Result.PerformanceCancelled(new Performances.Args());
            if (performance.SeatsLeft == 0)
                return new Result.SoldOut(new JoinReturnsQueue.Args(performance.Id));
            if (performance.SeatsLeft < seats)
                return new Result.NotEnoughSeats(performance.SeatsLeft, args with { Seats = performance.SeatsLeft });

            var booking = new Booking(References.NewBookingReference(), theatregoer, performance.Id, seats, clock.GetUtcNow());
            performances.Replace(performance, performance with { SeatsLeft = performance.SeatsLeft - seats });
            bookings.Add(booking);

            // Someone waiting for returns who gets seats is waiting no longer.
            var place = (await queue.All(cancellationToken))
                .FirstOrDefault(place => place.PerformanceId == performance.Id && place.Theatregoer == theatregoer);
            if (place is not null)
                queue.Leave(place);

            if (await changes.Commit(cancellationToken))
                return new Result.Bought(booking, new YourTickets.Args());
        }
    }
}
