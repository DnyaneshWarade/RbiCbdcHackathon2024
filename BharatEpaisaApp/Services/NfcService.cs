namespace BharatEpaisaApp.Services
{
    public partial class NfcService
    {
        partial void StartListening();
        partial void StopListening();
        partial void WriteTag(string data);
        partial void ReadTag();

        public void StartNFC()
        {
            StartListening();
        }

        public void StopNFC()
        {
            StopListening();
        }

        public void SendMessage(string data)
        {
            WriteTag(data);
        }
        public void ReadMessage()
        {
            ReadTag();
        }
    }
}
