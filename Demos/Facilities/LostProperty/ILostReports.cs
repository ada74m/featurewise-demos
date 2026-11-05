namespace Demos.LostProperty;

// Where travellers' reports of lost things are kept. Reads happen straight away. Writes are staged in the
// invocation's IChanges and happen only when the feature commits.
public interface ILostReports
{
    Task<LostReport?> Find(string reference, CancellationToken cancellationToken);

    // Every report. A demo-sized office can afford to scan.
    Task<IReadOnlyList<LostReport>> All(CancellationToken cancellationToken);

    // Stages keeping a new report, on condition that no report has its reference.
    void Add(LostReport report);

    // Stages noting that the item may be the one the report describes, on condition that the report
    // hasn't been matched already.
    void Match(LostReport report, string itemId);
}
