using System.Text;
using System.Text.Json;
using Amazon.Lambda.TestUtilities;
using Demos.Testing;
using Demos.LostProperty.Functions;
using Featurewise;

namespace Demos.LostProperty.Tests;

// The scheduled worker, given what EventBridge Scheduler sends.
public class ScheduledWorkerTests : IAsyncDisposable
{
    private readonly Fakes _fakes = new(Local.Registrations.AddFacilities);
    private readonly Features _features;
    private readonly ScheduledWorker _worker;

    public ScheduledWorkerTests()
    {
        _features = ServiceFeatures.Over(_fakes);
        _worker = new ScheduledWorker(_fakes.Register, _ => Task.FromResult(_fakes.Store.Sandboxes));
    }

    public ValueTask DisposeAsync() => _features.DisposeAsync();

    [Fact]
    public async Task AJobRunsInEverySandboxInUse()
    {
        await _features.Invoke(new UnclaimedItems.Args(), Callers.Traveller.In("a"));
        await _features.Invoke(new UnclaimedItems.Args(), Callers.Traveller.In("b"));

        await Handle("""{"Schedule":"arn:aws:scheduler:eu-west-2:123456789012:schedule/default/charity","Execution":"x1","Link":{"$worker":"lost-property/HandleCharityCollectionDue"}}""");

        Assert.Equal(2, (await _features.Invoke(new DonatedToCharity.Args(), Callers.Traveller.In("a"))).Items.Count);
        Assert.Equal(2, (await _features.Invoke(new DonatedToCharity.Args(), Callers.Traveller.In("b"))).Items.Count);
    }

    [Theory]
    [InlineData("""{"Schedule":"s","Link":{"$command":"lost-property/ClaimItem","ItemId":"F-000001","Reference":"R-000001"}}""")]
    [InlineData("""{"Schedule":"s"}""")]
    [InlineData("""{"Link":{"$worker":"lost-property/HandleCharityCollectionDue"}}""")]
    public async Task AnInputThatDoesntNameOneOfTheWorkersJobsFails(string input)
    {
        await Assert.ThrowsAsync<JsonException>(() => Handle(input));
    }

    private Task Handle(string input) =>
        _worker.Handle(new MemoryStream(Encoding.UTF8.GetBytes(input)), new TestLambdaContext { RemainingTime = TimeSpan.FromSeconds(30) });
}
