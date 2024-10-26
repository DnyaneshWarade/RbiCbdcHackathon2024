using BharatEpaisaApp.Database.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace BharatEpaisaApp.Helper
{
    internal static class CommonFunctions
    {
        // Regular expression to match exactly 10 digits
        static readonly string mobileNoPattern = @"^\d{10}$";
        static readonly public string Key = "yourSecretKey123";
        static private ICryptoTransform encyptCryptoTransform;
        static private ICryptoTransform decyptCryptoTransform;
        static public string LoggedInMobileNo { get; set; }
        static public string LoggedInMobilePin { get; set; }

        public static bool ValidatePhoneNumber(string mobileNo)
        {
            if (string.IsNullOrEmpty(mobileNo)) { return false; }
            return Regex.IsMatch(mobileNo, mobileNoPattern);
        }

        private static ICryptoTransform GetEncryptor()
        {
            try
            {
                if (encyptCryptoTransform != null)
                {
                    return encyptCryptoTransform;
                }

                // Convert the secret key to a byte array
                byte[] keyBytes = Encoding.UTF8.GetBytes(Key);
                Aes aesAlg = Aes.Create();

                aesAlg.Key = keyBytes;
                aesAlg.Mode = CipherMode.ECB;
                aesAlg.Padding = PaddingMode.PKCS7;
                encyptCryptoTransform = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                return encyptCryptoTransform;
            }
            catch
            {
                return null;
            }
        }

        private static ICryptoTransform GetDecryptor()
        {
            try
            {
                if (decyptCryptoTransform != null)
                {
                    return decyptCryptoTransform;
                }

                // Convert the secret key to a byte array
                byte[] keyBytes = Encoding.UTF8.GetBytes(Key);
                Aes aesAlg = Aes.Create();

                aesAlg.Key = keyBytes;
                aesAlg.Mode = CipherMode.ECB;
                aesAlg.Padding = PaddingMode.PKCS7;
                decyptCryptoTransform = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                return decyptCryptoTransform;
            }
            catch
            {
                return null;
            }
        }

        public static string GetEncryptedMessage(string message)
        {
            if (message == null)
            {
                return string.Empty;
            }
            try
            {
                // get encryptor first
                ICryptoTransform encryptor = GetEncryptor();
                if (encryptor == null)
                {
                    return string.Empty;
                }

                // encrypt the message
                byte[] encryptedBytes = encryptor.TransformFinalBlock(Encoding.UTF8.GetBytes(message), 0, message.Length);
                return Convert.ToBase64String(encryptedBytes);
            }
            catch { return string.Empty; }
        }

        public static string GetDecryptedMessage(string message)
        {
            if (message == null)
            {
                return string.Empty;
            }
            try
            {
                // get encryptor first
                ICryptoTransform decryptor = GetDecryptor();
                if (decryptor == null)
                {
                    return string.Empty;
                }

                // encrypt the message
                byte[] encryptedBytes = Convert.FromBase64String(message);
                byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return string.Empty;
            }
        }

        public static long GetEpochTime()
        {
            return (long)(DateTimeOffset.UtcNow - DateTimeOffset.UnixEpoch).TotalSeconds;
        }

        public static ICollection<Denomination> GetDenominations()
        {
            return new List<Denomination> { 
                new Denomination("Ten", "ten.jpg", 10),
                new Denomination("Twenty", "twenty.jpg", 20),
                new Denomination("Fifty", "fifty.jpg", 50),
                new Denomination("Hundred", "hundred.jpg", 100),
                new Denomination("TwoHundred", "twohundred.jpg", 200),
                new Denomination("FiveHundred", "fivehundred.jpg", 500),
                new Denomination("TwoThousand", "twothousand.jpg", 2000),
            };
        }
    }
}
