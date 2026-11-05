namespace Demos.LostProperty;

// Makes the references the office gives out: F-3A9C1E for a found item, R-77D20B for a lost report. Six
// hex digits make a clash in one office unlikely, and the facilities refuse one anyway.
public static class References
{
    public static string NewItemId() => $"F-{Random()}";

    public static string NewReportReference() => $"R-{Random()}";

    private static string Random() => Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
}
