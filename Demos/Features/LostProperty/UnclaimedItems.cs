using Featurewise;

namespace Demos.LostProperty;

// Everything waiting at the office to be claimed, oldest first, with what a caller may do from here.
public class UnclaimedItems(IFoundItems items, LinkPermissions permissions)
    : Query<UnclaimedItems.Args, UnclaimedItems.Result>
{
    public record Args : Link<UnclaimedItems, Result>;

    // The hand-in link is offered only to clerks, and leaves the item's details to be supplied.
    public record Result(
        IReadOnlyList<Entry> Items,
        HandInItem.Args? LinkToHandIn,
        YourLostReports.Args LinkToYourReports,
        DonatedToCharity.Args LinkToDonated);

    public record Entry(FoundItem Item, ItemsFoundAt.Args LinkToStation);

    public override async Task<Result> Invoke(Args args, CancellationToken cancellationToken)
    {
        var unclaimed = (await items.All(cancellationToken))
            .Where(item => item.Status == ItemStatus.Unclaimed)
            .OrderBy(item => item.HandedInAt)
            .Select(item => new Entry(item, new ItemsFoundAt.Args(item.Station)))
            .ToList();

        var handIn = new HandInItem.Args(null, null, null);
        return new Result(
            unclaimed,
            await permissions.MayInvoke(handIn) ? handIn : null,
            new YourLostReports.Args(),
            new DonatedToCharity.Args());
    }
}
