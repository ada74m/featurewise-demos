using System.Text.Json.Serialization;

namespace Demos.LostProperty;

// Where a found item is in its life at the office.
[JsonConverter(typeof(JsonStringEnumConverter<ItemStatus>))]
public enum ItemStatus
{
    Unclaimed,
    Claimed,
    DonatedToCharity
}
