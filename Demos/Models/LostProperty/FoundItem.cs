namespace Demos.LostProperty;

// Something found at a station and handed in to the lost property office.
public record FoundItem(
    string Id,
    string Category,
    string Description,
    string Station,
    DateTimeOffset HandedInAt,
    ItemStatus Status,
    string? ClaimedBy = null);
