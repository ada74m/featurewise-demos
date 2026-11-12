using Demos.BoxOffice;
using Demos.Testing;
using Featurewise;

namespace Demos.Tests;

// A standalone Features over the fakes, with every demo's features, for testing them without an entrypoint.
public static class ServiceFeatures
{
    public static Features Over(ITestFacilities fakes) =>
        new Features(Service.Id)
            .RegisterFeature<ResetDemo, AnySignedInCaller<ResetDemo.Args>>()
            .RegisterFeature<Performances, AnySignedInCaller<Performances.Args>>()
            .RegisterFeature<YourTickets, AnySignedInCaller<YourTickets.Args>>()
            .RegisterFeature<BuySeats, AnySignedInCaller<BuySeats.Args>>()
            .RegisterFeature<JoinReturnsQueue, AnySignedInCaller<JoinReturnsQueue.Args>>()
            .RegisterFeature<CancelPerformance, StaffOnly<CancelPerformance.Args>>()
            .RegisterFeature<ReleaseHouseSeats, StaffOnly<ReleaseHouseSeats.Args>>()
            .RegisterDependencies(fakes.Register)
            .Build();
}
