using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Demos.Aws;
using Demos.Local;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Demos.Tests;

// The AWS facilities over DynamoDB Local, each test in a table of its own, made as the demos' Terraform
// makes it. The tests that use it are skipped unless DYNAMODB_LOCAL names its endpoint, such as
// http://localhost:8000, after docker run -p 8000:8000 amazon/dynamodb-local.
public sealed class DynamoLocal : ITestFacilities, IAsyncDisposable
{
    private readonly AmazonDynamoDBClient _dynamo;
    private readonly DynamoSettings _settings = new($"demos-test-{Guid.NewGuid():N}");
    private bool _created;

    public DynamoLocal()
    {
        var endpoint = Environment.GetEnvironmentVariable("DYNAMODB_LOCAL");
        Skip.If(string.IsNullOrEmpty(endpoint), "DYNAMODB_LOCAL isn't set");
        _dynamo = new AmazonDynamoDBClient(new BasicAWSCredentials("local", "local"), new AmazonDynamoDBConfig { ServiceURL = endpoint });
    }

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 11, 5, 9, 0, 0, TimeSpan.Zero));

    public void Register(IServiceCollection services)
    {
        CreateTable().GetAwaiter().GetResult();
        Aws.Registrations.AddFacilities(services, _dynamo, _settings, Clock);
    }

    public async Task<IReadOnlyList<OutboxRecord>> TakeEvents()
    {
        var page = await _dynamo.ScanAsync(new ScanRequest
        {
            TableName = _settings.TableName,
            FilterExpression = "begins_with(#key, :outbox)",
            ExpressionAttributeNames = new() { ["#key"] = "Key" },
            ExpressionAttributeValues = new() { [":outbox"] = new AttributeValue { S = "outbox#" } },
            ConsistentRead = true
        });
        var records = new List<OutboxRecord>();
        foreach (var row in page.Items ?? [])
        {
            records.Add(new OutboxRecord(row["Key"].S, row["Sandbox"].S, row["Event"].S, row["Body"].S,
                row.TryGetValue("CausedBy", out var cause) ? cause.S : null));
            await _dynamo.DeleteItemAsync(_settings.TableName, new() { ["Sandbox"] = row["Sandbox"], ["Key"] = row["Key"] });
        }
        return records;
    }

    public Task<IReadOnlyList<string>> Sandboxes() => new DynamoSandboxes(_dynamo, Clock, _settings).Live(CancellationToken.None);

    public async ValueTask DisposeAsync()
    {
        if (_created)
            await _dynamo.DeleteTableAsync(_settings.TableName);
        _dynamo.Dispose();
    }

    private async Task CreateTable()
    {
        if (_created)
            return;
        await _dynamo.CreateTableAsync(new CreateTableRequest
        {
            TableName = _settings.TableName,
            BillingMode = BillingMode.PAY_PER_REQUEST,
            KeySchema = [new("Sandbox", KeyType.HASH), new("Key", KeyType.RANGE)],
            AttributeDefinitions = [new("Sandbox", ScalarAttributeType.S), new("Key", ScalarAttributeType.S)]
        });
        _created = true;
    }
}
