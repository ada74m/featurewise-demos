using Featurewise;

namespace Demos;

// Starts a demo again from its seed, forgetting whatever has been done in it in the caller's sandbox, so
// a visitor can follow its story from the beginning. Every demo registers it, so its link is the demo's
// own, as in lost-property/ResetDemo. Any persona may.
public class ResetDemo(IDemoData data, IChanges changes) : Command<ResetDemo.Args, ResetDemo.Result>
{
    public record Args : Link<ResetDemo, Result>;

    public record Result;

    public override async Task<Result> Invoke(Args args, CancellationToken cancellationToken)
    {
        await data.Reset(cancellationToken);
        await changes.Commit(cancellationToken);
        return new Result();
    }
}
