using Featurewise;
using Microsoft.AspNetCore.Authorization;

namespace Demos.LostProperty;

// A clerk logs something found at a station. A link with any detail null, as UnclaimedItems offers, asks
// for them: invoking it returns NeedsDetails. Handing an item in raises ItemHandedIn, so that
// HandleItemHandedIn can look for travellers who reported losing it.
public class HandInItem(IFoundItems items, IEvents events, IChanges changes, TimeProvider clock)
    : Command<HandInItem, HandInItem.Args, HandInItem.Result>
{
    public record Args(string? Category, string? Description, string? Station) : Link<HandInItem, Result>;

    public abstract record Result
    {
        private Result()
        {
        }

        // The item's id is the reference the clerk writes on its label.
        public record HandedIn(FoundItem Item, UnclaimedItems.Args LinkToUnclaimedItems) : Result;

        public record NeedsDetails(IReadOnlyList<string> Missing) : Result;
    }

    // Only clerks hand items in.
    public class Rule : AuthorizationHandler<InvokeRequirement, Args>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context, InvokeRequirement requirement, Args link)
        {
            if (context.User.IsInRole("clerk"))
                context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }

    public override async Task<Result> Invoke(Args args, CancellationToken cancellationToken)
    {
        var missing = Details.Missing(
            (nameof(Args.Category), args.Category),
            (nameof(Args.Description), args.Description),
            (nameof(Args.Station), args.Station));
        if (missing.Count > 0)
            return new Result.NeedsDetails(missing);

        var item = new FoundItem(
            References.NewItemId(), args.Category!.Trim(), args.Description!.Trim(), args.Station!.Trim(),
            clock.GetUtcNow(), ItemStatus.Unclaimed);
        items.Add(item);
        events.Raise(new ItemHandedIn(item.Id));

        if (!await changes.Commit(cancellationToken))
            throw new InvalidOperationException($"an item already has the id {item.Id}");
        return new Result.HandedIn(item, new UnclaimedItems.Args());
    }
}
