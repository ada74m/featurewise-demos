namespace Demos;

// What each demo holds for its caller.
public interface IDemoData
{
    // Stages forgetting everything the demo holds for the caller, in the invocation's IChanges, so that
    // once the feature commits, the demo starts again from its seed.
    Task Reset(string demo, CancellationToken cancellationToken);
}
