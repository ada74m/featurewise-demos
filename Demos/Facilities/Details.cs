namespace Demos;

// For links that leave details to be supplied: which of them are still missing.
public static class Details
{
    // The names of the details that are null or blank, in the order given.
    public static IReadOnlyList<string> Missing(params (string Name, string? Value)[] details) =>
        details.Where(detail => string.IsNullOrWhiteSpace(detail.Value)).Select(detail => detail.Name).ToList();
}
