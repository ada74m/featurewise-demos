namespace Demos.Aws;

// Where the demos' data is, and how long a sandbox's data lasts after its first use.
public record DynamoSettings(string TableName)
{
    public TimeSpan SandboxLifetime { get; init; } = TimeSpan.FromDays(7);

    // How long an event waits in the outbox for the stream, which delivers it within a day at worst.
    public TimeSpan OutboxLifetime { get; init; } = TimeSpan.FromDays(1);
}
