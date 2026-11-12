using Amazon.DynamoDBv2;
using Microsoft.Extensions.DependencyInjection;

namespace Demos.Aws;

// What every demo's facilities on AWS stand on: the client, the table, the clock, and each invocation's
// unit of work, events, demo data and sandboxed tables.
public static class CommonFacilities
{
    public static void Add(IServiceCollection services, IAmazonDynamoDB dynamo, DynamoSettings settings, TimeProvider clock)
    {
        services.AddSingleton(dynamo);
        services.AddSingleton(settings);
        services.AddSingleton(clock);

        // Each invocation has its own unit of work, which its facilities stage their writes in, and sees
        // only its sandbox's tables.
        services.AddScoped<DynamoChanges>();
        services.AddScoped<IChanges>(provider => provider.GetRequiredService<DynamoChanges>());
        services.AddScoped<DynamoTables>();
        services.AddScoped<IEvents, DynamoEvents>();
        services.AddScoped<IDemoData, DynamoDemoData>();
    }
}
