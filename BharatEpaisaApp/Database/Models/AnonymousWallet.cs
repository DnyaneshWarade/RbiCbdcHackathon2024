namespace BharatEpaisaApp.Database.Models
{
    internal class AnonymousWallet
    {
        public string id { get; set; }
        public string cloudMsgToken { get; set; }
        public string publicKey { get; set; }
        public bool isActive { get; set; }

        public AnonymousWallet(string idValue, string token, string key, bool isAValue)
        {
            id = idValue;
            cloudMsgToken = token;
            publicKey = key;
            isActive = isAValue;
        }
    }
}
