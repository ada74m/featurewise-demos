using Demos.Local;

namespace Demos.LostProperty.Local;

// Travellers' lost reports, in the invocation's sandbox.
public class InMemoryLostReports(SandboxedTables tables, InMemoryChanges changes, TimeProvider clock) : ILostReports
{
    private InMemoryTable<LostReport> Reports =>
        tables.Table("lost-property/reports", () => Seed.Reports(clock.GetUtcNow()).Select(report => (report.Reference, report)));

    public Task<LostReport?> Find(string reference, CancellationToken cancellationToken) => Task.FromResult(Reports[reference]);

    public Task<IReadOnlyList<LostReport>> All(CancellationToken cancellationToken) => Task.FromResult(Reports.All());

    public void Add(LostReport report)
    {
        var reports = Reports;
        changes.Stage(() => !reports.Contains(report.Reference), () => reports.Put(report.Reference, report));
    }

    public void Match(LostReport report, string itemId)
    {
        var reports = Reports;
        changes.Stage(
            () => reports[report.Reference] is { } current && current.MatchedItemId == report.MatchedItemId,
            () => reports.Put(report.Reference, report with { MatchedItemId = itemId }));
    }
}
