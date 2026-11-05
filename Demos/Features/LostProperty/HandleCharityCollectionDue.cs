using Featurewise;
using Microsoft.Extensions.Logging;

namespace Demos.LostProperty;

// The charity collects, on a schedule, everything that has waited unclaimed for three months.
public class HandleCharityCollectionDue(IFoundItems items, IChanges changes, TimeProvider clock, ILogger<HandleCharityCollectionDue> logger)
    : Worker<HandleCharityCollectionDue, HandleCharityCollectionDue.Args>
{
    public static readonly TimeSpan KeptFor = TimeSpan.FromDays(90);

    public record Args : Link<HandleCharityCollectionDue, Nil>;

    public override async Task<Nil> Invoke(Args args, CancellationToken cancellationToken)
    {
        var due = clock.GetUtcNow() - KeptFor;
        var donated = 0;
        foreach (var item in await items.All(cancellationToken))
        {
            if (item.Status == ItemStatus.Unclaimed && item.HandedInAt <= due)
            {
                items.Donate(item);
                donated++;
            }
        }

        // Fails only if a traveller claimed one of them meanwhile. Nothing is written then, and the
        // invocation fails, so the next collection starts again from what's there.
        if (!await changes.Commit(cancellationToken))
            throw new InvalidOperationException("items were claimed while the collection was being made");

        logger.LogInformation("The charity collected {Donated} items", donated);
        return Nil.Value;
    }
}
