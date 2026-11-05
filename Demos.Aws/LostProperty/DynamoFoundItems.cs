using Demos.LostProperty;

namespace Demos.Aws.LostProperty;

// The office's found items, in the invocation's sandbox.
public class DynamoFoundItems(DynamoTables tables, TimeProvider clock) : IFoundItems
{
    private DynamoTable<FoundItem> Items =>
        tables.Table("lost-property/items", () => Seed.Items(clock.GetUtcNow()).Select(item => (item.Id, item)));

    public Task<FoundItem?> Find(string id, CancellationToken cancellationToken) => Items.Find(id, cancellationToken);

    public Task<IReadOnlyList<FoundItem>> All(CancellationToken cancellationToken) => Items.All(cancellationToken);

    public void Add(FoundItem item) => Items.Add(item.Id, item);

    public void Claim(FoundItem item, string traveller) =>
        Items.Replace(item.Id, item with { Status = ItemStatus.Claimed, ClaimedBy = traveller }, StillUnclaimed);

    public void Donate(FoundItem item) =>
        Items.Replace(item.Id, item with { Status = ItemStatus.DonatedToCharity }, StillUnclaimed);

    private static DynamoCondition StillUnclaimed => DynamoCondition.FieldEquals(nameof(FoundItem.Status), nameof(ItemStatus.Unclaimed));
}
