using Demos.Local;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Demos.Testing;

// The local host's store, outbox and a clock the tests control, registered in place of AWS's, with a
// demo's own facilities in memory.
public class Fakes(Action<IServiceCollection> demoFacilities) : ITestFacilities
{
    public InMemoryStore Store { get; } = new();

    public InMemoryOutbox Outbox { get; } = new();

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 11, 5, 9, 0, 0, TimeSpan.Zero));

    public void Register(IServiceCollection services)
    {
        CommonFacilities.Add(services, Store, Outbox, Clock);
        demoFacilities(services);
    }

    public Task<IReadOnlyList<OutboxRecord>> TakeEvents() => Task.FromResult(Outbox.Take());

    public Task<IReadOnlyList<string>> Sandboxes() => Task.FromResult(Store.Sandboxes);
}
