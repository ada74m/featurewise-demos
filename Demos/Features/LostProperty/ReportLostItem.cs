using Featurewise;

namespace Demos.LostProperty;

// A traveller describes something they lost. If the office already has something like it, the report is
// matched to it at once, and the result offers to claim it. A link with any detail null, as YourLostReports
// offers, asks for them: invoking it returns NeedsDetails, with the link back, as filled in so far, to
// complete and follow again.
public class ReportLostItem(ILostReports reports, IFoundItems items, IChanges changes, Invocation invocation, TimeProvider clock)
    : Command<ReportLostItem.Args, ReportLostItem.Result>
{
    public record Args(string? Category, string? Description) : Link<ReportLostItem, Result>;

    public abstract record Result
    {
        private Result()
        {
        }

        public record Reported(LostReport Report, YourLostReports.Args LinkToYourReports) : Result;

        // The office already has something that may be it: the item handed in first, of those that fit.
        public record MayAlreadyHaveIt(LostReport Report, FoundItem Item, ClaimItem.Args LinkToClaim, YourLostReports.Args LinkToYourReports) : Result;

        public record NeedsDetails(IReadOnlyList<string> Missing, Args LinkToReport) : Result;
    }

    public override async Task<Result> Invoke(Args args, CancellationToken cancellationToken)
    {
        var missing = Details.Missing(
            (nameof(Args.Category), args.Category),
            (nameof(Args.Description), args.Description));
        if (missing.Count > 0)
            return new Result.NeedsDetails(missing, args);

        var report = new LostReport(
            References.NewReportReference(), invocation.Caller.Identity!.Name!, args.Category!.Trim(),
            args.Description!.Trim(), clock.GetUtcNow());
        var match = (await items.All(cancellationToken))
            .Where(item => Matches.Fits(report, item))
            .MinBy(item => item.HandedInAt);
        if (match is not null)
            report = report with { MatchedItemId = match.Id };
        reports.Add(report);

        if (!await changes.Commit(cancellationToken))
            throw new InvalidOperationException($"a report already has the reference {report.Reference}");
        return match is null
            ? new Result.Reported(report, new YourLostReports.Args())
            : new Result.MayAlreadyHaveIt(report, match, new ClaimItem.Args(match.Id, report.Reference), new YourLostReports.Args());
    }
}
