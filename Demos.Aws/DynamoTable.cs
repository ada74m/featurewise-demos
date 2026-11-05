using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;

namespace Demos.Aws;

// One sandbox's copy of one of a demo's tables, in the demos' DynamoDB table. Each row holds its record as a
// map in Body, so conditions can test its fields, and expires with the sandbox's copy of the table, by
// DynamoDB's TTL on Expires. The copy is made from the demo's seed the first time the sandbox uses it,
// and a marker row, seeded#<table>, records when it expires; a later visit after that makes a fresh copy.
// DynamoDB can take a day or two to remove expired rows, so reads ignore them.
public class DynamoTable<T>(
    IAmazonDynamoDB dynamo,
    DynamoChanges changes,
    TimeProvider clock,
    DynamoSettings settings,
    string sandbox,
    string name,
    Func<IEnumerable<(string Key, T Value)>> seed) where T : notnull
{
    // The live sandboxes, which scheduled jobs run in, are rows in a partition of their own.
    public const string SandboxesPartition = "sandboxes";

    private long? _expires;

    public async Task<T?> Find(string key, CancellationToken cancellationToken)
    {
        await EnsureSeeded(cancellationToken);
        var response = await dynamo.GetItemAsync(
            new GetItemRequest { TableName = settings.TableName, Key = RowKey(key), ConsistentRead = true }, cancellationToken);
        return response.Item is { Count: > 0 } row && Live(row) ? Read(row) : default;
    }

    public async Task<IReadOnlyList<T>> All(CancellationToken cancellationToken)
    {
        await EnsureSeeded(cancellationToken);
        var records = new List<T>();
        Dictionary<string, AttributeValue>? startKey = null;
        do
        {
            var page = await dynamo.QueryAsync(new QueryRequest
            {
                TableName = settings.TableName,
                KeyConditionExpression = "Sandbox = :sandbox AND begins_with(#key, :prefix)",
                ExpressionAttributeNames = new() { ["#key"] = "Key" },
                ExpressionAttributeValues = new()
                {
                    [":sandbox"] = new AttributeValue { S = sandbox },
                    [":prefix"] = new AttributeValue { S = $"{name}#" }
                },
                ConsistentRead = true,
                ExclusiveStartKey = startKey
            }, cancellationToken);
            records.AddRange((page.Items ?? []).Where(Live).Select(Read));
            startKey = page.LastEvaluatedKey is { Count: > 0 } lastKey ? lastKey : null;
        } while (startKey is not null);

        return records;
    }

    // Stages a new row, on condition that the key is free.
    public void Add(string key, T value) =>
        Stage(key, value, new DynamoCondition("attribute_not_exists(#key)", new() { ["#key"] = "Key" }, []));

    // Stages replacing a row, on condition that its record still meets the condition.
    public void Replace(string key, T value, DynamoCondition condition) => Stage(key, value, condition);

    private void Stage(string key, T value, DynamoCondition condition)
    {
        changes.BeforeCommit(EnsureSeeded);
        changes.Stage(() => new TransactWriteItem
        {
            Put = new Put
            {
                TableName = settings.TableName,
                Item = Row(key, value, _expires!.Value),
                ConditionExpression = condition.Expression,
                ExpressionAttributeNames = condition.Names,
                ExpressionAttributeValues = condition.Values.Count == 0 ? null : condition.Values
            }
        });
    }

    // Makes the sandbox's copy of the table from the seed, unless it has a live one. The marker's condition
    // makes two invocations seeding at once agree: one writes, and the other finds the copy it made.
    private async Task EnsureSeeded(CancellationToken cancellationToken)
    {
        if (_expires is not null)
            return;

        var now = clock.GetUtcNow();
        var marker = await dynamo.GetItemAsync(
            new GetItemRequest { TableName = settings.TableName, Key = MarkerKey, ConsistentRead = true }, cancellationToken);
        if (marker.Item is { Count: > 0 } row && Live(row))
        {
            _expires = long.Parse(row["Expires"].N);
            return;
        }

        var expires = (now + settings.SandboxLifetime).ToUnixTimeSeconds();
        var writes = seed().Select(record => new TransactWriteItem
        {
            Put = new Put { TableName = settings.TableName, Item = Row(record.Key, record.Value, expires) }
        }).ToList();
        writes.Add(new TransactWriteItem
        {
            Put = new Put
            {
                TableName = settings.TableName,
                Item = new Dictionary<string, AttributeValue>(MarkerKey) { ["Expires"] = Number(expires) },
                ConditionExpression = "attribute_not_exists(#key) OR Expires < :now",
                ExpressionAttributeNames = new() { ["#key"] = "Key" },
                ExpressionAttributeValues = new() { [":now"] = Number(now.ToUnixTimeSeconds()) }
            }
        });
        writes.Add(new TransactWriteItem
        {
            Put = new Put
            {
                TableName = settings.TableName,
                Item = new()
                {
                    ["Sandbox"] = new AttributeValue { S = SandboxesPartition },
                    ["Key"] = new AttributeValue { S = sandbox },
                    ["Expires"] = Number(expires)
                }
            }
        });

        try
        {
            await dynamo.TransactWriteItemsAsync(new TransactWriteItemsRequest { TransactItems = writes }, cancellationToken);
            _expires = expires;
        }
        catch (TransactionCanceledException)
        {
            // Another invocation seeded it first.
            var seeded = await dynamo.GetItemAsync(
                new GetItemRequest { TableName = settings.TableName, Key = MarkerKey, ConsistentRead = true }, cancellationToken);
            _expires = long.Parse(seeded.Item["Expires"].N);
        }
    }

    private Dictionary<string, AttributeValue> RowKey(string key) => KeyOf($"{name}#{key}");

    private Dictionary<string, AttributeValue> MarkerKey => KeyOf($"seeded#{name}");

    private Dictionary<string, AttributeValue> KeyOf(string sortKey) =>
        new()
        {
            ["Sandbox"] = new AttributeValue { S = sandbox },
            ["Key"] = new AttributeValue { S = sortKey }
        };

    private Dictionary<string, AttributeValue> Row(string key, T value, long expires) =>
        new(RowKey(key))
        {
            ["Body"] = new AttributeValue { M = Document.FromJson(JsonSerializer.Serialize(value)).ToAttributeMap() },
            ["Expires"] = Number(expires)
        };

    private bool Live(Dictionary<string, AttributeValue> row) =>
        !row.TryGetValue("Expires", out var expires) || long.Parse(expires.N) > clock.GetUtcNow().ToUnixTimeSeconds();

    private static T Read(Dictionary<string, AttributeValue> row) =>
        JsonSerializer.Deserialize<T>(Document.FromAttributeMap(row["Body"].M).ToJson())
        ?? throw new JsonException("a row's body is null");

    private static AttributeValue Number(long value) => new() { N = value.ToString() };
}
