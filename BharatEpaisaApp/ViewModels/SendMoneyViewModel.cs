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
        int amount;

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
        public SendMoneyViewModel()
        {
            Init();
        }

        async Task Init()
        {
            var moneyAvailableJson = await SecureStorage.Default.GetAsync("denominations");
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
            var data = "{" + $"\"requestId\": \"{reqId}\", \"amount\": {Amount}, \"senderPublicKey\": \"{CommonFunctions.WalletPublicKey}\", \"receiverPublicKey\": \"{ReceiverMobileNo}\", \"desc\":\"Send money\", \"denominations\": {denominationJson.Remove(denominationJson.Length - 1) + "}"}, \"senderZkp\": \"todo\", \"newWalletState\": \"todo\"" + "}";

            using (var client = new HttpClient())
            {
                var trxContent = new StringContent(data, Encoding.UTF8, "application/json");
                var awRes = await client.PostAsync($"{Constants.ApiURL}/transaction/senderToReceiverTx", trxContent);
                if (awRes.IsSuccessStatusCode)
                {
                    await SecureStorage.Default.SetAsync("denominations", JsonConvert.SerializeObject(_userAvailableDenominations));
                    Transaction newItem = new Transaction { ReqId = reqId.ToString(), Amount = Amount, From = CommonFunctions.LoggedInMobileNo, To = ReceiverMobileNo, Status = "Complete", Desc = "Send money" };

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
