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

        public static (byte[] encryptedData, byte[] ephemeralPublicKey, byte[] iv) EncryptWithPublicKey(string base64PublicKey, string plainText)
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
            aes.GenerateIV(); // Generate a random IV
            byte[] iv = aes.IV;

            using var encryptor = aes.CreateEncryptor();
            byte[] encryptedData = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Return encrypted data, ephemeral public key, and IV
            return (encryptedData, ephemeralPublicKey, iv);
        }

        public static string DecryptWithPrivateKey(string base64PrivateKey, byte[] encryptedData, byte[] ephemeralPublicKey, byte[] iv)
        {
            if (string.IsNullOrEmpty(base64PrivateKey))
            {
                return string.Empty;
            }
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
            aes.IV = iv; // Use the same IV that was used during encryption

            using var decryptor = aes.CreateDecryptor();
            byte[] decryptedData = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);

            return Encoding.UTF8.GetString(decryptedData);
        }

        public static string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256 instance
            using (SHA256 sha256 = SHA256.Create())
            {
                // Compute the hash as a byte array
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert the byte array to a hexadecimal string
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2")); // x2 formats as hexadecimal
                }
                return builder.ToString();
            }
        }
    }
}
