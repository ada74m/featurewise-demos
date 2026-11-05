using Demos.LostProperty;
using Featurewise;

namespace Demos.Tests;

// A standalone Features over the fakes, with every demo's features, for testing them without an entrypoint.
public static class ServiceFeatures
{
    public static Features Over(ITestFacilities fakes) =>
        new Features(Service.Id)
            .RegisterFeature<ResetDemo, AnySignedInCaller<ResetDemo.Args>>()
            .RegisterFeature<UnclaimedItems, AnySignedInCaller<UnclaimedItems.Args>>()
            .RegisterFeature<ItemsFoundAt, AnySignedInCaller<ItemsFoundAt.Args>>()
            .RegisterFeature<DonatedToCharity, AnySignedInCaller<DonatedToCharity.Args>>()
            .RegisterFeature<YourLostReports, AnySignedInCaller<YourLostReports.Args>>()
            .RegisterFeature<HandInItem, HandInItem.Rule>()
            .RegisterFeature<ReportLostItem, AnySignedInCaller<ReportLostItem.Args>>()
            .RegisterFeature<ClaimItem, AnySignedInCaller<ClaimItem.Args>>()
            .RegisterFeature<HandleItemHandedIn, AnySystemCaller<HandleItemHandedIn.Args>>()
            .RegisterFeature<HandleCharityCollectionDue, AnySystemCaller<HandleCharityCollectionDue.Args>>()
            .RegisterDependencies(fakes.Register)
            .Build();
}
