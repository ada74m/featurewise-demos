using Demos.Testing;
using Featurewise;

namespace Demos.LostProperty.Tests;

// The lost property office's features, invoked as its traveller and clerk would, following the links
// they're given, over facilities in memory and on DynamoDB Local alike.
public abstract class LostPropertyTests : IAsyncDisposable
{
    private readonly ITestFacilities _fakes;
    private readonly Features _features;

    protected LostPropertyTests(ITestFacilities facilities)
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
    public async Task UnclaimedItemsListsTheSeedOldestFirst()
    {
        var result = await _features.Invoke(new UnclaimedItems.Args(), Callers.Traveller);

        Assert.Equal(["F-000006", "F-000005", "F-000004", "F-000003", "F-000002", "F-000001"], result.Items.Select(entry => entry.Item.Id));
    }

    [SkippableFact]
    public async Task OnlyClerksAreOfferedTheLinkToHandAnItemIn()
    {
        Assert.Null((await _features.Invoke(new UnclaimedItems.Args(), Callers.Traveller)).LinkToHandIn);
        Assert.NotNull((await _features.Invoke(new UnclaimedItems.Args(), Callers.Clerk)).LinkToHandIn);
    }

    [SkippableFact]
    public async Task OnlyClerksMayHandAnItemIn()
    {
        await Assert.ThrowsAsync<InvocationDeniedException>(() =>
            _features.Invoke(new HandInItem.Args("Umbrella", "Black", "York"), Callers.Traveller));
    }

    [SkippableFact]
    public async Task EachUnclaimedItemLeadsToTheOthersFoundAtItsStation()
    {
        var unclaimed = await _features.Invoke(new UnclaimedItems.Args(), Callers.Traveller);
        var crewe = unclaimed.Items.Single(entry => entry.Item.Id == "F-000001").LinkToStation;

        var result = await _features.Invoke(crewe, Callers.Traveller);

        Assert.Equal(["F-000004", "F-000001"], result.Items.Select(item => item.Id));
    }

    [SkippableFact]
    public async Task HandingInAnItemNeedsEveryDetail()
    {
        var link = (await _features.Invoke(new UnclaimedItems.Args(), Callers.Clerk)).LinkToHandIn!;

        var result = await _features.Invoke(link with { Category = "Umbrella" }, Callers.Clerk);

        var needs = Assert.IsType<HandInItem.Result.NeedsDetails>(result);
        Assert.Equal(new[] { "Description", "Station" }, needs.Missing);
        Assert.Equal("Umbrella", needs.LinkToHandIn.Category);
        Assert.IsType<HandInItem.Result.HandedIn>(
            await _features.Invoke(needs.LinkToHandIn with { Description = "Black", Station = "York" }, Callers.Clerk));
    }

    [SkippableFact]
    public async Task AHandedInItemWaitsToBeClaimedAndRaisesItemHandedIn()
    {
        var result = await HandInUmbrella();

        var unclaimed = await _features.Invoke(new UnclaimedItems.Args(), Callers.Traveller);
        Assert.Contains(unclaimed.Items, entry => entry.Item.Id == result.Item.Id);
        var record = Assert.Single(await _fakes.TakeEvents());
        Assert.Equal(nameof(ItemHandedIn), record.Event);
        Assert.Contains(result.Item.Id, record.Body);
    }

    [SkippableFact]
    public async Task AHandedInItemIsOfferedToTheTravellerWhoReportedLosingOneLikeIt()
    {
        var handedIn = await HandInUmbrella();

        await _features.Invoke(new HandleItemHandedIn.Args(handedIn.Item.Id), Callers.Job);

        var report = Assert.Single((await _features.Invoke(new YourLostReports.Args(), Callers.Traveller)).Reports);
        Assert.Equal(handedIn.Item.Id, report.PossibleMatch?.Id);
        Assert.Equal(new ClaimItem.Args(handedIn.Item.Id, "R-000001"), report.LinkToClaim);
    }

    [SkippableFact]
    public async Task MatchingAnItemTwiceChangesNothingMore()
    {
        var handedIn = await HandInUmbrella();

        await _features.Invoke(new HandleItemHandedIn.Args(handedIn.Item.Id), Callers.Job);
        await _features.Invoke(new HandleItemHandedIn.Args(handedIn.Item.Id), Callers.Job);

        var report = Assert.Single((await _features.Invoke(new YourLostReports.Args(), Callers.Traveller)).Reports);
        Assert.Equal(handedIn.Item.Id, report.PossibleMatch?.Id);
    }

    [SkippableFact]
    public async Task FollowingTheClaimLinkHandsTheItemOverOnce()
    {
        var handedIn = await HandInUmbrella();
        await _features.Invoke(new HandleItemHandedIn.Args(handedIn.Item.Id), Callers.Job);
        var claim = (await _features.Invoke(new YourLostReports.Args(), Callers.Traveller)).Reports[0].LinkToClaim!;

        var claimed = Assert.IsType<ClaimItem.Result.Claimed>(await _features.Invoke(claim, Callers.Traveller));
        var again = await _features.Invoke(claim, Callers.Traveller);

        Assert.Equal("traveller", claimed.Item.ClaimedBy);
        Assert.IsType<ClaimItem.Result.AlreadyClaimed>(again);
        Assert.Null((await _features.Invoke(new YourLostReports.Args(), Callers.Traveller)).Reports[0].LinkToClaim);
    }

    [SkippableFact]
    public async Task AClaimWithAReportOfSomethingElseDoesntMatch()
    {
        var claim = new ClaimItem.Args("F-000002", "R-000001");

        var result = await _features.Invoke(claim, Callers.Traveller);

        Assert.Equal(new ClaimItem.Result.DoesntMatchReport("F-000002", "R-000001", claim, new YourLostReports.Args()), result);
    }

    [SkippableFact]
    public async Task AClaimThatDoesntMatchCanBeTriedAgainWithTheRightReport()
    {
        var handedIn = await HandInUmbrella();
        var reported = Assert.IsType<ReportLostItem.Result.Reported>(
            await _features.Invoke(new ReportLostItem.Args("Hat", "A bowler"), Callers.Traveller));

        var wrong = Assert.IsType<ClaimItem.Result.DoesntMatchReport>(
            await _features.Invoke(new ClaimItem.Args(handedIn.Item.Id, reported.Report.Reference), Callers.Traveller));

        var reports = await _features.Invoke(wrong.LinkToYourReports, Callers.Traveller);
        var umbrella = reports.Reports.Single(report => report.Report.Category == "Umbrella").Report.Reference;
        Assert.IsType<ClaimItem.Result.Claimed>(await _features.Invoke(wrong.LinkToClaim with { Reference = umbrella }, Callers.Traveller));
    }

    [SkippableFact]
    public async Task AClaimWithSomeoneElsesReportDoesntMatch()
    {
        var handedIn = await HandInUmbrella();

        var result = await _features.Invoke(new ClaimItem.Args(handedIn.Item.Id, "R-000001"), Callers.OtherTraveller);

        Assert.IsType<ClaimItem.Result.DoesntMatchReport>(result);
    }

    [SkippableFact]
    public async Task ClaimingAnItemThatIsntThereFindsNothing()
    {
        Assert.Equal(
            new ClaimItem.Result.NotFound("F-999999"),
            await _features.Invoke(new ClaimItem.Args("F-999999", "R-000001"), Callers.Traveller));
    }

    [SkippableFact]
    public async Task TheCharityCollectsWhatHasWaitedThreeMonths()
    {
        await _features.Invoke(new UnclaimedItems.Args(), Callers.Traveller);   // the visitor arrives, and the office is seeded

        await _features.Invoke(new HandleCharityCollectionDue.Args(), Callers.Job);

        var donated = await _features.Invoke(new DonatedToCharity.Args(), Callers.Traveller);
        Assert.Equal(["F-000006", "F-000005"], donated.Items.Select(item => item.Id));
        var unclaimed = await _features.Invoke(new UnclaimedItems.Args(), Callers.Traveller);
        Assert.DoesNotContain(unclaimed.Items, entry => entry.Item.Status == ItemStatus.DonatedToCharity);
        Assert.Equal(4, unclaimed.Items.Count);
    }

    [SkippableFact]
    public async Task AnItemTheCharityTookCantBeClaimed()
    {
        await _features.Invoke(new HandleCharityCollectionDue.Args(), Callers.Job);

        Assert.IsType<ClaimItem.Result.NotFound>(await _features.Invoke(new ClaimItem.Args("F-000006", "R-000001"), Callers.Traveller));
    }

    [SkippableFact]
    public async Task AnItemWaitsLongerAsTimePasses()
    {
        _fakes.Clock.Advance(TimeSpan.FromDays(20));   // the seed is dated from the visitor's arrival, after this
        await _features.Invoke(new UnclaimedItems.Args(), Callers.Traveller);
        _fakes.Clock.Advance(TimeSpan.FromDays(6));    // the false teeth have now waited 91 days, within the sandbox's week

        await _features.Invoke(new HandleCharityCollectionDue.Args(), Callers.Job);

        Assert.Equal(3, (await _features.Invoke(new DonatedToCharity.Args(), Callers.Traveller)).Items.Count);
    }

    [SkippableFact]
    public async Task AReportedLossIsAmongTheTravellersReports()
    {
        var link = (await _features.Invoke(new YourLostReports.Args(), Callers.OtherTraveller)).LinkToReport;

        var reported = Assert.IsType<ReportLostItem.Result.Reported>(
            await _features.Invoke(link with { Category = "Hat", Description = "A bowler" }, Callers.OtherTraveller));

        var report = Assert.Single((await _features.Invoke(reported.LinkToYourReports, Callers.OtherTraveller)).Reports);
        Assert.Equal("A bowler", report.Report.Description);
        Assert.Null(report.PossibleMatch);
    }

    [SkippableFact]
    public async Task ALossReportedAfterTheItemWasHandedInIsMatchedAtOnce()
    {
        var handedIn = await HandInUmbrella();

        var mayHave = Assert.IsType<ReportLostItem.Result.MayAlreadyHaveIt>(await _features.Invoke(
            new ReportLostItem.Args("Umbrella", "Black, with a duck's head handle"), Callers.OtherTraveller));

        Assert.Equal(handedIn.Item.Id, mayHave.Item.Id);
        Assert.Equal(new ClaimItem.Args(handedIn.Item.Id, mayHave.Report.Reference), mayHave.LinkToClaim);
        var report = Assert.Single((await _features.Invoke(mayHave.LinkToYourReports, Callers.OtherTraveller)).Reports);
        Assert.Equal(handedIn.Item.Id, report.PossibleMatch?.Id);
        Assert.IsType<ClaimItem.Result.Claimed>(await _features.Invoke(mayHave.LinkToClaim, Callers.OtherTraveller));
    }

    [SkippableFact]
    public async Task AReportedLossIsMatchedToTheItemHandedInFirst()
    {
        await _features.Invoke(new HandInItem.Args("Glove", "One right glove, red wool", "Crewe"), Callers.Clerk);

        var mayHave = Assert.IsType<ReportLostItem.Result.MayAlreadyHaveIt>(
            await _features.Invoke(new ReportLostItem.Args("Glove", "Red wool"), Callers.OtherTraveller));

        Assert.Equal("F-000001", mayHave.Item.Id);
    }

    [SkippableFact]
    public async Task AReportWhoseMatchWentToSomeoneElseIsMatchedAgain()
    {
        var first = await HandInUmbrella();
        await _features.Invoke(new HandleItemHandedIn.Args(first.Item.Id), Callers.Job);
        var theirs = Assert.IsType<ReportLostItem.Result.MayAlreadyHaveIt>(await _features.Invoke(
            new ReportLostItem.Args("Umbrella", "Green, folding"), Callers.OtherTraveller));
        Assert.IsType<ClaimItem.Result.Claimed>(await _features.Invoke(theirs.LinkToClaim, Callers.OtherTraveller));

        var second = await HandInUmbrella();
        await _features.Invoke(new HandleItemHandedIn.Args(second.Item.Id), Callers.Job);

        var mine = Assert.Single((await _features.Invoke(new YourLostReports.Args(), Callers.Traveller)).Reports);
        Assert.Equal(second.Item.Id, mine.PossibleMatch?.Id);
        Assert.Equal(new ClaimItem.Args(second.Item.Id, "R-000001"), mine.LinkToClaim);
        var claimed = Assert.Single((await _features.Invoke(new YourLostReports.Args(), Callers.OtherTraveller)).Reports);
        Assert.Equal(first.Item.Id, claimed.PossibleMatch?.Id);
    }

    [SkippableFact]
    public async Task ReportingALossNeedsEveryDetail()
    {
        var result = await _features.Invoke(new ReportLostItem.Args(null, " "), Callers.Traveller);

        var needs = Assert.IsType<ReportLostItem.Result.NeedsDetails>(result);
        Assert.Equal(new[] { "Category", "Description" }, needs.Missing);
        Assert.IsType<ReportLostItem.Result.Reported>(
            await _features.Invoke(needs.LinkToReport with { Category = "Hat", Description = "A bowler" }, Callers.Traveller));
    }

    [SkippableFact]
    public async Task ResettingTheDemoStartsItsStoryAgain()
    {
        var handedIn = await HandInUmbrella();
        await _features.Invoke(new HandleItemHandedIn.Args(handedIn.Item.Id), Callers.Job);

        await _features.Invoke(new ResetDemo.Args(), Callers.Traveller);

        var unclaimed = await _features.Invoke(new UnclaimedItems.Args(), Callers.Clerk);
        Assert.DoesNotContain(unclaimed.Items, entry => entry.Item.Id == handedIn.Item.Id);
        Assert.Equal(6, unclaimed.Items.Count);
        Assert.Null(Assert.Single((await _features.Invoke(new YourLostReports.Args(), Callers.Traveller)).Reports).PossibleMatch);
    }

    [SkippableFact]
    public async Task ResettingTheDemoLeavesOtherSandboxesAlone()
    {
        var handedIn = await HandInUmbrella(Callers.Clerk.In("a"));

        await _features.Invoke(new ResetDemo.Args(), Callers.Traveller.In("b"));

        Assert.Contains((await _features.Invoke(new UnclaimedItems.Args(), Callers.Clerk.In("a"))).Items, entry => entry.Item.Id == handedIn.Item.Id);
    }

    [SkippableFact]
    public async Task AnItemFromBeforeAResetCantBeClaimed()
    {
        var handedIn = await HandInUmbrella();
        await _features.Invoke(new HandleItemHandedIn.Args(handedIn.Item.Id), Callers.Job);
        await _features.Invoke(new ResetDemo.Args(), Callers.Traveller);

        Assert.IsType<ClaimItem.Result.NotFound>(await _features.Invoke(new ClaimItem.Args(handedIn.Item.Id, "R-000001"), Callers.Traveller));
    }

    [SkippableFact]
    public async Task EachSandboxHasAnOfficeOfItsOwn()
    {
        var handedIn = await HandInUmbrella(Callers.Clerk.In("a"));

        var inA = await _features.Invoke(new UnclaimedItems.Args(), Callers.Traveller.In("a"));
        var inB = await _features.Invoke(new UnclaimedItems.Args(), Callers.Traveller.In("b"));

        Assert.Contains(inA.Items, entry => entry.Item.Id == handedIn.Item.Id);
        Assert.DoesNotContain(inB.Items, entry => entry.Item.Id == handedIn.Item.Id);
        Assert.Equal(["a", "b"], await _fakes.Sandboxes());
    }

    private async Task<HandInItem.Result.HandedIn> HandInUmbrella(System.Security.Claims.ClaimsPrincipal? clerk = null) =>
        Assert.IsType<HandInItem.Result.HandedIn>(await _features.Invoke(
            new HandInItem.Args("Umbrella", "Black, with a duck's head handle", "Crewe"), clerk ?? Callers.Clerk));
}

public class InMemoryLostPropertyTests() : LostPropertyTests(new Fakes(Local.Registrations.AddFacilities));

public class DynamoLostPropertyTests() : LostPropertyTests(new DynamoLocal(Aws.Registrations.AddFacilities));
