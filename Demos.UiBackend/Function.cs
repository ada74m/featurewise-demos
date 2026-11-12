using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Demos.BoxOffice;
using Demos.Functions;
using Demos.LostProperty;
using Featurewise;
using Microsoft.Extensions.DependencyInjection;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Demos.UiBackend;

// The demos' UI backend: the features a visitor can invoke, each with its rule, over the shared UiBackend.
public class Function
{
    private readonly Functions.UiBackend _backend;

    public Function() : this(Aws.Registrations.Register)
    {
    }

    public Function(Action<IServiceCollection> registerDependencies)
    {
        _backend = new Functions.UiBackend(Service.Id, features => features
            // Every demo.
            .RegisterFeature<ResetDemo, AnySignedInCaller<ResetDemo.Args>>()

            // The lost property office.
            .RegisterFeature<UnclaimedItems, AnySignedInCaller<UnclaimedItems.Args>>()
            .RegisterFeature<ItemsFoundAt, AnySignedInCaller<ItemsFoundAt.Args>>()
            .RegisterFeature<DonatedToCharity, AnySignedInCaller<DonatedToCharity.Args>>()
            .RegisterFeature<YourLostReports, AnySignedInCaller<YourLostReports.Args>>()
            .RegisterFeature<HandInItem, HandInItem.Rule>()
            .RegisterFeature<ReportLostItem, AnySignedInCaller<ReportLostItem.Args>>()
            .RegisterFeature<ClaimItem, AnySignedInCaller<ClaimItem.Args>>()

            // The theatre box office.
            .RegisterFeature<Performances, AnySignedInCaller<Performances.Args>>()
            .RegisterFeature<YourTickets, AnySignedInCaller<YourTickets.Args>>()
            .RegisterFeature<BuySeats, AnySignedInCaller<BuySeats.Args>>()
            .RegisterFeature<JoinReturnsQueue, AnySignedInCaller<JoinReturnsQueue.Args>>()
            .RegisterFeature<CancelPerformance, StaffOnly<CancelPerformance.Args>>()
            .RegisterFeature<ReleaseHouseSeats, StaffOnly<ReleaseHouseSeats.Args>>(),
            registerDependencies);
    }

    public Task<APIGatewayHttpApiV2ProxyResponse> Handle(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context) =>
        _backend.Handle(request, context);
}
