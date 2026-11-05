using Demos.Local;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Demos.Tests;

// The local host's store, outbox and a clock the tests control, registered in place of AWS's.
public class Fakes
{
    public InMemoryStore Store { get; } = new();

    public InMemoryOutbox Outbox { get; } = new();

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 11, 5, 9, 0, 0, TimeSpan.Zero));

    public void Register(IServiceCollection services) => Registrations.AddFacilities(services, Store, Outbox, Clock);
}
