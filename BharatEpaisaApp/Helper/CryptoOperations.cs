using System.Security.Cryptography;

namespace BharatEpaisaApp.Helper
{
    internal static class CryptoOperations
    {
        static public (string publicKey, string privateKey) GenerateECCKeyPair()
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

            // Export public and private keys in PEM format
            var publicKey = Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo());
            var privateKey = Convert.ToBase64String(ecdsa.ExportPkcs8PrivateKey());

            return (publicKey, privateKey);
        }
    }
}
