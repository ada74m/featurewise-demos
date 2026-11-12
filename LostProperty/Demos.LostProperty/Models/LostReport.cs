namespace Demos.LostProperty;

// A traveller's description of something they lost, and the found item that may be it, once one is
// handed in.
public record LostReport(
    string Reference,
    string Traveller,
    string Category,
    string Description,
    DateTimeOffset ReportedAt,
    string? MatchedItemId = null);
