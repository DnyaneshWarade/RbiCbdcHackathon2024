using Android.App;
using Android.Content;
using Android.Nfc;
using Android.Nfc.Tech;
using System.Text;

namespace BharatEpaisaApp.Services
{
    public partial class NfcService
    {
        private NfcAdapter _nfcAdapter;
        private Activity _activity;

        public NfcService()
        {
            _activity = Platform.CurrentActivity;
            _nfcAdapter = NfcAdapter.GetDefaultAdapter(_activity);
        }

        partial void StartListening()
        {
            if (_nfcAdapter != null)
            {
                _nfcAdapter.EnableForegroundDispatch(
                    _activity,
                    PendingIntent.GetActivity(
                        _activity,
                        0,
                        new Intent(_activity, _activity.GetType()).AddFlags(ActivityFlags.SingleTop),
                        PendingIntentFlags.Immutable),
                    new IntentFilter[] { new IntentFilter(NfcAdapter.ActionTechDiscovered) },
                    null);
            }
        }

        partial void StopListening()
        {
            _nfcAdapter?.DisableForegroundDispatch(_activity);
        }

        partial void WriteTag(string message)
        {
            var intent = _activity.Intent;

            if (NfcAdapter.ActionTechDiscovered.Equals(intent.Action))
            {
                var tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;

                if (tag == null)
                {
                    Console.WriteLine("No NFC tag detected.");
                    return;
                }

                var ndef = Ndef.Get(tag);

                if (ndef != null && ndef.IsWritable)
                {
                    var ndefMessage = new NdefMessage(new[]
                    {
                        CreateTextRecord(message, "en")
                    });

                    try
                    {
                        ndef.Connect();
                        ndef.WriteNdefMessage(ndefMessage);
                        Console.WriteLine("Message written to the NFC tag.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error writing to NFC tag: {ex.Message}");
                    }
                    finally
                    {
                        ndef.Close();
                    }
                }
                else
                {
                    Console.WriteLine("NFC tag is not writable.");
                }
            }
        }

        private NdefRecord CreateTextRecord(string text, string languageCode)
        {
            var langBytes = Encoding.UTF8.GetBytes(languageCode);
            var textBytes = Encoding.UTF8.GetBytes(text);
            var payload = new byte[1 + langBytes.Length + textBytes.Length];

            // Set status byte (length of language code + UTF-8 encoding bit)
            payload[0] = (byte)langBytes.Length;

            // Copy language code and text into payload
            System.Array.Copy(langBytes, 0, payload, 1, langBytes.Length);
            System.Array.Copy(textBytes, 0, payload, 1 + langBytes.Length, textBytes.Length);

            return new NdefRecord(
                NdefRecord.TnfWellKnown,
                NdefRecord.RtdText.ToArray(),
                new byte[0], // No ID
                payload
            );
        }

        partial void ReadTag()
        {
            var intent = _activity.Intent;

            if (NfcAdapter.ActionTagDiscovered.Equals(intent.Action))
            {
                var rawMsgs = intent.GetParcelableArrayExtra(NfcAdapter.ExtraNdefMessages);
                if (rawMsgs != null)
                {
                    var ndefMessage = (NdefMessage)rawMsgs[0];
                    var ndefRecord = ndefMessage.GetRecords()[0];

                    var payload = ndefRecord.GetPayload();
                    var message = ParseTextRecord(payload);

                    Console.WriteLine($"Received NFC message: {message}");
                    MessagingCenter.Send<object, string>(this, "NfcMessageReceived", message);
                }
            }
        }

        private string ParseTextRecord(byte[] payload)
        {
            try
            {
                var languageCodeLength = payload[0];
                var text = Encoding.UTF8.GetString(payload, languageCodeLength + 1, payload.Length - languageCodeLength - 1);
                return text;
            }
            catch
            {
                return null;
            }
        }
    }
}
