using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Amazon.KeyManagementService;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Demos.SignIn;

// The demos' sign-in: an OpenID Connect provider with no passwords, where a visitor becomes any of the
// demos' personas by choosing it, or by the shell naming it in login_hint, which signs it in at once.
// Every token carries the visitor's sandbox, from a cookie the sign-in sets on the first visit, so all
// the personas a visitor becomes share one sandbox. Only the demo environment has it.
//
//   GET  /.well-known/openid-configuration   discovery, which API Gateway's JWT authorizers read
//   GET  /jwks.json                          the public key tokens are verified with
//   GET  /authorize                          the code flow with PKCE; a page of personas without a hint
//   POST /token                              trades a code for tokens
//   GET  /logout                             sends the browser back to the shell
//
// The code is itself a short-lived token, signed like the others, so the sign-in stores nothing. It can
// be traded more than once in its two minutes, but only by whoever holds the PKCE verifier.
public class Function
{
    private const string SandboxCookie = "sandbox";

    private readonly ISigner _signer;
    private readonly Settings _settings;
    private readonly IReadOnlyList<Persona> _personas;
    private readonly TimeProvider _clock;

    public Function() : this(
        new KmsSigner(new AmazonKeyManagementServiceClient(), Setting("KEY_ID")),
        new Settings(Setting("CLIENT_ID"), Setting("REDIRECT_URI"), Setting("LOGOUT_URI")),
        Persona.Load(Path.Combine(AppContext.BaseDirectory, "personas.json")),
        TimeProvider.System)
    {
    }

    public Function(ISigner signer, Settings settings, IReadOnlyList<Persona> personas, TimeProvider clock)
    {
        _signer = signer;
        _settings = settings;
        _personas = personas;
        _clock = clock;
    }

    public async Task<APIGatewayHttpApiV2ProxyResponse> Handle(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        using var timeout = new CancellationTokenSource(context.RemainingTime - TimeSpan.FromSeconds(1));
        var issuer = $"https://{request.RequestContext.DomainName}";
        return request.RouteKey switch
        {
            "GET /.well-known/openid-configuration" => Json(HttpStatusCode.OK, Discovery(issuer)),
            "GET /jwks.json" => Json(HttpStatusCode.OK, await Keys(timeout.Token)),
            "GET /authorize" => await Authorize(request, timeout.Token),
            "POST /token" => await Token(request, issuer, timeout.Token),
            "GET /logout" => Redirect(_settings.LogoutUri),
            _ => Text(HttpStatusCode.NotFound, "There's nothing here.")
        };
    }

    private static JsonObject Discovery(string issuer) =>
        new()
        {
            ["issuer"] = issuer,
            ["authorization_endpoint"] = $"{issuer}/authorize",
            ["token_endpoint"] = $"{issuer}/token",
            ["end_session_endpoint"] = $"{issuer}/logout",
            ["jwks_uri"] = $"{issuer}/jwks.json",
            ["response_types_supported"] = new JsonArray("code"),
            ["grant_types_supported"] = new JsonArray("authorization_code"),
            ["code_challenge_methods_supported"] = new JsonArray("S256"),
            ["subject_types_supported"] = new JsonArray("public"),
            ["id_token_signing_alg_values_supported"] = new JsonArray("RS256"),
            ["token_endpoint_auth_methods_supported"] = new JsonArray("none")
        };

    private async Task<JsonObject> Keys(CancellationToken cancellationToken)
    {
        var key = await _signer.PublicKey(cancellationToken);
        return new JsonObject
        {
            ["keys"] = new JsonArray(new JsonObject
            {
                ["kty"] = "RSA",
                ["use"] = "sig",
                ["alg"] = "RS256",
                ["kid"] = _signer.KeyId,
                ["n"] = Jwt.Base64Url(key.Modulus!),
                ["e"] = Jwt.Base64Url(key.Exponent!)
            })
        };
    }

    private async Task<APIGatewayHttpApiV2ProxyResponse> Authorize(APIGatewayHttpApiV2ProxyRequest request, CancellationToken cancellationToken)
    {
        var query = request.QueryStringParameters ?? new Dictionary<string, string>();
        string? Get(string name) => query.TryGetValue(name, out var value) ? value : null;

        // Anything wrong with the client or where to send the browser is shown here, never redirected.
        if (Get("client_id") != _settings.ClientId || Get("redirect_uri") != _settings.RedirectUri)
            return Text(HttpStatusCode.BadRequest, "The sign-in doesn't know that client, or where it wants to go back to.");
        if (Get("response_type") != "code" || Get("code_challenge_method") != "S256" || string.IsNullOrEmpty(Get("code_challenge")))
            return Text(HttpStatusCode.BadRequest, "The sign-in needs the authorization code flow with PKCE (S256).");

        var (sandbox, newSandbox) = SandboxOf(request);
        var persona = _personas.FirstOrDefault(persona => persona.Id == Get("login_hint"));
        if (persona is null)
            return WithSandbox(Html(Chooser(query)), sandbox, newSandbox);

        var now = _clock.GetUtcNow();
        var code = await Jwt.Sign(new JsonObject
        {
            ["use"] = "code",
            ["sub"] = persona.Id,
            ["sandbox"] = sandbox,
            ["client_id"] = _settings.ClientId,
            ["redirect_uri"] = _settings.RedirectUri,
            ["code_challenge"] = Get("code_challenge"),
            ["exp"] = (now + _settings.CodeLifetime).ToUnixTimeSeconds()
        }, _signer, cancellationToken);

        var back = new StringBuilder($"{_settings.RedirectUri}?code={Uri.EscapeDataString(code)}");
        if (Get("state") is { } state)
            back.Append($"&state={Uri.EscapeDataString(state)}");
        return WithSandbox(Redirect(back.ToString()), sandbox, newSandbox);
    }

    private async Task<APIGatewayHttpApiV2ProxyResponse> Token(APIGatewayHttpApiV2ProxyRequest request, string issuer, CancellationToken cancellationToken)
    {
        var form = Form(request);
        string? Get(string name) => form.TryGetValue(name, out var value) ? value : null;

        if (Get("grant_type") != "authorization_code")
            return TokenError("unsupported_grant_type");
        var code = Get("code") is { } text ? await Jwt.Verify(text, _signer, cancellationToken) : null;
        var now = _clock.GetUtcNow();
        if (code is null
            || (string?)code["use"] != "code"
            || (long?)code["exp"] < now.ToUnixTimeSeconds()
            || (string?)code["client_id"] != Get("client_id")
            || (string?)code["redirect_uri"] != Get("redirect_uri")
            || Get("code_verifier") is not { } verifier
            || Jwt.Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))) != (string?)code["code_challenge"])
            return TokenError("invalid_grant");

        var persona = _personas.FirstOrDefault(persona => persona.Id == (string?)code["sub"]);
        if (persona is null)
            return TokenError("invalid_grant");

        var claims = new JsonObject
        {
            ["iss"] = issuer,
            ["sub"] = persona.Id,
            ["aud"] = _settings.ClientId,
            ["client_id"] = _settings.ClientId,
            ["username"] = persona.Id,
            ["name"] = persona.Label,
            ["groups"] = new JsonArray([.. (persona.Groups ?? []).Select(group => JsonValue.Create(group))]),
            ["sandbox"] = (string?)code["sandbox"],
            ["iat"] = now.ToUnixTimeSeconds(),
            ["exp"] = (now + _settings.TokenLifetime).ToUnixTimeSeconds()
        };
        var token = await Jwt.Sign(claims, _signer, cancellationToken);
        var response = Json(HttpStatusCode.OK, new JsonObject
        {
            ["access_token"] = token,
            ["id_token"] = token,
            ["token_type"] = "Bearer",
            ["expires_in"] = (int)_settings.TokenLifetime.TotalSeconds
        });
        response.Headers["cache-control"] = "no-store";
        return response;
    }

    // The visitor's sandbox, and whether it's new, so the cookie must be set.
    private (string Sandbox, bool New) SandboxOf(APIGatewayHttpApiV2ProxyRequest request)
    {
        foreach (var cookie in request.Cookies ?? [])
        {
            var (name, value) = (cookie.Split('=', 2)[0].Trim(), cookie.Split('=', 2).ElementAtOrDefault(1)?.Trim());
            if (name == SandboxCookie && value is { Length: > 0 } && value.All(char.IsAsciiHexDigit))
                return (value, false);
        }
        return (Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(8)), true);
    }

    private APIGatewayHttpApiV2ProxyResponse WithSandbox(APIGatewayHttpApiV2ProxyResponse response, string sandbox, bool isNew)
    {
        if (isNew)
            response.Cookies = [$"{SandboxCookie}={sandbox}; Path=/; Max-Age={(int)_settings.SandboxLifetime.TotalSeconds}; Secure; HttpOnly; SameSite=Lax"];
        return response;
    }

    // The page that offers the personas, grouped by demo, each a link back here naming it.
    private string Chooser(IDictionary<string, string> query)
    {
        var page = new StringBuilder("""
            <!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width">
            <title>Choose who to be</title>
            <style>body{font:16px system-ui,sans-serif;max-width:32rem;margin:3rem auto;padding:0 1rem;color:#222}
            h2{font-size:1rem;color:#666;margin:1.5rem 0 .5rem}a{display:inline-block;margin:0 .5rem .5rem 0;padding:.4rem .9rem;
            border:1px solid #2563eb;border-radius:999px;color:#2563eb;text-decoration:none}a:hover{background:#eff4ff}</style>
            </head><body><h1>Choose who to be</h1><p>There are no passwords: pick anyone. Whoever you are, the demos remember what you do for a week, in a sandbox of your own.</p>
            """);
        foreach (var demo in _personas.GroupBy(persona => persona.Demo))
        {
            page.Append($"<h2>{WebUtility.HtmlEncode(TitleOf(demo.Key))}</h2>");
            foreach (var persona in demo)
            {
                var with = query.Where(parameter => parameter.Key != "login_hint")
                    .Append(KeyValuePair.Create("login_hint", persona.Id))
                    .Select(parameter => $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}");
                page.Append($"""<a href="authorize?{WebUtility.HtmlEncode(string.Join('&', with))}">{WebUtility.HtmlEncode(persona.Label)}</a>""");
            }
        }
        return page.Append("</body></html>").ToString();
    }

    // "lost-property" is shown as "Lost property".
    private static string TitleOf(string demo) =>
        demo.Length == 0 ? demo : char.ToUpperInvariant(demo[0]) + demo[1..].Replace('-', ' ');

    private static Dictionary<string, string> Form(APIGatewayHttpApiV2ProxyRequest request)
    {
        var body = request.IsBase64Encoded && request.Body is not null
            ? Encoding.UTF8.GetString(Convert.FromBase64String(request.Body))
            : request.Body ?? "";
        return body.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .GroupBy(pair => Uri.UnescapeDataString(pair[0].Replace('+', ' ')))
            .ToDictionary(group => group.Key, group => Uri.UnescapeDataString(group.First().ElementAtOrDefault(1)?.Replace('+', ' ') ?? ""));
    }

    // The shell calls the token endpoint from its own origin, as a simple POST, so only this is needed.
    private APIGatewayHttpApiV2ProxyResponse TokenError(string error)
    {
        var response = Json(HttpStatusCode.BadRequest, new JsonObject { ["error"] = error });
        response.Headers["cache-control"] = "no-store";
        return response;
    }

    private APIGatewayHttpApiV2ProxyResponse Json(HttpStatusCode status, JsonObject body) =>
        new()
        {
            StatusCode = (int)status,
            Body = body.ToJsonString(),
            Headers = new Dictionary<string, string>
            {
                ["content-type"] = "application/json",
                ["access-control-allow-origin"] = _settings.ShellOrigin
            }
        };

    private static APIGatewayHttpApiV2ProxyResponse Html(string html) =>
        new() { StatusCode = 200, Body = html, Headers = new Dictionary<string, string> { ["content-type"] = "text/html; charset=utf-8" } };

    private static APIGatewayHttpApiV2ProxyResponse Text(HttpStatusCode status, string text) =>
        new() { StatusCode = (int)status, Body = text, Headers = new Dictionary<string, string> { ["content-type"] = "text/plain; charset=utf-8" } };

    private static APIGatewayHttpApiV2ProxyResponse Redirect(string url) =>
        new() { StatusCode = 302, Headers = new Dictionary<string, string> { ["location"] = url, ["cache-control"] = "no-store" } };

    private static string Setting(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"the environment variable {name} isn't set");
}
