using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using BharatEpaisaApp.Helper;
using BharatEpaisaApp.Database.Models;
using Newtonsoft.Json;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;

namespace BharatEpaisaApp.ViewModels
{
    public partial class LoadMoneyViewModel : ObservableObject
    {
        [ObservableProperty]
        ObservableCollection<Denomination> denominations = new ObservableCollection<Denomination>();

        [ObservableProperty]
        int amount;

        [ObservableProperty]
        string error;

        public event EventHandler ClosePopup;

        public LoadMoneyViewModel()
        {
            foreach (var item in CommonFunctions.GetDenominations())
            {
                Denominations.Add(item);
            }
        }

        [RelayCommand]
        async Task LoadMoney()
        {
            try
            {
                Error = string.Empty;
                if (Amount <= 0)
                {
                    Error = "Enter valid amount";
                    return;
                }
                var isAnonymousMode = Preferences.Get(Constants.IsAnonymousMode, false);
                var isAMLoadMoneySuccessful = false;
                var reqId = CommonFunctions.GetEpochTime();

                // get the previous load money and add current one in it
                var denominations = isAnonymousMode ? Constants.AnonymousDenominationsStr : Constants.NormalDenominationsStr;
                var moneyAvailableJson = await SecureStorage.Default.GetAsync(denominations);
                Collection<Denomination> moneyAvailableCollection;
                if (!string.IsNullOrWhiteSpace(moneyAvailableJson) && moneyAvailableJson != "null")
                {
                    moneyAvailableCollection = JsonConvert.DeserializeObject<Collection<Denomination>>(moneyAvailableJson);
                    foreach (var item in moneyAvailableCollection)
                    {
                        var note = Denominations.FirstOrDefault(d => d.Name == item.Name);
                        if (note != null)
                        {
                            item.MaxLimit += note.Quantity;
                        }
                    }
                }
                else
                {
                    moneyAvailableCollection = new Collection<Denomination>();
                    foreach (var item in Denominations)
                    {
                        item.MaxLimit = item.Quantity;
                        item.Quantity = 0;
                        moneyAvailableCollection.Add(item);
                    }
                }

                var denominationsStr = JsonConvert.SerializeObject(moneyAvailableCollection);
                if (isAnonymousMode)
                {
                    //load money api call
                    using (var client = new HttpClient())
                    {
                        var anBalStr = await SecureStorage.GetAsync(Constants.AnonymousBalStr);
                        double.TryParse(anBalStr, out var abal);

                        // geenrate the zpk proof
                        var input = new
                        {
                            balance = abal,
                            amount = Amount,
                            balance_max = Constants.MaxAnonymousWalletBal
                        };
                        var payload = new
                        {
                            name = "load_money",
                            input
                        };
                        string payloadString = System.Text.Json.JsonSerializer.Serialize(payload);
                        var zkpContent = new StringContent(payloadString, Encoding.UTF8, "application/json");
                        var awRes = await client.PostAsync($"{Constants.ApiURL}/transaction/generateProof", zkpContent);
                        if (!awRes.IsSuccessStatusCode)
                        {
                            Error = "Failed to generate the ZKP";
                            return;
                        }

                        var token = Preferences.Get("DeviceToken", "");
                        var zkpRes = await awRes.Content.ReadAsStringAsync();

                        var newAccountState = new
                        {
                            reqId,
                            newBal = abal + Amount,
                            denominations = denominationsStr,
                            oldBal = abal,
                            trxAmount = Amount,
                            desc = "Load Money",
                        };
                        var actStateStr = System.Text.Json.JsonSerializer.Serialize(newAccountState);
                        var (encryptedSate, blind) = CryptoOperations.EncryptWithPublicKey(CommonFunctions.WalletPublicKey, actStateStr);
                        var data = new
                        {
                            requestId = reqId,
                            token,
                            zkp = zkpRes,
                            accountState = encryptedSate,
                            blind
                        };
                        var dataStr = System.Text.Json.JsonSerializer.Serialize(data);
                        var trxContent = new StringContent(dataStr, Encoding.UTF8, "application/json");
                        awRes = await client.PostAsync($"{Constants.ApiURL}/transaction/loadMoney", trxContent);
                        isAMLoadMoneySuccessful = awRes.IsSuccessStatusCode;
                        if (!isAMLoadMoneySuccessful)
                        {
                            Error = "Anonymous load money transaction failed";
                            return;
                        }
                    }
                }

                if (!isAnonymousMode || isAMLoadMoneySuccessful)
                {
                    await SecureStorage.Default.SetAsync(denominations, denominationsStr);
                    Transaction newItem = new Transaction { ReqId = reqId.ToString(), Amount = Amount, To = CommonFunctions.LoggedInMobileNo, Status = "Complete", Desc = "Load money", IsAnonymous = isAnonymousMode };

                    var navigationParameter = new Dictionary<string, object>
                                            {
                                                { "transaction", newItem }
                                            };
                    await Shell.Current.GoToAsync("..", true, navigationParameter);
                    ClosePopup?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                Error = "Failed to Load Money";
                Console.WriteLine(ex.Message);
            }
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
