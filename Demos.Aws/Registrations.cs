using Amazon.DynamoDBv2;
using Demos.Aws.BoxOffice;
using Demos.Aws.LostProperty;
using Demos.BoxOffice;
using Demos.LostProperty;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Demos.Aws;

// The core's facilities on AWS, as every function registers them, configured by its environment.
public static class Registrations
{
    public static void Register(IServiceCollection services) => AddFacilities(services, new AmazonDynamoDBClient(), Settings());

    public static DynamoSettings Settings() => new(Setting("DEMOS_TABLE"));

    // The facilities over a given client and table, as the functions and the tests against DynamoDB Local
    // register them.
    public static void AddFacilities(IServiceCollection services, IAmazonDynamoDB dynamo, DynamoSettings settings, TimeProvider? clock = null)
    {
        // Lambda's logger writes each entry as it's logged, so nothing is lost when the function freezes.
        services.AddLogging(logging => logging.AddLambdaLogger(new LambdaLoggerOptions { IncludeScopes = true }));

        CommonFacilities.Add(services, dynamo, settings, clock ?? TimeProvider.System);

        services.AddScoped<IFoundItems, DynamoFoundItems>();
        services.AddScoped<ILostReports, DynamoLostReports>();

        services.AddScoped<IPerformances, DynamoPerformances>();
        services.AddScoped<IBookings, DynamoBookings>();
        services.AddScoped<IReturnsQueue, DynamoReturnsQueue>();
    }

    private static string Setting(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"the environment variable {name} isn't set");
}
