using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BharatEpaisaApp.Helper;
using BharatEpaisaApp.Database.Models;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using BharatEpaisaApp.Pages.Popups;
using System.Text;

namespace BharatEpaisaApp.ViewModels
{
    public partial class SendMoneyViewModel : ObservableObject
    {
        private readonly Color primaryColorWithTransparency = Color.FromArgb("#4D007BFF");

        [ObservableProperty]
        ObservableCollection<Denomination> denominations = new ObservableCollection<Denomination>();

        [ObservableProperty]
        string receiverMobileNo;

        [ObservableProperty]
        double amount;

        [ObservableProperty]
        string pin;

        [ObservableProperty]
        string error;

        [ObservableProperty]
        private string selectedIcon;

        [ObservableProperty]
        private Color icon1BackgroundColor;

        [ObservableProperty]
        private Color icon2BackgroundColor;

        [ObservableProperty]
        private Color icon3BackgroundColor;

        [ObservableProperty]
        bool isLoading = false;

        public event EventHandler ClosePopup;

        private Collection<Denomination> _userAvailableDenominations;
        private string _denominationStr;
        public SendMoneyViewModel()
        {
            Init();
        }

        async Task Init()
        {
            var isAnonymousMode = Preferences.Get(Constants.IsAnonymousMode, false);
            _denominationStr = isAnonymousMode ? Constants.AnonymousDenominationsStr : Constants.NormalDenominationsStr;
            var moneyAvailableJson = await SecureStorage.Default.GetAsync(_denominationStr);
            _userAvailableDenominations = JsonConvert.DeserializeObject<Collection<Denomination>>(moneyAvailableJson);
            foreach (var item in _userAvailableDenominations)
            {
                //item.MaxLimit = item.MaxLimit != 0 ? item.MaxLimit : 100;
                Denominations.Add(item);
            }
        }

        [RelayCommand]
        async Task SendMoney()
        {
            Error = string.Empty;
            string? storedPin = await SecureStorage.Default.GetAsync("pin");
            if (storedPin != Pin)
            {
                Error = "Enter valid pin";
                return;
            }
            if (Amount < 0)
            {
                Error = "Enter valid amount";
                return;
            }
            var isAnonymousMode = Preferences.Get(Constants.IsAnonymousMode, false);
            if (isAnonymousMode && Amount >= 500)
            {
               await Application.Current.MainPage.DisplayAlert("Amount Exceed alert", "You cannot send more than 500 through anonymous wallet, please use KYC wallet for this transaction.", "ok");
                await Shell.Current.GoToAsync("..", true); 
            }

            IsLoading = true;
            var denominationJson = "{";
            foreach (var item in Denominations)
            {
                var note = _userAvailableDenominations.FirstOrDefault(d => d.Name == item.Name);
                if (note != null)
                {
                    denominationJson += $"\"{item.Name}\":{item.Quantity},";
                    note.MaxLimit -= item.Quantity;
                    note.Quantity = 0;
                }
            }
            denominationJson += "}";
            var reqId = CommonFunctions.GetEpochTime().ToString();
            
            var trx = new Transaction()
            {
                ReqId = reqId,
                Desc = "Send money",
                Amount = Amount,
                IsAnonymous = isAnonymousMode,
                Status = "In Progress",
                Denominations = denominationJson
            };
            ServerTransaction serverTransaction = null;
            if (!isAnonymousMode)
            {
                serverTransaction = new ServerTransaction()
                {
                    TrxId = reqId,
                    Trx = trx,
                    ReceiverPublicKey = ReceiverMobileNo,
                    SenderCloudToken = CommonFunctions.CloudMessaginToken,
                    IsAnonymous = isAnonymousMode
                };
            }
            else
            {
                var anBalStr = await SecureStorage.GetAsync(Constants.AnonymousBalStr);
                double.TryParse(anBalStr, out var abal);

                // generate the zpk proof
                var input = new
                {
                    sender_balance = abal,
                    transaction_amount = Amount,
                    balance_max = Constants.MaxAnonymousWalletBal,
                    daily_transaction_count = 2,
                    daily_transaction_limit = 50
                };
                var payload = new
                {
                    name = "sender",
                    input
                };
                using (var client = new HttpClient())
                {
                    string payloadString = System.Text.Json.JsonSerializer.Serialize(payload);
                    var zkpContent = new StringContent(payloadString, Encoding.UTF8, "application/json");
                    var awRes = await client.PostAsync($"{Constants.ApiURL}/transaction/generateProof", zkpContent);
                    if (!awRes.IsSuccessStatusCode)
                    {
                        Error = "Failed to generate the ZKP";
                        IsLoading = false;
                        return;
                    }

                    var zkpRes = await awRes.Content.ReadAsStringAsync();
                    var senderNewAccountState = new
                    {
                        reqId,
                        newBal = abal - Amount,
                        denominations = denominationJson,
                        oldBal = abal,
                        trxAmount = Amount,
                        desc = "Send Money",
                    };
                    var senderActStateStr = System.Text.Json.JsonSerializer.Serialize(senderNewAccountState);
                    var (senderEncryptedSate, senderBlind, senderIV) = CryptoOperations.EncryptWithPublicKey(CommonFunctions.WalletPublicKey, senderActStateStr);
                    var trxStr = System.Text.Json.JsonSerializer.Serialize(trx);
                    var (trxEncryptedSate, trxBlind, trxIV) = CryptoOperations.EncryptWithPublicKey(ReceiverMobileNo, trxStr);

                    serverTransaction = new ServerTransaction()
                    {
                        TrxId = reqId,
                        ReceiverPublicKey = ReceiverMobileNo,
                        SenderCloudToken = CommonFunctions.CloudMessaginToken,
                        IsAnonymous = isAnonymousMode,
                        SenderZkp = zkpRes,
                        SenderAccountState = senderEncryptedSate,
                        SenderBlind = senderBlind,
                        SenderIV = senderIV,
                        TrxEncryptedSate = trxEncryptedSate,
                        TrxBlind = trxBlind,
                        TrxIV = trxIV,
                    };
                }
            }
            if (serverTransaction != null)
            {
                using (var client = new HttpClient())
                {
                    var data = JsonConvert.SerializeObject(serverTransaction);
                    var trxContent = new StringContent(data, Encoding.UTF8, "application/json");
                    var awRes = await client.PostAsync($"{Constants.ApiURL}/transaction/senderToReceiverTx", trxContent);
                    if (awRes.IsSuccessStatusCode)
                    {
                        await SecureStorage.Default.SetAsync(_denominationStr, JsonConvert.SerializeObject(_userAvailableDenominations));

                        trx.From = CommonFunctions.LoggedInMobileNo;
                        trx.To = ReceiverMobileNo;
                        var navigationParameter = new Dictionary<string, object>
                                            {
                                                { "transaction", trx }
                                            };
                        IsLoading = false;
                        await Shell.Current.GoToAsync("..", true, navigationParameter);
                        ClosePopup?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        [RelayCommand]
        private void NfcTap()
        {
            ResetBackgroundColors();
            Icon1BackgroundColor = primaryColorWithTransparency;
            SelectedIcon = "NFC";
            CommonFunctions.StartNfcListening();
            CommonFunctions.SendNfcMessage("NFC test message");
        }

        [RelayCommand]
        private void ShareTap()
        {
            ResetBackgroundColors();
            Icon2BackgroundColor = primaryColorWithTransparency;
            SelectedIcon = "Quick Share";
        }

        [RelayCommand]
        private void DistanceTap()
        {
            ResetBackgroundColors();
            Icon3BackgroundColor = primaryColorWithTransparency;
            SelectedIcon = "Remote";
        }

        internal void OnStepperValueCahnged()
        {
            Amount = 0;
            foreach (var item in Denominations)
            {
                Amount += item.Quantity * item.Value;
            }
        }

        private void ResetBackgroundColors()
        {
            Icon1BackgroundColor = Colors.Transparent;
            Icon2BackgroundColor = Colors.Transparent;
            Icon3BackgroundColor = Colors.Transparent;
        }

        [RelayCommand]
        public async Task ScanQr()
        {
            await Shell.Current.GoToAsync(nameof(ScanQrPopup));
        }


    }
}
