using Microsoft.Extensions.DependencyInjection;

namespace Demos.Local;

// What every demo's facilities in memory stand on: the store, the outbox, the clock, and each
// invocation's unit of work, events, demo data and sandboxed tables.
public static class CommonFacilities
{
    public static void Add(IServiceCollection services, InMemoryStore store, InMemoryOutbox outbox, TimeProvider clock)
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
    }
}
