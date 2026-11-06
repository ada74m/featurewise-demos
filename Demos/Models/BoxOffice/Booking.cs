namespace Demos.BoxOffice;

// Seats a theatregoer has bought for a performance. The reference is what they give at the door.
public record Booking(string Reference, string Theatregoer, string PerformanceId, int Seats, DateTimeOffset BookedAt);
