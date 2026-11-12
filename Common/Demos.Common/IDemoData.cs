namespace Demos;

// What a demo holds for its caller.
public interface IDemoData
{
    // Stages forgetting everything the demo holds for the caller's sandbox, in the invocation's IChanges,
    // so that once the feature commits, the demo starts again from its seed.
    Task Reset(CancellationToken cancellationToken);
}
