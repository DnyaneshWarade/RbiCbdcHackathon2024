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
                item.MaxLimit = item.MaxLimit != 0 ? item.MaxLimit : 100;
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
            if(Amount < 0)
            {
                Error = "Enter valid amount";
                return;
            }

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
            var reqId = CommonFunctions.GetEpochTime();
            var isAnonymousMode = Preferences.Get(Constants.IsAnonymousMode, false);
            var trx = new Transaction()
            {
                ReqId = reqId.ToString(),
                Desc = "Send money",
                Amount = Amount,
                IsAnonymous = isAnonymousMode,
                Status = "In Progress"
            };
            var serverTrx = new ServerTransaction()
            {
                Trx = trx,
                Denominations = denominationJson,
                ReceiverPublicKey = ReceiverMobileNo,
                SenderCloudToken = CommonFunctions.CloudMessaginToken,
                SenderZkp = "ToDo"
            };

            using (var client = new HttpClient())
            {
                var data = JsonConvert.SerializeObject(serverTrx);
                var trxContent = new StringContent(data, Encoding.UTF8, "application/json");
                var awRes = await client.PostAsync($"{Constants.ApiURL}/transaction/senderToReceiverTx", trxContent);
                if (awRes.IsSuccessStatusCode)
                {
                    await SecureStorage.Default.SetAsync(_denominationStr, JsonConvert.SerializeObject(_userAvailableDenominations));
                    Transaction newItem = new Transaction { ReqId = reqId.ToString(), Amount = Amount, From = CommonFunctions.LoggedInMobileNo, To = ReceiverMobileNo, Status = "In Progress", Desc = "Send money" };

                    var navigationParameter = new Dictionary<string, object>
                                            {
                                                { "transaction", newItem }
                                            };
                    await Shell.Current.GoToAsync("..", true, navigationParameter);
                    ClosePopup?.Invoke(this, EventArgs.Empty);
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
