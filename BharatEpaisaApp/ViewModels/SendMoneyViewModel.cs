using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BharatEpaisaApp.Helper;
using BharatEpaisaApp.Database.Models;
using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace BharatEpaisaApp.ViewModels
{
    public partial class SendMoneyViewModel : ObservableObject
    {
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
            if (!CommonFunctions.ValidatePhoneNumber(ReceiverMobileNo))
            {
                Error = "Enter valid receiver number";
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
            var message = "{" + $"\"requestId\": \"{reqId}\",\"action\":\"sendMoney\",\"amount\": {Amount}, \"from\": {CommonFunctions.LoggedInMobileNo}, \"pin\": {Pin}, \"to\": {ReceiverMobileNo}, \"desc\":\"Send money\", \"denominations\": {denominationJson.Remove(denominationJson.Length - 1) + "}"}" + "}";
            //CommonFunctions.SendEncryptedSms(ReceiverMobileNo, message);

            
            await SecureStorage.Default.SetAsync("denominations", JsonConvert.SerializeObject(_userAvailableDenominations));
            Transaction newItem = new Transaction { ReqId = reqId.ToString(), Amount = Amount, From = CommonFunctions.LoggedInMobileNo, To = ReceiverMobileNo, Status = "In Process", Desc = "Send money" };

            var navigationParameter = new Dictionary<string, object>
                                            {
                                                { "transaction", newItem }
                                            };
            await Shell.Current.GoToAsync("..", true, navigationParameter);
            ClosePopup?.Invoke(this, EventArgs.Empty);
        }

        internal void OnStepperValueCahnged()
        {
            Amount = 0;
            foreach (var item in Denominations)
            {
                Amount += item.Quantity * item.Value;
            }
        }
    }
}
