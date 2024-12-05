namespace BharatEpaisaApp.Database.Models
{
    public class ServerTransaction
    {
        public Transaction Trx { get; set; }
        public string ReceiverPublicKey { get; set; }
        public string ReceiverCloudToken { get; set; }
        public string ReceiverZkp { get; set; }
        public string SenderCloudToken { get; set; }
        public string SenderZkp { get; set; }
        public string Denominations { get; set; }
        public string ServerStatus { get; set; }
    }
}
