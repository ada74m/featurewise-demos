using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Featurewise;
using Microsoft.Extensions.DependencyInjection;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Demos.BoxOffice.Functions;

// The box office's UI backend: the features a visitor can invoke, each with its rule.
public class UiBackend
{
    private readonly Demos.Functions.UiBackend _backend;

    public UiBackend() : this(Aws.Registrations.Register)
    {
    }

    public UiBackend(Action<IServiceCollection> registerDependencies)
    {
        _backend = new Demos.Functions.UiBackend(Service.Id, features => features
            .RegisterFeature<ResetDemo, AnySignedInCaller<ResetDemo.Args>>()
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
