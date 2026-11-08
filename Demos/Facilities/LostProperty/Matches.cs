namespace Demos.LostProperty;

// The office's rule for which found item may be the one a report describes. HandleItemHandedIn applies it
// when an item is handed in, and ReportLostItem when a loss is reported, so the match is found in either
// order, and the two can't disagree.
public static class Matches
{
    // The item is waiting to be claimed, and it's the kind of thing the report describes.
    public static bool Fits(LostReport report, FoundItem item) =>
        item.Status == ItemStatus.Unclaimed
        && string.Equals(report.Category, item.Category, StringComparison.OrdinalIgnoreCase);

    // The report has no match yet, or its match has gone to someone else or to charity. A report whose
    // traveller claimed its match has found what it was looking for.
    public static bool IsLooking(LostReport report, FoundItem? match) =>
        report.MatchedItemId is null
        || match is null
        || match.Status == ItemStatus.DonatedToCharity
        || (match.Status == ItemStatus.Claimed && match.ClaimedBy != report.Traveller);
}
