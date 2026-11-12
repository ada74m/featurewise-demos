using System.Text.Json;
using System.Text.Json.Nodes;

namespace Demos.SignIn;

// Someone a visitor can be in a demo, as each demo lists them: the shell shows the label, and the demo's
// rules see the id as the caller's name and the groups as its roles.
public record Persona(string Id, string Label, string Demo, IReadOnlyList<string>? Groups = null)
{
    // Every demo's personas, in the order given, from a list of demos as JSON, each with its id and its
    // personas, as the environment passes them in DEMOS.
    public static IReadOnlyList<Persona> Read(string json)
    {
        var demos = JsonNode.Parse(json) as JsonArray ?? throw new JsonException("the demos aren't a list");
        return demos
            .SelectMany(demo => (demo!["personas"] as JsonArray ?? []).Select(persona => new Persona(
                (string)persona!["id"]!,
                (string?)persona["label"] ?? (string)persona["id"]!,
                (string)demo["id"]!,
                (persona["groups"] as JsonArray)?.Select(group => (string)group!).ToList())))
            .ToList();
    }
}
