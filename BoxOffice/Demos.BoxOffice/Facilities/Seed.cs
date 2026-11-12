using System.Globalization;

namespace Demos.BoxOffice;

// What a new visitor's box office holds: the four performances of the coming weekend, dated from when they
// first arrive. Friday is nearly full, and the Saturday matinee is sold out, though the house seats are
// still held back. The theatregoer already has tickets for Thursday.
public static class Seed
{
    public const string Theatregoer = "theatregoer";

    public const string Title = "The Pirates of Penge";

    public static IReadOnlyList<Performance> Performances(DateTimeOffset now)
    {
        var today = now.UtcDateTime.Date;
        var untilThursday = ((int)DayOfWeek.Thursday - (int)today.DayOfWeek + 7) % 7;
        var thursday = today.AddDays(untilThursday == 0 ? 7 : untilThursday);
        return
        [
            On("thursday", thursday, 19, 30, seatsLeft: 38),
            On("friday", thursday.AddDays(1), 19, 30, seatsLeft: 2),
            On("saturday-matinee", thursday.AddDays(2), 14, 30, seatsLeft: 0, houseSeats: 4),
            On("saturday-evening", thursday.AddDays(2), 19, 30, seatsLeft: 51)
        ];
    }

    public static IReadOnlyList<Booking> Bookings(DateTimeOffset now) =>
    [
        new("B-000001", Theatregoer, "thursday", 2, now.AddDays(-9))
    ];

    private static Performance On(string id, DateTime day, int hour, int minute, int seatsLeft, int houseSeats = 0)
    {
        var startsAt = new DateTimeOffset(day.AddHours(hour).AddMinutes(minute), TimeSpan.Zero);
        var when = string.Create(CultureInfo.InvariantCulture, $"{startsAt:dddd d MMMM, h.mm}{(hour < 12 ? "am" : "pm")}");
        return new Performance(id, Title, when, startsAt, seatsLeft, houseSeats);
    }
}
