using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Demos.BoxOffice;
using Demos.Functions;
using Featurewise;
using Microsoft.Extensions.DependencyInjection;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Demos.UiBackend;

// The box office's UI backend, still the demos service until it becomes a service of its own: the
// features a visitor can invoke, each with its rule.
public class Function
{
    private readonly Functions.UiBackend _backend;

    public Function() : this(Aws.Registrations.Register)
    {
    }

    public Function(Action<IServiceCollection> registerDependencies)
    {
        _backend = new Functions.UiBackend(Service.Id, features => features
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
