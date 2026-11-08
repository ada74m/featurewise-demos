using Featurewise;
using Microsoft.Extensions.Logging;

namespace Demos.LostProperty;

// When an item is handed in, notes it on every report still looking for the same kind of thing, so
// those travellers see it in YourLostReports. Running it twice for one item changes nothing more.
public class HandleItemHandedIn(IFoundItems items, ILostReports reports, IChanges changes, ILogger<HandleItemHandedIn> logger)
    : Worker<HandleItemHandedIn.Args>
{
    public record Args(string ItemId) : Link<HandleItemHandedIn, Nil>;

    public override async Task<Nil> Invoke(Args args, CancellationToken cancellationToken)
    {
        if (await items.Find(args.ItemId, cancellationToken) is not { Status: ItemStatus.Unclaimed } item)
            return Nil.Value;

        var all = (await items.All(cancellationToken)).ToDictionary(found => found.Id);
        var matched = 0;
        foreach (var report in await reports.All(cancellationToken))
        {
            var match = report.MatchedItemId is { } id ? all.GetValueOrDefault(id) : null;
            if (report.MatchedItemId != item.Id && Matches.IsLooking(report, match) && Matches.Fits(report, item))
            {
                reports.Match(report, item.Id);
                matched++;
            }
        }

        // Fails only if another item was matched to one of the reports meanwhile. Nothing is written then,
        // and the invocation fails, so the event is delivered again.
        if (!await changes.Commit(cancellationToken))
            throw new InvalidOperationException($"the reports changed while {item.Id} was being matched");

        logger.LogInformation("Matched {Item} to {Matched} reports", item.Id, matched);
        return Nil.Value;
    }
}
