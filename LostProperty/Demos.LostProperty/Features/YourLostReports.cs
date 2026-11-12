using Featurewise;

namespace Demos.LostProperty;

// The caller's reports of things they lost, newest first. When a handed-in item may be the one a report
// describes, the report shows it, with a link to claim it while it's still unclaimed.
public class YourLostReports(ILostReports reports, IFoundItems items, Invocation invocation)
    : Query<YourLostReports.Args, YourLostReports.Result>
{
    public record Args : Link<YourLostReports, Result>;

    // The report link leaves the details to be supplied.
    public record Result(IReadOnlyList<Entry> Reports, ReportLostItem.Args LinkToReport, UnclaimedItems.Args LinkToUnclaimedItems);

    public record Entry(LostReport Report, FoundItem? PossibleMatch, ClaimItem.Args? LinkToClaim);

    public override async Task<Result> Invoke(Args args, CancellationToken cancellationToken)
    {
        var traveller = invocation.Caller.Identity?.Name;
        var entries = new List<Entry>();
        foreach (var report in (await reports.All(cancellationToken))
                     .Where(report => report.Traveller == traveller)
                     .OrderByDescending(report => report.ReportedAt))
        {
            var match = report.MatchedItemId is { } id ? await items.Find(id, cancellationToken) : null;
            var claim = match?.Status == ItemStatus.Unclaimed ? new ClaimItem.Args(match.Id, report.Reference) : null;
            entries.Add(new Entry(report, match, claim));
        }

        return new Result(entries, new ReportLostItem.Args(null, null), new UnclaimedItems.Args());
    }
}
