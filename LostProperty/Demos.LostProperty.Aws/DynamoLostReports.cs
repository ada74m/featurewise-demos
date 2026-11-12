using Demos.Aws;

namespace Demos.LostProperty.Aws;

// Travellers' lost reports, in the invocation's sandbox.
public class DynamoLostReports(DynamoTables tables, TimeProvider clock) : ILostReports
{
    private DynamoTable<LostReport> Reports =>
        tables.Table("lost-property/reports", () => Seed.Reports(clock.GetUtcNow()).Select(report => (report.Reference, report)));

    public Task<LostReport?> Find(string reference, CancellationToken cancellationToken) => Reports.Find(reference, cancellationToken);

    public Task<IReadOnlyList<LostReport>> All(CancellationToken cancellationToken) => Reports.All(cancellationToken);

    public void Add(LostReport report) => Reports.Add(report.Reference, report);

    public void Match(LostReport report, string itemId) =>
        Reports.Replace(report.Reference, report with { MatchedItemId = itemId }, report.MatchedItemId is { } matched
            ? DynamoCondition.FieldEquals(nameof(LostReport.MatchedItemId), matched)
            : DynamoCondition.FieldIsNull(nameof(LostReport.MatchedItemId)));
}
