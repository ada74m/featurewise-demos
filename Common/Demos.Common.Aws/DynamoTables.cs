using Amazon.DynamoDBv2;
using Featurewise;

namespace Demos.Aws;

// The tables of the invocation's sandbox, which the caller carries, so that a demo's facilities read and
// write as if there were only one visitor. Every demo's tables share one DynamoDB table: each row's
// partition key is its sandbox, and its sort key is its table's name and its key.
public class DynamoTables(IAmazonDynamoDB dynamo, DynamoChanges changes, Invocation invocation, TimeProvider clock, DynamoSettings settings)
{
    private readonly Dictionary<string, object> _tables = [];

    public string Sandbox => Sandboxes.Of(invocation.Caller);

    public DynamoTable<T> Table<T>(string name, Func<IEnumerable<(string Key, T Value)>> seed) where T : notnull
    {
        if (!_tables.TryGetValue(name, out var table))
            _tables[name] = table = new DynamoTable<T>(dynamo, changes, clock, settings, Sandbox, name, seed);
        return (DynamoTable<T>)table;
    }
}
