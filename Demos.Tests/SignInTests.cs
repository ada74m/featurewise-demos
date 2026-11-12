using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using Demos.SignIn;
using Microsoft.Extensions.Time.Testing;

namespace Demos.Tests;

// The demo sign-in, given the requests the shell's browser makes, over a key held here in place of KMS's.
public class SignInTests
{
    private const string Domain = "signin.example";
    private const string Shell = "https://shell.example";

    // The demos and their personas, as the environment passes them in DEMOS.
    private const string Demos = """
        [
          { "id": "lost-property", "personas": [{ "id": "traveller", "label": "Traveller" }, { "id": "clerk", "label": "Clerk", "groups": ["clerk"] }] },
          { "id": "box-office", "personas": [{ "id": "theatregoer", "label": "Theatregoer" }] }
        ]
        """;

    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 5, 9, 0, 0, TimeSpan.Zero));
    private readonly Function _signIn;

    public SignInTests() =>
        _signIn = new Function(
            new LocalSigner(),
            new Settings("shell", $"{Shell}/_shell/callback", $"{Shell}/"),
            Persona.Read(Demos),
            _clock);

    [Fact]
    public async Task DiscoveryNamesTheEndpointsUnderTheIssuer()
    {
        var discovery = JsonNode.Parse((await Get("/.well-known/openid-configuration")).Body)!;

        Assert.Equal($"https://{Domain}", discovery["issuer"]!.GetValue<string>());
        Assert.Equal($"https://{Domain}/jwks.json", discovery["jwks_uri"]!.GetValue<string>());
    }

    [Fact]
    public async Task APersonaNamedInTheHintIsSignedInAtOnceInANewSandbox()
    {
        var response = await Get("/authorize", Authorize("clerk"));

        Assert.Equal(302, response.StatusCode);
        var back = new Uri(response.Headers["location"]);
        Assert.StartsWith($"{Shell}/_shell/callback?code=", back.ToString());
        Assert.Contains("state=s1", back.Query);
        Assert.Matches("^sandbox=[0-9a-f]{16}; ", Assert.Single(response.Cookies));
    }

    [Fact]
    public async Task AVisitorKeepsTheirSandbox()
    {
        var response = await Get("/authorize", Authorize("clerk"), cookie: "sandbox=7f3a7f3a7f3a7f3a");

        Assert.Null(response.Cookies);
        var tokens = await Trade(response);
        Assert.Equal("7f3a7f3a7f3a7f3a", Claims(tokens)["sandbox"]!.GetValue<string>());
    }

    [Fact]
    public async Task WithoutAHintThePersonasAreOfferedByDemo()
    {
        var response = await Get("/authorize", Authorize(null));

        Assert.Equal(200, response.StatusCode);
        Assert.Contains("<h2>Lost property</h2>", response.Body);
        Assert.Contains("login_hint=traveller", response.Body);
        Assert.Contains(">Clerk</a>", response.Body);
    }

    [Theory]
    [InlineData("client_id", "someone-else")]
    [InlineData("redirect_uri", "https://elsewhere.example/_shell/callback")]
    [InlineData("code_challenge_method", "plain")]
    public async Task AnAuthorizationItCantTrustIsRefusedWithoutRedirecting(string parameter, string value)
    {
        var query = Authorize("clerk");
        query[parameter] = value;

        var response = await Get("/authorize", query);

        Assert.Equal(400, response.StatusCode);
        Assert.False(response.Headers.ContainsKey("location"));
    }

    [Fact]
    public async Task ACodeIsTradedForTokensThatSayWhoAndWhere()
    {
        var tokens = await Trade(await Get("/authorize", Authorize("clerk"), cookie: "sandbox=abc123"));

        var claims = Claims(tokens);
        Assert.Equal($"https://{Domain}", claims["iss"]!.GetValue<string>());
        Assert.Equal("clerk", claims["username"]!.GetValue<string>());
        Assert.Equal("shell", claims["aud"]!.GetValue<string>());
        Assert.Equal(["clerk"], claims["groups"]!.AsArray().Select(group => group!.GetValue<string>()));
        Assert.Equal("abc123", claims["sandbox"]!.GetValue<string>());
        Assert.True(await VerifiesWithTheJwks(tokens["access_token"]!.GetValue<string>()));
    }

    [Fact]
    public async Task ACodeIsntTradedWithoutTheVerifierWhoseHashItWasGiven()
    {
        var response = await Get("/authorize", Authorize("clerk"));

        var traded = await Post("/token", TokenForm(CodeOf(response), "not-the-verifier"));

        Assert.Equal(400, traded.StatusCode);
        Assert.Equal("invalid_grant", JsonNode.Parse(traded.Body)!["error"]!.GetValue<string>());
    }

    [Fact]
    public async Task ACodeExpires()
    {
        var response = await Get("/authorize", Authorize("clerk"));
        _clock.Advance(TimeSpan.FromMinutes(3));

        Assert.Equal(400, (await Post("/token", TokenForm(CodeOf(response), Verifier))).StatusCode);
    }

    [Fact]
    public async Task AnAlteredCodeIsRefused()
    {
        var code = CodeOf(await Get("/authorize", Authorize("traveller")));
        var parts = code.Split('.');
        var claims = JsonNode.Parse(Jwt.FromBase64Url(parts[1]))!.AsObject();
        claims["sub"] = "clerk";
        var altered = $"{parts[0]}.{Jwt.Base64Url(claims.ToJsonString())}.{parts[2]}";

        Assert.Equal(400, (await Post("/token", TokenForm(altered, Verifier))).StatusCode);
    }

    [Fact]
    public async Task TheShellMayReadTheTokenEndpointsAnswer()
    {
        var traded = await Post("/token", TokenForm(CodeOf(await Get("/authorize", Authorize("clerk"))), Verifier));

        Assert.Equal(Shell, traded.Headers["access-control-allow-origin"]);
    }

    private const string Verifier = "a-verifier-the-shell-made-and-kept-to-itself-0123456789";

    private static Dictionary<string, string> Authorize(string? persona)
    {
        var query = new Dictionary<string, string>
        {
            ["response_type"] = "code",
            ["client_id"] = "shell",
            ["redirect_uri"] = $"{Shell}/_shell/callback",
            ["state"] = "s1",
            ["code_challenge"] = Jwt.Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(Verifier))),
            ["code_challenge_method"] = "S256"
        };
        if (persona is not null)
            query["login_hint"] = persona;
        return query;
    }

    private static string CodeOf(APIGatewayHttpApiV2ProxyResponse response) =>
        Uri.UnescapeDataString(new Uri(response.Headers["location"]).Query.TrimStart('?').Split('&')
            .Single(pair => pair.StartsWith("code=")).Split('=', 2)[1]);

    private static string TokenForm(string code, string verifier) =>
        $"grant_type=authorization_code&client_id=shell&code={Uri.EscapeDataString(code)}" +
        $"&redirect_uri={Uri.EscapeDataString($"{Shell}/_shell/callback")}&code_verifier={Uri.EscapeDataString(verifier)}";

    private async Task<JsonObject> Trade(APIGatewayHttpApiV2ProxyResponse authorized)
    {
        var traded = await Post("/token", TokenForm(CodeOf(authorized), Verifier));
        Assert.Equal(200, traded.StatusCode);
        return JsonNode.Parse(traded.Body)!.AsObject();
    }

    private static JsonObject Claims(JsonObject tokens) =>
        JsonNode.Parse(Jwt.FromBase64Url(tokens["access_token"]!.GetValue<string>().Split('.')[1]))!.AsObject();

    // As API Gateway's authorizer checks a token: with the key the JWKS publishes.
    private async Task<bool> VerifiesWithTheJwks(string token)
    {
        var key = JsonNode.Parse((await Get("/jwks.json")).Body)!["keys"]![0]!;
        using var rsa = RSA.Create(new RSAParameters
        {
            Modulus = Jwt.FromBase64Url(key["n"]!.GetValue<string>()),
            Exponent = Jwt.FromBase64Url(key["e"]!.GetValue<string>())
        });
        var parts = token.Split('.');
        return rsa.VerifyData(Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}"), Jwt.FromBase64Url(parts[2]),
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    private Task<APIGatewayHttpApiV2ProxyResponse> Get(string path, Dictionary<string, string>? query = null, string? cookie = null) =>
        Send("GET", path, query, null, cookie);

    private Task<APIGatewayHttpApiV2ProxyResponse> Post(string path, string form) => Send("POST", path, null, form, null);

    private Task<APIGatewayHttpApiV2ProxyResponse> Send(string method, string path, Dictionary<string, string>? query, string? body, string? cookie) =>
        _signIn.Handle(new APIGatewayHttpApiV2ProxyRequest
        {
            RouteKey = $"{method} {path}",
            RawPath = path,
            QueryStringParameters = query,
            Body = body,
            Cookies = cookie is null ? null : [cookie],
            RequestContext = new APIGatewayHttpApiV2ProxyRequest.ProxyRequestContext { DomainName = Domain }
        }, new TestLambdaContext { RemainingTime = TimeSpan.FromSeconds(30) });

    // An RSA key held in memory, in place of KMS's.
    private sealed class LocalSigner : ISigner
    {
        private readonly RSA _rsa = RSA.Create(2048);

        public string KeyId => "local";

        public Task<byte[]> SignDigest(byte[] digest, CancellationToken cancellationToken) =>
            Task.FromResult(_rsa.SignHash(digest, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));

        public Task<RSAParameters> PublicKey(CancellationToken cancellationToken) => Task.FromResult(_rsa.ExportParameters(false));
    }
}
