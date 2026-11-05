using System.Text.Json;
using System.Text.Json.Nodes;

namespace Demos.SignIn;

// Someone a visitor can be in a demo, as demos.json lists them under each demo: the shell shows the
// label, and the demo's rules see the id as the caller's name and the groups as its roles.
public record Persona(string Id, string Label, string Demo, IReadOnlyList<string>? Groups = null)
{
    // Every demo's personas, in the order demos.json gives them.
    public static IReadOnlyList<Persona> Load(string path)
    {
        var demos = JsonNode.Parse(File.ReadAllText(path)) as JsonArray ?? throw new JsonException($"{path} isn't a list of demos");
        return demos
            .SelectMany(demo => (demo!["personas"] as JsonArray ?? []).Select(persona => new Persona(
                (string)persona!["id"]!,
                (string?)persona["label"] ?? (string)persona["id"]!,
                (string)demo["id"]!,
                (persona["groups"] as JsonArray)?.Select(group => (string)group!).ToList())))
            .ToList();
    }
}
