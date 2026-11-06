using Demos.BoxOffice;
using Demos.Local.BoxOffice;
using Demos.Local.LostProperty;
using Demos.LostProperty;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Demos.Local;

// The core's facilities in memory, as every function the local host runs registers them. The functions
// share one store and one outbox, as they share one table in AWS.
public static class Registrations
{
    public static Action<IServiceCollection> Over(InMemoryStore store, InMemoryOutbox outbox) => services =>
    {
        services.AddLogging(logging => logging.AddSimpleConsole(console => console.IncludeScopes = true));
        AddFacilities(services, store, outbox, TimeProvider.System);
    };

    // The facilities alone, without the console logging, as the tests register them with a clock of their own.
    public static void AddFacilities(IServiceCollection services, InMemoryStore store, InMemoryOutbox outbox, TimeProvider clock)
    {
        services.AddSingleton(store);
        services.AddSingleton(outbox);
        services.AddSingleton(clock);

        // Each invocation has its own unit of work, which its facilities stage their writes in, and sees
        // only its sandbox's tables.
        services.AddScoped<InMemoryChanges>();
        services.AddScoped<IChanges>(provider => provider.GetRequiredService<InMemoryChanges>());
        services.AddScoped<SandboxedTables>();
        services.AddScoped<IEvents, InMemoryEvents>();
        services.AddScoped<IDemoData, InMemoryDemoData>();

        services.AddScoped<IFoundItems, InMemoryFoundItems>();
        services.AddScoped<ILostReports, InMemoryLostReports>();

        services.AddScoped<IPerformances, InMemoryPerformances>();
        services.AddScoped<IBookings, InMemoryBookings>();
        services.AddScoped<IReturnsQueue, InMemoryReturnsQueue>();
    }
}
