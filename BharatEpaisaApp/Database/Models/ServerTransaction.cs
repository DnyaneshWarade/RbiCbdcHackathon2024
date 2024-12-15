namespace BharatEpaisaApp.Database.Models
{
    public class ServerTransaction
    {
        public string TrxId { get; set; }
        public Transaction Trx { get; set; }
        public string ReceiverPublicKey { get; set; }
        public string ReceiverCloudToken { get; set; }
        public string ReceiverZkp { get; set; }
        public byte[] ReceiverAccountState { get; set; }
        public byte[] ReceiverBlind { get; set; }
        public byte[] ReceiverIV { get; set; }
        public string SenderCloudToken { get; set; }
        public string SenderZkp { get; set; }
        public byte[] SenderAccountState { get; set; }
        public byte[] SenderBlind { get; set; }
        public byte[] SenderIV { get; set; }
        public byte[] TrxEncryptedSate { get; set; }
        public byte[] TrxBlind { get; set; }
        public byte[] TrxIV { get; set; }
        public string ServerStatus { get; set; }
        public bool IsAnonymous { get; set; }
    }
}
