using Amazon.DynamoDBv2;
using Demos.Aws;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Demos.BoxOffice.Aws;

// The box office's facilities on AWS, as every function registers them, configured by its environment.
public static class Registrations
{
    public static void Register(IServiceCollection services)
    {
        // Lambda's logger writes each entry as it's logged, so nothing is lost when the function freezes.
        services.AddLogging(logging => logging.AddLambdaLogger(new LambdaLoggerOptions { IncludeScopes = true }));
        CommonFacilities.Add(services, new AmazonDynamoDBClient(), Settings(), TimeProvider.System);
        AddFacilities(services);
    }

    public static DynamoSettings Settings() => new(Environment.GetEnvironmentVariable("DEMOS_TABLE") is { Length: > 0 } table
        ? table
        : throw new InvalidOperationException("the environment variable DEMOS_TABLE isn't set"));

    // The box office's own facilities, over the common ones, as the functions and the tests against
    // DynamoDB Local register them.
    public static void AddFacilities(IServiceCollection services)
    {
        services.AddScoped<IPerformances, DynamoPerformances>();
        services.AddScoped<IBookings, DynamoBookings>();
        services.AddScoped<IReturnsQueue, DynamoReturnsQueue>();
    }
}
