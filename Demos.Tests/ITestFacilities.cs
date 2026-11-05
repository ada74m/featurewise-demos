using Demos.Local;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Demos.Tests;

// Facilities for tests that must hold of every implementation, in memory and on DynamoDB alike.
public interface ITestFacilities
{
    FakeTimeProvider Clock { get; }

    void Register(IServiceCollection services);

    // The events committed so far, taken out of the outbox.
    Task<IReadOnlyList<OutboxRecord>> TakeEvents();

    Task<IReadOnlyList<string>> Sandboxes();
}
