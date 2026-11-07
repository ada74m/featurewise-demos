using Featurewise;

namespace Demos.LostProperty;

// A traveller describes something they lost. A link with any detail null, as YourLostReports offers,
// asks for them: invoking it returns NeedsDetails, with the link back, as filled in so far, to complete and
// follow again.
public class ReportLostItem(ILostReports reports, IChanges changes, Invocation invocation, TimeProvider clock)
    : Command<ReportLostItem.Args, ReportLostItem.Result>
{
    public record Args(string? Category, string? Description) : Link<ReportLostItem, Result>;

    public abstract record Result
    {
        private Result()
        {
        }

        public record Reported(LostReport Report, YourLostReports.Args LinkToYourReports) : Result;

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
        reports.Add(report);

        if (!await changes.Commit(cancellationToken))
            throw new InvalidOperationException($"a report already has the reference {report.Reference}");
        return new Result.Reported(report, new YourLostReports.Args());
    }
}
