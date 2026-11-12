using System.Security.Claims;
using Demos.Testing;
using Featurewise;

namespace Demos.BoxOffice.Tests;

// The theatre box office's features, invoked as its theatregoer and staff would, following the links
// they're given, over facilities in memory and on DynamoDB Local alike.
public abstract class BoxOfficeTests : IAsyncDisposable
{
    private readonly ITestFacilities _fakes;
    private readonly Features _features;

    protected BoxOfficeTests(ITestFacilities facilities)
    {
        _fakes = facilities;
        _features = ServiceFeatures.Over(facilities);
    }

    public virtual async ValueTask DisposeAsync()
    {
        await _features.DisposeAsync();
        if (_fakes is IAsyncDisposable disposable)
            await disposable.DisposeAsync();
    }

    [SkippableFact]
    public async Task PerformancesListsTheComingWeekendInOrder()
    {
        var result = await _features.Invoke(new Performances.Args(), Callers.Theatregoer);

        Assert.Equal(["thursday", "friday", "saturday-matinee", "saturday-evening"], result.Performances.Select(entry => entry.Performance.Id));
        Assert.Equal("Thursday 12 November, 7.30pm", result.Performances[0].Performance.When);
        Assert.Equal("Saturday 14 November, 2.30pm", result.Performances[2].Performance.When);
    }

    [SkippableFact]
    public async Task OnlyPerformancesWithSeatsLeftOfferThemAndASoldOutOneOffersTheReturnsQueue()
    {
        var entries = (await _features.Invoke(new Performances.Args(), Callers.Theatregoer)).Performances;

        var matinee = entries.Single(entry => entry.Performance.Id == "saturday-matinee");
        Assert.Null(matinee.LinkToBuySeats);
        Assert.Equal(new JoinReturnsQueue.Args("saturday-matinee"), matinee.LinkToJoinReturnsQueue);
        var friday = entries.Single(entry => entry.Performance.Id == "friday");
        Assert.Equal(new BuySeats.Args("friday", null), friday.LinkToBuySeats);
        Assert.Null(friday.LinkToJoinReturnsQueue);
    }

    [SkippableFact]
    public async Task OnlyStaffAreOfferedCancellingAndReleasingHouseSeats()
    {
        var theatregoer = (await _features.Invoke(new Performances.Args(), Callers.Theatregoer)).Performances;
        var staff = (await _features.Invoke(new Performances.Args(), Callers.Staff)).Performances;

        Assert.All(theatregoer, entry => Assert.Null(entry.LinkToCancel));
        Assert.All(theatregoer, entry => Assert.Null(entry.LinkToReleaseHouseSeats));
        Assert.All(staff, entry => Assert.NotNull(entry.LinkToCancel));
        Assert.Equal(["saturday-matinee"], staff.Where(entry => entry.LinkToReleaseHouseSeats is not null).Select(entry => entry.Performance.Id));
    }

    [SkippableFact]
    public async Task OnlyStaffMayCancelOrReleaseHouseSeats()
    {
        await Assert.ThrowsAsync<InvocationDeniedException>(() => _features.Invoke(new CancelPerformance.Args("friday"), Callers.Theatregoer));
        await Assert.ThrowsAsync<InvocationDeniedException>(() => _features.Invoke(new ReleaseHouseSeats.Args("saturday-matinee"), Callers.Theatregoer));
    }

    [SkippableFact]
    public async Task BuyingSeatsAsksHowManyAndHandsTheLinkBackToFillIn()
    {
        var link = (await Entry("saturday-evening", Callers.Theatregoer)).LinkToBuySeats!;

        var needs = Assert.IsType<BuySeats.Result.NeedsDetails>(await _features.Invoke(link, Callers.Theatregoer));

        Assert.Equal(new[] { "Seats" }, needs.Missing);
        Assert.IsType<BuySeats.Result.Bought>(await _features.Invoke(needs.LinkToBuySeats with { Seats = 2 }, Callers.Theatregoer));
    }

    [SkippableFact]
    public async Task BoughtSeatsAreAmongTheTheatregoersTickets()
    {
        var link = (await Entry("saturday-evening", Callers.Theatregoer)).LinkToBuySeats!;

        var bought = Assert.IsType<BuySeats.Result.Bought>(await _features.Invoke(link with { Seats = 3 }, Callers.Theatregoer));

        var tickets = (await _features.Invoke(bought.LinkToYourTickets, Callers.Theatregoer)).Tickets;
        Assert.Equal(["B-000001", bought.Booking.Reference], tickets.Select(ticket => ticket.Booking.Reference));
        Assert.Equal(48, (await Entry("saturday-evening", Callers.Theatregoer)).Performance.SeatsLeft);
    }

    [SkippableFact]
    public async Task AskingForMoreSeatsThanAreLeftOffersThoseThatAre()
    {
        var link = (await Entry("friday", Callers.Theatregoer)).LinkToBuySeats!;

        var result = Assert.IsType<BuySeats.Result.NotEnoughSeats>(await _features.Invoke(link with { Seats = 4 }, Callers.Theatregoer));

        Assert.Equal(2, result.SeatsLeft);
        Assert.IsType<BuySeats.Result.Bought>(await _features.Invoke(result.LinkToBuySeatsLeft, Callers.Theatregoer));
        var friday = await Entry("friday", Callers.Theatregoer);
        Assert.Null(friday.LinkToBuySeats);
        Assert.NotNull(friday.LinkToJoinReturnsQueue);
    }

    [SkippableFact]
    public async Task AStaleLinkToBuyTheLastSeatsOffersTheReturnsQueue()
    {
        var mine = (await Entry("friday", Callers.Theatregoer)).LinkToBuySeats!;
        var theirs = (await Entry("friday", Callers.OtherTheatregoer)).LinkToBuySeats!;
        await _features.Invoke(theirs with { Seats = 2 }, Callers.OtherTheatregoer);

        var result = await _features.Invoke(mine with { Seats = 2 }, Callers.Theatregoer);

        Assert.Equal(new BuySeats.Result.SoldOut(new JoinReturnsQueue.Args("friday")), result);
    }

    [SkippableFact]
    public async Task TwoTheatregoersBuyingTheLastSeatsAtOnceDontBothGetThem()
    {
        var sales = await Task.WhenAll(
            _features.Invoke(new BuySeats.Args("friday", 2), Callers.Theatregoer),
            _features.Invoke(new BuySeats.Args("friday", 2), Callers.OtherTheatregoer));

        Assert.Single(sales, sale => sale is BuySeats.Result.Bought);
        Assert.Equal(0, (await Entry("friday", Callers.Theatregoer)).Performance.SeatsLeft);
    }

    [SkippableFact]
    public async Task ATheatregoerWaitingForReturnsIsOfferedSeatsWhenTheHouseSeatsAreReleased()
    {
        var join = (await Entry("saturday-matinee", Callers.Theatregoer)).LinkToJoinReturnsQueue!;
        Assert.IsType<JoinReturnsQueue.Result.Joined>(await _features.Invoke(join, Callers.Theatregoer));
        var waiting = Assert.Single((await _features.Invoke(new YourTickets.Args(), Callers.Theatregoer)).ReturnsQueues);
        Assert.Null(waiting.LinkToBuySeats);
        Assert.Null((await Entry("saturday-matinee", Callers.Theatregoer)).LinkToJoinReturnsQueue);

        var release = (await Entry("saturday-matinee", Callers.Staff)).LinkToReleaseHouseSeats!;
        var released = Assert.IsType<ReleaseHouseSeats.Result.Released>(await _features.Invoke(release, Callers.Staff));

        Assert.Equal(4, released.Performance.SeatsLeft);
        waiting = Assert.Single((await _features.Invoke(new YourTickets.Args(), Callers.Theatregoer)).ReturnsQueues);
        Assert.Equal(new BuySeats.Args("saturday-matinee", null), waiting.LinkToBuySeats);
        Assert.Null((await Entry("saturday-matinee", Callers.Staff)).LinkToReleaseHouseSeats);
    }

    [SkippableFact]
    public async Task BuyingSeatsEndsTheTheatregoersWaitForReturns()
    {
        await _features.Invoke(new JoinReturnsQueue.Args("saturday-matinee"), Callers.Theatregoer);
        await _features.Invoke(new ReleaseHouseSeats.Args("saturday-matinee"), Callers.Staff);

        Assert.IsType<BuySeats.Result.Bought>(await _features.Invoke(new BuySeats.Args("saturday-matinee", 2), Callers.Theatregoer));

        var yours = await _features.Invoke(new YourTickets.Args(), Callers.Theatregoer);
        Assert.Empty(yours.ReturnsQueues);
        Assert.Contains(yours.Tickets, ticket => ticket.Performance.Id == "saturday-matinee");
    }

    [SkippableFact]
    public async Task JoiningTheQueueTwiceKeepsOnePlace()
    {
        await _features.Invoke(new JoinReturnsQueue.Args("saturday-matinee"), Callers.Theatregoer);

        Assert.IsType<JoinReturnsQueue.Result.AlreadyQueuing>(
            await _features.Invoke(new JoinReturnsQueue.Args("saturday-matinee"), Callers.Theatregoer));
        Assert.Single((await _features.Invoke(new YourTickets.Args(), Callers.Theatregoer)).ReturnsQueues);
    }

    [SkippableFact]
    public async Task ThereIsNoQueueForAPerformanceWithSeats()
    {
        var result = await _features.Invoke(new JoinReturnsQueue.Args("thursday"), Callers.Theatregoer);

        Assert.Equal(new JoinReturnsQueue.Result.SeatsAvailable(38, new BuySeats.Args("thursday", null)), result);
    }

    [SkippableFact]
    public async Task ACancelledPerformanceOffersNothingButOtherPerformances()
    {
        var cancel = (await Entry("friday", Callers.Staff)).LinkToCancel!;
        var stale = (await Entry("friday", Callers.Theatregoer)).LinkToBuySeats!;

        Assert.IsType<CancelPerformance.Result.Cancelled>(await _features.Invoke(cancel, Callers.Staff));

        var friday = await Entry("friday", Callers.Staff);
        Assert.True(friday.Performance.Cancelled);
        Assert.Equal(new BuySeats.Result.PerformanceCancelled(new Performances.Args()), await _features.Invoke(stale with { Seats = 1 }, Callers.Theatregoer));
        Assert.IsType<CancelPerformance.Result.AlreadyCancelled>(await _features.Invoke(cancel, Callers.Staff));
        Assert.Null(friday.LinkToBuySeats);
        Assert.Null(friday.LinkToCancel);
    }

    [SkippableFact]
    public async Task BuyingSeatsForNoSuchPerformanceFindsNothing()
    {
        Assert.Equal(new BuySeats.Result.NotFound("sunday"), await _features.Invoke(new BuySeats.Args("sunday", 2), Callers.Theatregoer));
    }

    [SkippableFact]
    public async Task ResettingTheDemoStartsItsStoryAgain()
    {
        await _features.Invoke(new ReleaseHouseSeats.Args("saturday-matinee"), Callers.Staff);
        await _features.Invoke(new BuySeats.Args("friday", 2), Callers.Theatregoer);

        await _features.Invoke(new ResetDemo.Args(), Callers.Theatregoer);

        Assert.Equal(0, (await Entry("saturday-matinee", Callers.Theatregoer)).Performance.SeatsLeft);
        Assert.Equal(2, (await Entry("friday", Callers.Theatregoer)).Performance.SeatsLeft);
        Assert.Single((await _features.Invoke(new YourTickets.Args(), Callers.Theatregoer)).Tickets);
    }

    [SkippableFact]
    public async Task EachSandboxHasABoxOfficeOfItsOwn()
    {
        await _features.Invoke(new BuySeats.Args("friday", 2), Callers.Theatregoer.In("a"));

        Assert.Equal(0, (await Entry("friday", Callers.Theatregoer.In("a"))).Performance.SeatsLeft);
        Assert.Equal(2, (await Entry("friday", Callers.Theatregoer.In("b"))).Performance.SeatsLeft);
    }

    private async Task<Performances.Entry> Entry(string performance, ClaimsPrincipal caller) =>
        (await _features.Invoke(new Performances.Args(), caller)).Performances.Single(entry => entry.Performance.Id == performance);
}

public class InMemoryBoxOfficeTests() : BoxOfficeTests(new Fakes(Local.Registrations.AddFacilities));

public class DynamoBoxOfficeTests() : BoxOfficeTests(new DynamoLocal(Aws.Registrations.AddFacilities));
