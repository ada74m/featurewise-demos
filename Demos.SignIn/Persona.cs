using System.Text.Json;

namespace Demos.SignIn;

// Someone a visitor can be in a demo, as personas.json lists them: the shell shows the label, and the
// demo's rules see the id as the caller's name and the groups as its roles.
public record Persona(string Id, string Label, string Demo, IReadOnlyList<string>? Groups = null)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<Persona> Load(string path) =>
        JsonSerializer.Deserialize<List<Persona>>(File.ReadAllText(path), Json)
        ?? throw new JsonException($"{path} lists no personas");
}
