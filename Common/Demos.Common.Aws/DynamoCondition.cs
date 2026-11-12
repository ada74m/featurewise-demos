using Amazon.DynamoDBv2.Model;

namespace Demos.Aws;

// A condition on a row's record, which DynamoDB checks when the commit writes it. The record is the map in
// the row's Body, so a field of it is #body.#<field>. Each field has placeholders of its own, so that
// conditions on different fields can be joined with And.
public record DynamoCondition(string Expression, Dictionary<string, string> Names, Dictionary<string, AttributeValue> Values)
{
    public static DynamoCondition FieldEquals(string field, string value) => Equals(field, new AttributeValue { S = value });

    public static DynamoCondition FieldEquals(string field, int value) => Equals(field, new AttributeValue { N = value.ToString() });

    public static DynamoCondition FieldEquals(string field, bool value) => Equals(field, new AttributeValue { BOOL = value });

    public static DynamoCondition FieldIsNull(string field) =>
        new($"attribute_not_exists(#body.#{field}) OR attribute_type(#body.#{field}, :null)",
            new() { ["#body"] = "Body", [$"#{field}"] = field },
            new() { [":null"] = new AttributeValue { S = "NULL" } });

    // Both conditions.
    public DynamoCondition And(DynamoCondition other) =>
        new($"({Expression}) AND ({other.Expression})",
            new(Names.Concat(other.Names).DistinctBy(name => name.Key)),
            new(Values.Concat(other.Values).DistinctBy(value => value.Key)));

    private static DynamoCondition Equals(string field, AttributeValue value) =>
        new($"#body.#{field} = :{field}", new() { ["#body"] = "Body", [$"#{field}"] = field }, new() { [$":{field}"] = value });
}
