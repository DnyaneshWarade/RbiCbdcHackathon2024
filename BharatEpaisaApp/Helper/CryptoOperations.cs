using System.Security.Cryptography;
using System.Text;

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

        public static (byte[] encryptedData, byte[] ephemeralPublicKey) EncryptWithPublicKey(string base64PublicKey, string plainText)
        {
            byte[] publicKeyBytes = Convert.FromBase64String(base64PublicKey);
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            // Import the receiver's public key
            using var receiverECDH = ECDiffieHellman.Create();
            receiverECDH.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

            // Generate an ephemeral key pair for the sender
            using var senderECDH = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            byte[] ephemeralPublicKey = senderECDH.PublicKey.ExportSubjectPublicKeyInfo();

            // Derive a shared secret
            byte[] sharedSecret = senderECDH.DeriveKeyFromHash(
                receiverECDH.PublicKey,
                HashAlgorithmName.SHA256);

            // Encrypt data using AES
            using var aes = Aes.Create();
            aes.Key = sharedSecret;
            aes.GenerateIV(); // For added security

            using var encryptor = aes.CreateEncryptor();
            byte[] encryptedData = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            return (encryptedData, ephemeralPublicKey);
        }

        public static string DecryptWithPrivateKey(string base64PrivateKey, byte[] encryptedData, byte[] ephemeralPublicKey)
        {
            byte[] privateKeyBytes = Convert.FromBase64String(base64PrivateKey);

            // Import the receiver's private key
            using var receiverECDH = ECDiffieHellman.Create();
            receiverECDH.ImportPkcs8PrivateKey(privateKeyBytes, out _);

            // Import the sender's ephemeral public key
            using var senderECDH = ECDiffieHellman.Create();
            senderECDH.ImportSubjectPublicKeyInfo(ephemeralPublicKey, out _);

            // Derive the shared secret
            byte[] sharedSecret = receiverECDH.DeriveKeyFromHash(
                senderECDH.PublicKey,
                HashAlgorithmName.SHA256);

            // Decrypt data using AES
            using var aes = Aes.Create();
            aes.Key = sharedSecret;

            using var decryptor = aes.CreateDecryptor();
            byte[] decryptedData = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);

            return Encoding.UTF8.GetString(decryptedData);
        }
    }
}
