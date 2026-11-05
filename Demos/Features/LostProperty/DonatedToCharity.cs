using Featurewise;

namespace Demos.LostProperty;

// What the charity collections have taken: items nobody claimed in time.
public class DonatedToCharity(IFoundItems items) : Query<DonatedToCharity, DonatedToCharity.Args, DonatedToCharity.Result>
{
    public record Args : Link<DonatedToCharity, Result>;

    public record Result(IReadOnlyList<FoundItem> Items, UnclaimedItems.Args LinkToUnclaimedItems);

    public override async Task<Result> Invoke(Args args, CancellationToken cancellationToken)
    {
        var donated = (await items.All(cancellationToken))
            .Where(item => item.Status == ItemStatus.DonatedToCharity)
            .OrderBy(item => item.HandedInAt)
            .ToList();

        return new Result(donated, new UnclaimedItems.Args());
    }
}
