using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Demos.SignIn;

// Signed JSON Web Tokens, RS256, which is what API Gateway's JWT authorizers accept.
public static class Jwt
{
    public static async Task<string> Sign(JsonObject claims, ISigner signer, CancellationToken cancellationToken)
    {
        var header = new JsonObject { ["alg"] = "RS256", ["typ"] = "JWT", ["kid"] = signer.KeyId };
        var signed = $"{Base64Url(header.ToJsonString())}.{Base64Url(claims.ToJsonString())}";
        var signature = await signer.SignDigest(SHA256.HashData(Encoding.ASCII.GetBytes(signed)), cancellationToken);
        return $"{signed}.{Base64Url(signature)}";
    }

    // The token's claims, if the key signed it, or null.
    public static async Task<JsonObject?> Verify(string token, ISigner signer, CancellationToken cancellationToken)
    {
        var parts = token.Split('.');
        if (parts.Length != 3)
            return null;
        try
        {
            using var rsa = RSA.Create(await signer.PublicKey(cancellationToken));
            var digest = SHA256.HashData(Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}"));
            if (!rsa.VerifyHash(digest, FromBase64Url(parts[2]), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
                return null;
            return JsonNode.Parse(FromBase64Url(parts[1])) as JsonObject;
        }
        catch (Exception e) when (e is FormatException or JsonException or CryptographicException)
        {
            return null;
        }
    }

    public static string Base64Url(string text) => Base64Url(Encoding.UTF8.GetBytes(text));

    public static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static byte[] FromBase64Url(string text)
    {
        var base64 = text.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '='));
    }
}
