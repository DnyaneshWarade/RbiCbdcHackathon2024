namespace BharatEpaisaApp.Helper
{
    internal static class Constants
    {
        static public readonly string ApiURL = @"https://asia-southeast1-rbicbdchackathon2024.cloudfunctions.net/api";
        //static public readonly string ApiURL = @"http://127.0.0.1:5001/rbicbdchackathon2024/asia-southeast1/api";
        static public readonly string MIME_TYPE = "application/com.dygnify.bharaterupee";
        static public readonly string NormalBalStr = "NormalBalance";
        static public readonly string NormalUnClrBalStr = "NormalUnclearedBal";
        static public readonly string AnonymousBalStr = "AnonymousBalance";
        static public readonly string AnonymousUnclrBalStr = "AnonymousUnclearedBal";
        static public readonly string AnonymousDenominationsStr = "AnonymousDenominations";
        static public readonly string NormalDenominationsStr = "NormalDenominations";
        static public readonly string IsAnonymousMode = "IsAnonymousMode";
        static public readonly string PublicKeyStr = "ECC_PublicKey";
        static public readonly string PrivateKeyStr = "ECC_PrivateKey";
        static public readonly double MaxAnonymousWalletBal = 2000;
    }
}
