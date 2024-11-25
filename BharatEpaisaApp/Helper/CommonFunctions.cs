using BharatEpaisaApp.Database.Models;
using BharatEpaisaApp.Services;
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
        static public string WalletPublicKey;
        static private ICryptoTransform encyptCryptoTransform;
        static private ICryptoTransform decyptCryptoTransform;
        static private NewNfcService nfcService;
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

        private static NewNfcService GetNfcService()
        {
            if (nfcService != null)
            {
                return nfcService;
            }
            nfcService = new NewNfcService();
            return nfcService;
        }

        public static void StartNfcListening()
        {
            var nfc = GetNfcService();
            if (nfc == null)
            {
                return;
            }
            nfc.BeginListening();
        }

        public static void StopNfcListening()
        {
            var nfc = GetNfcService();
            if (nfc == null)
            {
                return;
            }
            nfc.StopListening();
        }

        public static async Task SendNfcMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            try
            {
                //// Check if permission is already granted
                //PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.Sms>();
                //if (status != PermissionStatus.Granted)
                //{
                //    // Request permission
                //    await Permissions.RequestAsync<Permissions.Sms>();
                //}
                var nfc = GetNfcService();
                if (nfc == null)
                {
                    return ;
                }
                nfc.Publish(Plugin.NFC.NFCNdefTypeFormat.Mime);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static async Task ReadNfcMessage()
        {
            try
            {
                //// Check if permission is already granted
                //PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.Sms>();
                //if (status != PermissionStatus.Granted)
                //{
                //    // Request permission
                //    await Permissions.RequestAsync<Permissions.Sms>();
                //}
                var nfc = GetNfcService();
                if (nfc == null)
                {
                    return;
                }
                //var message = nfc.GetMessage();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
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
