using Featurewise;

namespace Demos;

// Starts a demo again from its seed, forgetting whatever has been done in it, so a visitor can follow
// its story from the beginning. Any persona may.
public class ResetDemo(IDemoData data, IChanges changes) : Command<ResetDemo.Args, ResetDemo.Result>
{
    public record Args(string Demo) : Link<ResetDemo, Result>;

    public record Result(string Demo);

    public override async Task<Result> Invoke(Args args, CancellationToken cancellationToken)
    {
        await data.Reset(args.Demo, cancellationToken);
        await changes.Commit(cancellationToken);
        return new Result(args.Demo);
    }
}
