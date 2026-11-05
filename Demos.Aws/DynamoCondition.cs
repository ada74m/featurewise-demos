using Amazon.DynamoDBv2.Model;

namespace Demos.Aws;

// A condition on a row's record, which DynamoDB checks when the commit writes it. The record is the map in
// the row's Body, so a field of it is #body.#field.
public record DynamoCondition(string Expression, Dictionary<string, string> Names, Dictionary<string, AttributeValue> Values)
{
    public static DynamoCondition FieldEquals(string field, string value) =>
        new("#body.#field = :value", new() { ["#body"] = "Body", ["#field"] = field }, new() { [":value"] = new AttributeValue { S = value } });

    public static DynamoCondition FieldIsNull(string field) =>
        new("attribute_not_exists(#body.#field) OR attribute_type(#body.#field, :null)",
            new() { ["#body"] = "Body", ["#field"] = field },
            new() { [":null"] = new AttributeValue { S = "NULL" } });
}
