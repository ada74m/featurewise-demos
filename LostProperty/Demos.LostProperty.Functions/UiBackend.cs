using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Featurewise;
using Microsoft.Extensions.DependencyInjection;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Demos.LostProperty.Functions;

// The office's UI backend: the features a visitor can invoke, each with its rule.
public class UiBackend
{
    private readonly Demos.Functions.UiBackend _backend;

    public UiBackend() : this(Aws.Registrations.Register)
    {
    }

    public UiBackend(Action<IServiceCollection> registerDependencies)
    {
        _backend = new Demos.Functions.UiBackend(Service.Id, features => features
            .RegisterFeature<UnclaimedItems, AnySignedInCaller<UnclaimedItems.Args>>()
            .RegisterFeature<ItemsFoundAt, AnySignedInCaller<ItemsFoundAt.Args>>()
            .RegisterFeature<DonatedToCharity, AnySignedInCaller<DonatedToCharity.Args>>()
            .RegisterFeature<YourLostReports, AnySignedInCaller<YourLostReports.Args>>()
            .RegisterFeature<HandInItem, HandInItem.Rule>()
            .RegisterFeature<ReportLostItem, AnySignedInCaller<ReportLostItem.Args>>()
            .RegisterFeature<ClaimItem, AnySignedInCaller<ClaimItem.Args>>()
            .RegisterFeature<ResetDemo, AnySignedInCaller<ResetDemo.Args>>(),
            registerDependencies);
    }

    public Task<APIGatewayHttpApiV2ProxyResponse> Handle(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context) =>
        _backend.Handle(request, context);
}
