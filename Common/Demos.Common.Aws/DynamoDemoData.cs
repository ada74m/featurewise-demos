using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Demos.Aws;

// Each demo's tables are named <demo>/<table>, so forgetting a demo is deleting the sandbox's markers for
// the tables under its name: one write per table, however many rows they hold. The next use seeds a new
// generation, and TTL removes the old rows.
public class DynamoDemoData(IAmazonDynamoDB dynamo, DynamoChanges changes, DynamoTables tables, DynamoSettings settings) : IDemoData
{
    public async Task Reset(string demo, CancellationToken cancellationToken)
    {
        var markers = await dynamo.QueryAsync(new QueryRequest
        {
            TableName = settings.TableName,
            KeyConditionExpression = "Sandbox = :sandbox AND begins_with(#key, :prefix)",
            ExpressionAttributeNames = new() { ["#key"] = "Key" },
            ExpressionAttributeValues = new()
            {
                [":sandbox"] = new AttributeValue { S = tables.Sandbox },
                [":prefix"] = new AttributeValue { S = $"{DynamoTable<object>.MarkerPrefix}{demo}/" }
            },
            ConsistentRead = true
        }, cancellationToken);

        foreach (var marker in markers.Items ?? [])
        {
            var key = new Dictionary<string, AttributeValue> { ["Sandbox"] = marker["Sandbox"], ["Key"] = marker["Key"] };
            changes.Stage(() => new TransactWriteItem { Delete = new Delete { TableName = settings.TableName, Key = key } });
        }
    }
}
