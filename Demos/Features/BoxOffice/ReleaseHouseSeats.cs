using Featurewise;

namespace Demos.BoxOffice;

// The box office puts the seats it held back for the house on sale, so a sold-out performance has seats
// again. Only staff may.
public class ReleaseHouseSeats(IPerformances performances, IChanges changes)
    : Command<ReleaseHouseSeats.Args, ReleaseHouseSeats.Result>
{
    public record Args(string PerformanceId) : Link<ReleaseHouseSeats, Result>;

    public abstract record Result
    {
        private Result()
        {
        }

        public record Released(Performance Performance, Performances.Args LinkToPerformances) : Result;

        // None were held back, or they've been released already.
        public record NoHouseSeats(string PerformanceId) : Result;

        public record PerformanceCancelled(string PerformanceId) : Result;

        public record NotFound(string PerformanceId) : Result;
    }

    public override async Task<Result> Invoke(Args args, CancellationToken cancellationToken)
    {
        // A sale may change the performance between reading it and committing: look again.
        while (true)
        {
            var performance = await performances.Find(args.PerformanceId, cancellationToken);
            if (performance is null)
                return new Result.NotFound(args.PerformanceId);
            if (performance.Cancelled)
                return new Result.PerformanceCancelled(performance.Id);
            if (performance.HouseSeats == 0)
                return new Result.NoHouseSeats(performance.Id);

            var released = performance with { SeatsLeft = performance.SeatsLeft + performance.HouseSeats, HouseSeats = 0 };
            performances.Replace(performance, released);
            if (await changes.Commit(cancellationToken))
                return new Result.Released(released, new Performances.Args());
        }
    }
}
