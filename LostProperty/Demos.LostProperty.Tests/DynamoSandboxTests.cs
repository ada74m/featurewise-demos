using Demos.Testing;
using Featurewise;

namespace Demos.LostProperty.Tests;

// What only DynamoDB's sandboxes do: their data expires a week after their first use.
public class DynamoSandboxTests : IAsyncDisposable
{
    private readonly DynamoLocal _dynamo = new(Aws.Registrations.AddFacilities);
    private readonly Features _features;

    public DynamoSandboxTests() => _features = ServiceFeatures.Over(_dynamo);

    public async ValueTask DisposeAsync()
    {
        await _features.DisposeAsync();
        await _dynamo.DisposeAsync();
    }

    [SkippableFact]
    public async Task ASandboxStartsAfreshAWeekAfterItsFirstUse()
    {
        var handedIn = (HandInItem.Result.HandedIn)await _features.Invoke(
            new HandInItem.Args("Umbrella", "Black", "Crewe"), Callers.Clerk.In("a"));

        _dynamo.Clock.Advance(TimeSpan.FromDays(8));
        var unclaimed = await _features.Invoke(new UnclaimedItems.Args(), Callers.Traveller.In("a"));

        Assert.DoesNotContain(unclaimed.Items, entry => entry.Item.Id == handedIn.Item.Id);
        Assert.Equal(6, unclaimed.Items.Count);
    }

    [SkippableFact]
    public async Task OnlySandboxesWithLiveDataRunScheduledJobs()
    {
        await _features.Invoke(new UnclaimedItems.Args(), Callers.Traveller.In("a"));
        _dynamo.Clock.Advance(TimeSpan.FromDays(5));
        await _features.Invoke(new UnclaimedItems.Args(), Callers.Traveller.In("b"));
        _dynamo.Clock.Advance(TimeSpan.FromDays(3));

        Assert.Equal(["b"], await _dynamo.Sandboxes());
    }
}
