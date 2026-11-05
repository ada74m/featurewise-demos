using System.Diagnostics;
using Amazon.Lambda.TestUtilities;
using Demos.Local;
using Demos.LostProperty;
using Demos.QueueWorker;
using Featurewise;

namespace Demos.Tests;

// The queue worker, given the outbox's events as DynamoDB's stream delivers them.
public class QueueWorkerTests : IAsyncDisposable
{
    private readonly Fakes _fakes = new();
    private readonly Features _features;
    private readonly Function _worker;

    public QueueWorkerTests()
    {
        _features = ServiceFeatures.Over(_fakes);
        _worker = new Function(_fakes.Register);
    }

    public ValueTask DisposeAsync() => _features.DisposeAsync();

    [Fact]
    public async Task AnEventInvokesItsWorkerInTheSandboxItWasRaisedIn()
    {
        var handedIn = (HandInItem.Result.HandedIn)await _features.Invoke(
            new HandInItem.Args("Umbrella", "Black", "Crewe"), Callers.Clerk.In("a"));

        var response = await _worker.Handle(OutboxRecord.ToStreamEvent(_fakes.Outbox.Take()), Context());

        Assert.Empty(response.BatchItemFailures);
        var inA = await _features.Invoke(new YourLostReports.Args(), Callers.Traveller.In("a"));
        var inLocal = await _features.Invoke(new YourLostReports.Args(), Callers.Traveller);
        Assert.Equal(handedIn.Item.Id, inA.Reports[0].PossibleMatch?.Id);
        Assert.Null(inLocal.Reports[0].PossibleMatch);
    }

    [Fact]
    public async Task AnEventsWorkerIsLinkedToTheInvocationThatRaisedIt()
    {
        var finished = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == Features.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => { lock (finished) finished.Add(activity); }
        };
        ActivitySource.AddActivityListener(listener);

        await _features.Invoke(new HandInItem.Args("Umbrella", "Black", "Crewe"), Callers.Clerk.In("caused"));
        var records = _fakes.Outbox.Take();
        await _worker.Handle(OutboxRecord.ToStreamEvent(records), Context());

        Activity[] seen;
        lock (finished) seen = finished.ToArray();
        var handIn = seen.Single(activity => activity.Id == Assert.Single(records).CausedBy);
        var worker = seen.Single(activity => activity.OperationName == nameof(HandleItemHandedIn)
            && activity.Links.Any(link => link.Context.TraceId == handIn.TraceId));
        Assert.Equal(nameof(HandInItem), handIn.OperationName);
        Assert.NotEqual(handIn.TraceId, worker.TraceId);
    }

    [Fact]
    public async Task TheFirstEventThatFailsIsReportedForDeliveringAgain()
    {
        var records = new[]
        {
            new OutboxRecord("1", "local", nameof(ItemHandedIn), """{"ItemId":"F-000001"}"""),
            new OutboxRecord("2", "local", "SomethingElse", "{}"),
            new OutboxRecord("3", "local", nameof(ItemHandedIn), """{"ItemId":"F-000002"}""")
        };

        var response = await _worker.Handle(OutboxRecord.ToStreamEvent(records), Context());

        Assert.Equal("2", Assert.Single(response.BatchItemFailures).ItemIdentifier);
    }

    private static TestLambdaContext Context() => new() { RemainingTime = TimeSpan.FromSeconds(30) };
}
