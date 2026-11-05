namespace Demos;

// Raises events for the service's own workers. Raising one stages it in the invocation's IChanges, in the
// outbox, so it goes out if, and only if, the feature's other writes are committed with it.
public interface IEvents
{
    void Raise<TEvent>(TEvent @event) where TEvent : notnull;
}
