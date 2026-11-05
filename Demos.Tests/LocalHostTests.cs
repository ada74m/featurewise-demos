using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Demos.Tests;

// The local host, over HTTP, as a visitor through the UI shell reaches it.
public class LocalHostTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _host = new();
    private readonly HttpClient _client;

    public LocalHostTests() => _client = _host.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _host.Dispose();
    }

    [Fact]
    public async Task TheUiBackendOffersLinksAsItsCallersUnsignedTokenSays()
    {
        var clerk = await Post("UnclaimedItems", Clerk, "{}");
        var traveller = await Post("UnclaimedItems", Traveller, "{}");

        Assert.NotNull(clerk["LinkToHandIn"]);
        Assert.Null(traveller["LinkToHandIn"]);
    }

    [Fact]
    public async Task ATravellerHearsOfAnItemSoonAfterAClerkHandsItIn()
    {
        var handedIn = await Post("HandInItem", Clerk, """{"Category":"Umbrella","Description":"Black","Station":"Crewe"}""");
        var id = handedIn["Item"]!["Id"]!.GetValue<string>();

        // The event reaches the worker through the outbox, a moment after the commit.
        JsonNode? match = null;
        for (var attempt = 0; attempt < 50 && match is null; attempt++)
        {
            await Task.Delay(100);
            match = (await Post("YourLostReports", Traveller, "{}"))["Reports"]![0]!["PossibleMatch"];
        }

        Assert.Equal(id, match?["Id"]?.GetValue<string>());
    }

    [Fact]
    public async Task AVisitorsSandboxComesFromTheirToken()
    {
        var handedIn = await Post("HandInItem", """{"username":"clerk","cognito:groups":["clerk"],"sandbox":"7f3a"}""", """{"Category":"Hat","Description":"A bowler","Station":"York"}""");
        var id = handedIn["Item"]!["Id"]!.GetValue<string>();

        var inSandbox = await Post("UnclaimedItems", """{"username":"traveller","sandbox":"7f3a"}""", "{}");
        var elsewhere = await Post("UnclaimedItems", Traveller, "{}");

        Assert.Contains(inSandbox["Items"]!.AsArray(), entry => entry!["Item"]!["Id"]!.GetValue<string>() == id);
        Assert.DoesNotContain(elsewhere["Items"]!.AsArray(), entry => entry!["Item"]!["Id"]!.GetValue<string>() == id);
    }

    [Fact]
    public async Task WithoutATokenTheUiBackendSaysToSignIn()
    {
        var response = await _client.PostAsync("/api/demos/UnclaimedItems", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TheShellsConfigurationOffersTheDemosPersonas()
    {
        var config = JsonNode.Parse(await _client.GetStringAsync("/services.json"))!;

        Assert.Equal(["traveller", "clerk"], config["personas"]!.AsArray().Select(persona => persona!["id"]!.GetValue<string>()));
    }

    private const string Traveller = """{"username":"traveller"}""";

    private const string Clerk = """{"username":"clerk","cognito:groups":["clerk"]}""";

    private async Task<JsonNode> Post(string feature, string claims, string body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/demos/{feature}")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = Token(claims);
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
    }

    private static AuthenticationHeaderValue Token(string claims) =>
        new("Bearer", $"e30.{Convert.ToBase64String(Encoding.UTF8.GetBytes(claims))}.x");
}
