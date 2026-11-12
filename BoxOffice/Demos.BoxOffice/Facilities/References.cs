namespace Demos.BoxOffice;

// Makes the references the box office gives out, such as B-3A9C1E for a booking. Six hex digits make a
// clash unlikely, and the facilities refuse one anyway.
public static class References
{
    public static string NewBookingReference() => $"B-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
}
