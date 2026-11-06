namespace Demos.BoxOffice;

// A theatregoer waiting for returns to a sold-out performance.
public record QueuePlace(string PerformanceId, string Theatregoer, DateTimeOffset JoinedAt);
