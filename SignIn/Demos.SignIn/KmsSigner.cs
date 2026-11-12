using System.Security.Cryptography;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;

namespace Demos.SignIn;

// Signs with an asymmetric KMS key, RSA_2048 for signing, whose private half never leaves KMS.
public class KmsSigner(IAmazonKeyManagementService kms, string keyId) : ISigner
{
    private RSAParameters? _publicKey;

    public string KeyId => keyId;

    public async Task<byte[]> SignDigest(byte[] digest, CancellationToken cancellationToken)
    {
        var response = await kms.SignAsync(new SignRequest
        {
            KeyId = keyId,
            Message = new MemoryStream(digest),
            MessageType = MessageType.DIGEST,
            SigningAlgorithm = SigningAlgorithmSpec.RSASSA_PKCS1_V1_5_SHA_256
        }, cancellationToken);
        return response.Signature.ToArray();
    }

    // Fetched once per container: the public half never changes.
    public async Task<RSAParameters> PublicKey(CancellationToken cancellationToken)
    {
        if (_publicKey is { } known)
            return known;
        var response = await kms.GetPublicKeyAsync(new GetPublicKeyRequest { KeyId = keyId }, cancellationToken);
        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(response.PublicKey.ToArray(), out _);
        _publicKey = rsa.ExportParameters(includePrivateParameters: false);
        return _publicKey.Value;
    }
}
