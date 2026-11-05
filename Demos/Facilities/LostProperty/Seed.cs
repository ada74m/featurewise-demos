namespace Demos.LostProperty;

// What a new visitor's office holds, dated from when they first arrive. Some items have waited long
// enough for the next charity collection, and the traveller has already reported an umbrella, which
// nobody has handed in yet.
public static class Seed
{
    public const string Traveller = "traveller";

    public static IReadOnlyList<FoundItem> Items(DateTimeOffset now) =>
    [
        new("F-000001", "Glove", "One left glove, red wool", "Crewe", now.AddDays(-3), ItemStatus.Unclaimed),
        new("F-000002", "Instrument", "A tuba, in its case", "York", now.AddDays(-12), ItemStatus.Unclaimed),
        new("F-000003", "Clothing", "A wedding dress, ivory, size 12", "Bristol Temple Meads", now.AddDays(-40), ItemStatus.Unclaimed),
        new("F-000004", "Teeth", "False teeth, upper set only", "Crewe", now.AddDays(-75), ItemStatus.Unclaimed),
        new("F-000005", "Footwear", "A single roller skate", "York", now.AddDays(-95), ItemStatus.Unclaimed),
        new("F-000006", "Book", "A library book, eleven years overdue", "Bristol Temple Meads", now.AddDays(-120), ItemStatus.Unclaimed)
    ];

    public static IReadOnlyList<LostReport> Reports(DateTimeOffset now) =>
    [
        new("R-000001", Traveller, "Umbrella", "Black, with a duck's head handle", now.AddDays(-1))
    ];
}
