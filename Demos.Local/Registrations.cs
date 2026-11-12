using Demos.BoxOffice;
using Demos.Local.BoxOffice;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Demos.Local;

// The box office's facilities in memory, as the local host registers them.
public static class Registrations
{
    public static Action<IServiceCollection> Over(InMemoryStore store, InMemoryOutbox outbox) => services =>
    {
        services.AddLogging(logging => logging.AddSimpleConsole(console => console.IncludeScopes = true));
        CommonFacilities.Add(services, store, outbox, TimeProvider.System);
        AddFacilities(services);
    };

    // The box office's own facilities, over the common ones, as the tests register them with a clock of
    // their own.
    public static void AddFacilities(IServiceCollection services)
    {
        services.AddScoped<IPerformances, InMemoryPerformances>();
        services.AddScoped<IBookings, InMemoryBookings>();
        services.AddScoped<IReturnsQueue, InMemoryReturnsQueue>();
    }
}
