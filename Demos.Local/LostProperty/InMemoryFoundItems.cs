using Demos.LostProperty;

namespace Demos.Local.LostProperty;

// The office's found items, in the invocation's sandbox.
public class InMemoryFoundItems(SandboxedTables tables, InMemoryChanges changes, TimeProvider clock) : IFoundItems
{
    private InMemoryTable<FoundItem> Items =>
        tables.Table("lost-property/items", () => Seed.Items(clock.GetUtcNow()).Select(item => (item.Id, item)));

    public Task<FoundItem?> Find(string id, CancellationToken cancellationToken) => Task.FromResult(Items[id]);

    public Task<IReadOnlyList<FoundItem>> All(CancellationToken cancellationToken) => Task.FromResult(Items.All());

    public void Add(FoundItem item)
    {
        var items = Items;
        changes.Stage(() => !items.Contains(item.Id), () => items.Put(item.Id, item));
    }

    public void Claim(FoundItem item, string traveller) =>
        Change(item, ItemStatus.Unclaimed, item with { Status = ItemStatus.Claimed, ClaimedBy = traveller });

    public void Donate(FoundItem item) =>
        Change(item, ItemStatus.Unclaimed, item with { Status = ItemStatus.DonatedToCharity });

    private void Change(FoundItem item, ItemStatus expected, FoundItem changed)
    {
        var items = Items;
        changes.Stage(() => items[item.Id]?.Status == expected, () => items.Put(item.Id, changed));
    }
}
