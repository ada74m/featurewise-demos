using System.Security.Cryptography;

namespace Demos.SignIn;

// Signs tokens with an RSA key whose private half the sign-in never holds, as KMS keeps it.
public interface ISigner
{
    // Names the key in each token's header and in the JWKS, so a verifier knows which key to use.
    string KeyId { get; }

    // An RS256 signature of the SHA-256 digest.
    Task<byte[]> SignDigest(byte[] digest, CancellationToken cancellationToken);

    Task<RSAParameters> PublicKey(CancellationToken cancellationToken);
}
