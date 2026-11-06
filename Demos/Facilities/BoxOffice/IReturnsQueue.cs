namespace Demos.BoxOffice;

// Who is waiting for returns to which performance. Writes are staged in the invocation's IChanges.
public interface IReturnsQueue
{
    Task<IReadOnlyList<QueuePlace>> All(CancellationToken cancellationToken);

    // Stages a place in the queue, on condition that the theatregoer isn't already waiting for that performance.
    void Join(QueuePlace place);

    // Stages giving up a place, as when its theatregoer buys seats after all.
    void Leave(QueuePlace place);
}
