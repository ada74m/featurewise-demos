namespace Demos.BoxOffice;

// One performance of the production. The house seats are held back from sale until the box office
// releases them.
public record Performance(
    string Id,
    string Title,
    string When,
    DateTimeOffset StartsAt,
    int SeatsLeft,
    int HouseSeats,
    bool Cancelled = false);
