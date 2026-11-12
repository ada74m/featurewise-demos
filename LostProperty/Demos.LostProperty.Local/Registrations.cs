using Demos.Local;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Demos.LostProperty.Local;

// The office's facilities in memory, as every function the local host runs registers them. The functions
// share one store and one outbox, as they share one table in AWS.
public static class Registrations
{
    public static Action<IServiceCollection> Over(InMemoryStore store, InMemoryOutbox outbox) => services =>
    {
        services.AddLogging(logging => logging.AddSimpleConsole(console => console.IncludeScopes = true));
        CommonFacilities.Add(services, store, outbox, TimeProvider.System);
        AddFacilities(services);
    };

    // The office's own facilities, over the common ones, as the tests register them with a clock of their own.
    public static void AddFacilities(IServiceCollection services)
    {
        services.AddScoped<IFoundItems, InMemoryFoundItems>();
        services.AddScoped<ILostReports, InMemoryLostReports>();
    }
}
