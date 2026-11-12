namespace Demos.BoxOffice;

// The production's performances. Reads happen straight away. Writes are staged in the invocation's
// IChanges and happen only when the feature commits.
public interface IPerformances
{
    Task<Performance?> Find(string id, CancellationToken cancellationToken);

    // Every performance, cancelled ones included, in the order they're on.
    Task<IReadOnlyList<Performance>> All(CancellationToken cancellationToken);

    // Stages replacing the performance as it was read with its changed self, on condition that nobody has
    // changed it since, so that two sales can't both take the last seats.
    void Replace(Performance read, Performance changed);
}
