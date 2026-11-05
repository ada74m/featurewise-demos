using Amazon.DynamoDBv2;
using Demos.Aws.LostProperty;
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

        services.AddSingleton(dynamo);
        services.AddSingleton(settings);
        services.AddSingleton(clock ?? TimeProvider.System);

        // Each invocation has its own unit of work, which its facilities stage their writes in, and sees
        // only its sandbox's tables.
        services.AddScoped<DynamoChanges>();
        services.AddScoped<IChanges>(provider => provider.GetRequiredService<DynamoChanges>());
        services.AddScoped<DynamoTables>();
        services.AddScoped<IEvents, DynamoEvents>();

        services.AddScoped<IFoundItems, DynamoFoundItems>();
        services.AddScoped<ILostReports, DynamoLostReports>();
    }

    private static string Setting(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"the environment variable {name} isn't set");
}
