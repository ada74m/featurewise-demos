using Featurewise;

namespace Demos.BoxOffice;

// The box office calls a performance off. Only staff may; Performances offers them the link, and nobody else.
public class CancelPerformance(IPerformances performances, IChanges changes)
    : Command<CancelPerformance.Args, CancelPerformance.Result>
{
    public record Args(string PerformanceId) : Link<CancelPerformance, Result>;

    public abstract record Result
    {
        private Result()
        {
        }

        public record Cancelled(Performance Performance, Performances.Args LinkToPerformances) : Result;

        public record AlreadyCancelled(string PerformanceId) : Result;

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
                return new Result.AlreadyCancelled(performance.Id);

            var cancelled = performance with { Cancelled = true };
            performances.Replace(performance, cancelled);
            if (await changes.Commit(cancellationToken))
                return new Result.Cancelled(cancelled, new Performances.Args());
        }
    }
}
