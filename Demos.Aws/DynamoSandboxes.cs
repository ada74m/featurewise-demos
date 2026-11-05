using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Demos.Aws;

// The sandboxes whose data hasn't expired, which a scheduled job runs in.
public class DynamoSandboxes(IAmazonDynamoDB dynamo, TimeProvider clock, DynamoSettings settings)
{
    public async Task<IReadOnlyList<string>> Live(CancellationToken cancellationToken)
    {
        var sandboxes = new List<string>();
        Dictionary<string, AttributeValue>? startKey = null;
        do
        {
            var page = await dynamo.QueryAsync(new QueryRequest
            {
                TableName = settings.TableName,
                KeyConditionExpression = "Sandbox = :sandboxes",
                FilterExpression = "Expires > :now",
                ExpressionAttributeValues = new()
                {
                    [":sandboxes"] = new AttributeValue { S = DynamoTable<object>.SandboxesPartition },
                    [":now"] = new AttributeValue { N = clock.GetUtcNow().ToUnixTimeSeconds().ToString() }
                },
                ExclusiveStartKey = startKey
            }, cancellationToken);
            sandboxes.AddRange((page.Items ?? []).Select(row => row["Key"].S));
            startKey = page.LastEvaluatedKey is { Count: > 0 } lastKey ? lastKey : null;
        } while (startKey is not null);

        return sandboxes.Order().ToList();
    }
}
