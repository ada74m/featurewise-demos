using Featurewise;

namespace Demos.BoxOffice;

// Every performance of the production, in order, each with only the links that would work for this caller
// now: buying seats while there are some, joining the returns queue once there aren't, and for staff,
// cancelling it and releasing its house seats.
public class Performances(IPerformances performances, IReturnsQueue queue, LinkPermissions permissions, Invocation invocation)
    : Query<Performances, Performances.Args, Performances.Result>
{
    public record Args : Link<Performances, Result>;

    public record Result(IReadOnlyList<Entry> Performances, YourTickets.Args LinkToYourTickets);

    public record Entry(
        Performance Performance,
        BuySeats.Args? LinkToBuySeats,
        JoinReturnsQueue.Args? LinkToJoinReturnsQueue,
        CancelPerformance.Args? LinkToCancel,
        ReleaseHouseSeats.Args? LinkToReleaseHouseSeats);

    public override async Task<Result> Invoke(Args args, CancellationToken cancellationToken)
    {
        var caller = invocation.Caller.Identity?.Name;
        var waiting = (await queue.All(cancellationToken))
            .Where(place => place.Theatregoer == caller)
            .Select(place => place.PerformanceId)
            .ToHashSet();

        var entries = new List<Entry>();
        foreach (var performance in await performances.All(cancellationToken))
        {
            var on = !performance.Cancelled;
            var cancel = new CancelPerformance.Args(performance.Id);
            var release = new ReleaseHouseSeats.Args(performance.Id);
            entries.Add(new Entry(
                performance,
                on && performance.SeatsLeft > 0 ? new BuySeats.Args(performance.Id, null) : null,
                on && performance.SeatsLeft == 0 && !waiting.Contains(performance.Id) ? new JoinReturnsQueue.Args(performance.Id) : null,
                on && await permissions.MayInvoke(cancel) ? cancel : null,
                on && performance.HouseSeats > 0 && await permissions.MayInvoke(release) ? release : null));
        }

        return new Result(entries, new YourTickets.Args());
    }
}
