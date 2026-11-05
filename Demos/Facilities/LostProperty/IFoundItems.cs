namespace Demos.LostProperty;

// Where found items are kept. Reads happen straight away. Writes are staged in the invocation's IChanges
// and happen only when the feature commits.
public interface IFoundItems
{
    Task<FoundItem?> Find(string id, CancellationToken cancellationToken);

    // Every item, whatever its status. A demo-sized office can afford to scan.
    Task<IReadOnlyList<FoundItem>> All(CancellationToken cancellationToken);

    // Stages keeping a newly handed-in item, on condition that no item has its id.
    void Add(FoundItem item);

    // Stages handing the item to a traveller, on condition that it's still unclaimed.
    void Claim(FoundItem item, string traveller);

    // Stages giving the item to charity, on condition that it's still unclaimed.
    void Donate(FoundItem item);
}
