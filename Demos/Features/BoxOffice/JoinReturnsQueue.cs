using Featurewise;

namespace Demos.BoxOffice;

// A theatregoer waits for returns to a sold-out performance. If seats have come free meanwhile, there's
// nothing to wait for, and it offers them instead.
public class JoinReturnsQueue(IPerformances performances, IReturnsQueue queue, IChanges changes, Invocation invocation, TimeProvider clock)
    : Command<JoinReturnsQueue, JoinReturnsQueue.Args, JoinReturnsQueue.Result>
{
    public record Args(string PerformanceId) : Link<JoinReturnsQueue, Result>;

    public abstract record Result
    {
        private Result()
        {
        }

        public record Joined(QueuePlace Place, YourTickets.Args LinkToYourTickets) : Result;

        public record AlreadyQueuing(YourTickets.Args LinkToYourTickets) : Result;

        public record SeatsAvailable(int SeatsLeft, BuySeats.Args LinkToBuySeats) : Result;

        public record PerformanceCancelled(Performances.Args LinkToOtherPerformances) : Result;

        public record NotFound(string PerformanceId) : Result;
    }

    public override async Task<Result> Invoke(Args args, CancellationToken cancellationToken)
    {
        var performance = await performances.Find(args.PerformanceId, cancellationToken);
        if (performance is null)
            return new Result.NotFound(args.PerformanceId);
        if (performance.Cancelled)
            return new Result.PerformanceCancelled(new Performances.Args());
        if (performance.SeatsLeft > 0)
            return new Result.SeatsAvailable(performance.SeatsLeft, new BuySeats.Args(performance.Id, null));

        // The place's condition fails if the theatregoer is already waiting.
        var place = new QueuePlace(performance.Id, invocation.Caller.Identity!.Name!, clock.GetUtcNow());
        queue.Join(place);
        if (!await changes.Commit(cancellationToken))
            return new Result.AlreadyQueuing(new YourTickets.Args());
        return new Result.Joined(place, new YourTickets.Args());
    }
}
