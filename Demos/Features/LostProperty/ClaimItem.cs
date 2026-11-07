using Featurewise;

namespace Demos.LostProperty;

// A traveller claims a found item, giving the reference of their report of losing it. The item is theirs
// if the report is theirs and describes the same kind of thing.
public class ClaimItem(IFoundItems items, ILostReports reports, IChanges changes, Invocation invocation)
    : Command<ClaimItem.Args, ClaimItem.Result>
{
    public record Args(string ItemId, string Reference) : Link<ClaimItem, Result>;

    public abstract record Result
    {
        private Result()
        {
        }

        public record Claimed(FoundItem Item, YourLostReports.Args LinkToYourReports) : Result;

        public record AlreadyClaimed(string ItemId) : Result;

        // The link back, to try another reference, and the traveller's reports, where their references are.
        public record DoesntMatchReport(string ItemId, string Reference, Args LinkToClaim, YourLostReports.Args LinkToYourReports) : Result;

        // No such item, or not any more: it went to charity.
        public record NotFound(string ItemId) : Result;
    }

    public override async Task<Result> Invoke(Args args, CancellationToken cancellationToken)
    {
        var item = await items.Find(args.ItemId, cancellationToken);
        if (item is null || item.Status == ItemStatus.DonatedToCharity)
            return new Result.NotFound(args.ItemId);
        if (item.Status == ItemStatus.Claimed)
            return new Result.AlreadyClaimed(args.ItemId);

        var traveller = invocation.Caller.Identity!.Name!;
        var report = await reports.Find(args.Reference, cancellationToken);
        if (report is null || report.Traveller != traveller
            || !string.Equals(report.Category, item.Category, StringComparison.OrdinalIgnoreCase))
            return new Result.DoesntMatchReport(args.ItemId, args.Reference, args, new YourLostReports.Args());

        // Someone else may claim it first, and then the claim's condition fails.
        items.Claim(item, traveller);
        if (!await changes.Commit(cancellationToken))
            return new Result.AlreadyClaimed(args.ItemId);
        return new Result.Claimed(item with { Status = ItemStatus.Claimed, ClaimedBy = traveller }, new YourLostReports.Args());
    }
}
