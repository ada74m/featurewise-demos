using System.Security.Cryptography;
using System.Text;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Featurewise;

namespace Demos.Aws;

// The invocation's unit of work on DynamoDB. The facilities stage their writes, and Commit sends them as
// one TransactWriteItems request, which DynamoDB applies all or none. Nothing has happened until then.
// A write can be staged before what it needs is known, such as when its sandbox's data expires, so the
// facilities can also stage work to do just before the commit.
public sealed class DynamoChanges(IAmazonDynamoDB dynamo, Invocation invocation) : IChanges, IDisposable
{
    private readonly List<Func<CancellationToken, Task>> _preparations = [];
    private readonly List<Func<TransactWriteItem>> _staged = [];

    public void Stage(Func<TransactWriteItem> write) => _staged.Add(write);

    public void BeforeCommit(Func<CancellationToken, Task> preparation) => _preparations.Add(preparation);

    public async Task<bool> Commit(CancellationToken cancellationToken)
    {
        foreach (var preparation in _preparations.ToList())
            await preparation(cancellationToken);
        _preparations.Clear();

        if (_staged.Count == 0)
            return true;

        var request = new TransactWriteItemsRequest
        {
            TransactItems = _staged.Select(write => write()).ToList(),
            ClientRequestToken = TokenFor(invocation.IdempotencyKey)
        };
        _staged.Clear();

        try
        {
            await dynamo.TransactWriteItemsAsync(request, cancellationToken);
            return true;
        }
        catch (TransactionCanceledException cancelled)
            when (cancelled.CancellationReasons?.Any(reason => reason.Code == "ConditionalCheckFailed") == true)
        {
            return false;
        }
    }

    // An invocation that ends with writes it never committed is a bug.
    public void Dispose()
    {
        if (_staged.Count > 0)
            throw new InvalidOperationException(
                $"the invocation staged {_staged.Count} writes and never committed them (if it also threw, that's the first failure)");
    }

    // DynamoDB treats a repeated request with the same token as done for ten minutes, so a retried
    // invocation commits once. A token is at most 36 characters, and an idempotency key may be longer.
    private static string? TokenFor(string? idempotencyKey) =>
        idempotencyKey is null
            ? null
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(idempotencyKey)))[..32];
}
