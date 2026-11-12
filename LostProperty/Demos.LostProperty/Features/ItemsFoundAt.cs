using Featurewise;

namespace Demos.LostProperty;

// What's waiting to be claimed from one station, oldest first.
public class ItemsFoundAt(IFoundItems items) : Query<ItemsFoundAt.Args, ItemsFoundAt.Result>
{
    public record Args(string Station) : Link<ItemsFoundAt, Result>;

    public record Result(string Station, IReadOnlyList<FoundItem> Items, UnclaimedItems.Args LinkToAllStations);

    public override async Task<Result> Invoke(Args args, CancellationToken cancellationToken)
    {
        var found = (await items.All(cancellationToken))
            .Where(item => item.Status == ItemStatus.Unclaimed
                && string.Equals(item.Station, args.Station, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.HandedInAt)
            .ToList();

        return new Result(args.Station, found, new UnclaimedItems.Args());
    }
}
