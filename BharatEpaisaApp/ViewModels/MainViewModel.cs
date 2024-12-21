using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BharatEpaisaApp.Database;
using BharatEpaisaApp.Database.Models;
using BharatEpaisaApp.Pages.Popups;
using System.Collections.ObjectModel;
using BharatEpaisaApp.Helper;
using BharatEpaisaApp.Pages;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BharatEpaisaApp.ViewModels
{
    public partial class MainViewModel : ObservableObject, IQueryAttributable
    {
        private readonly DatabaseContext _databaseContext;

        [ObservableProperty]
        ObservableCollection<Transaction> transactions = new ObservableCollection<Transaction>();

        [ObservableProperty]
        bool isLoading = false;

        [ObservableProperty]
        double balance = 0;

        [ObservableProperty]
        double unclearedBal = 0;

        [ObservableProperty]
        bool isAnonymousMode;

        [ObservableProperty]
        string anonymousWalletLable = "KYC Wallet";

        private double normalBalance;
        private double normalUnclearedBal;
        private double anonymousBalance;
        private double anonymousUnclearedBal;

        public MainViewModel(DatabaseContext databaseContext)
        {
            _databaseContext = databaseContext;
            LoadTransactionsAsync();
            LoadBalance();

            // Subscribe to notification click events
            MessagingCenter.Subscribe<object, string>(this, "NfcMessageReceived", async (sender, data) =>
            {
                await App.Current.MainPage.DisplayAlert("NFC message received", data, "OK");
                // Handle the notification click event
                // CheckUserReceivedTrx(data);
            });

            // Subscribe to notification click events
            MessagingCenter.Subscribe<object, string>(this, "transaction", async (sender, data) =>
            {
                // Handle the notification click event
                await CheckUserReceivedTrx(data);
            });
            MessagingCenter.Subscribe<object, string>(this, "status", async (sender, data) =>
            {
                // Handle the notification click event
                await UpdateTrxStatus(data);
            });
        }

        private async Task LoadBalance()
        {
            var nBalStr = await SecureStorage.GetAsync(Constants.NormalBalStr);
            if (double.TryParse(nBalStr, out var bal))
                normalBalance = bal;

            var normalUnclearBalStr = await SecureStorage.GetAsync(Constants.NormalUnClrBalStr);
            if (double.TryParse(normalUnclearBalStr, out var unclearBal))
                normalUnclearedBal = unclearBal;

            var anBalStr = await SecureStorage.GetAsync(Constants.AnonymousBalStr);
            if (double.TryParse(anBalStr, out var abal))
                anonymousBalance = abal;

            var anonymousUnclearBalStr = await SecureStorage.GetAsync(Constants.AnonymousUnclrBalStr);
            if (double.TryParse(anonymousUnclearBalStr, out var aUnclearBal))
                anonymousUnclearedBal = aUnclearBal;

            UpdateBalance();
        }

        public async void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            try
            {
                var trx = query["transaction"] as Transaction;
                if (trx != null)
                {
                    trx.AmtColor = trx.Status == "Complete" ? "#0B6623" : "#FEBE00";
                    await _databaseContext.AddItemAsync(trx);
                    Transactions.Insert(0, trx);
                    //CheckUserInitTrxResponses(trx);
                    if (trx.Status == "Complete")
                    {
                        if (IsAnonymousMode)
                        {
                            anonymousBalance += trx.Amount;
                            await SecureStorage.Default.SetAsync(Constants.AnonymousBalStr, anonymousBalance.ToString());
                        }
                        else
                        {
                            normalBalance += trx.Amount;
                            await SecureStorage.Default.SetAsync(Constants.NormalBalStr, normalBalance.ToString());
                        }
                    }
                    else
                    {
                        if (IsAnonymousMode)
                        {
                            anonymousUnclearedBal += trx.Amount;
                            await SecureStorage.Default.SetAsync(Constants.AnonymousUnclrBalStr, anonymousUnclearedBal.ToString());
                        }
                        else
                        {
                            normalUnclearedBal += trx.Amount;
                            await SecureStorage.Default.SetAsync(Constants.NormalUnClrBalStr, normalUnclearedBal.ToString());
                        }
                    }

                    UpdateBalance();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            query.Clear();
        }

        private void UpdateBalance()
        {
            Balance = IsAnonymousMode ? anonymousBalance : normalBalance;
            UnclearedBal = IsAnonymousMode ? anonymousUnclearedBal : normalUnclearedBal;
        }

        [RelayCommand]
        public async Task ClearData()
        {
            try
            {
                await _databaseContext.DeleteAllAsync<Transaction>();
                Transactions.Clear();
                UnclearedBal = Balance = normalBalance = normalUnclearedBal = anonymousBalance = anonymousUnclearedBal = 0;
                await SecureStorage.SetAsync(Constants.NormalUnClrBalStr, Balance.ToString());
                await SecureStorage.SetAsync(Constants.NormalBalStr, Balance.ToString());
                await SecureStorage.SetAsync(Constants.AnonymousBalStr, Balance.ToString());
                await SecureStorage.SetAsync(Constants.AnonymousUnclrBalStr, Balance.ToString());
                await SecureStorage.SetAsync(Constants.NormalDenominationsStr, string.Empty);
                await SecureStorage.SetAsync(Constants.AnonymousDenominationsStr, string.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public async Task LoadTransactionsAsync()
        {
            Transactions.Clear();
            var trxs = await _databaseContext.GetFileteredAsync<Transaction>(t => t.IsAnonymous == IsAnonymousMode);
            if (trxs is not null && trxs.Any())
            {
                foreach (var item in trxs.OrderByDescending(t => t.ReqId))
                {
                    Transactions.Add(item);
                }
            }
        }

        private async Task UpdateTransaction(Transaction trx)
        {
            try
            {
                var trxCopy = trx.Clone();
                trxCopy.Status = "Complete";
                await _databaseContext.UpdateItemAsync(trxCopy);
                var searchTrx = Transactions.FirstOrDefault(t => t.ReqId == trx.ReqId);
                var index = 0;
                if (searchTrx != null)
                {
                    index = Transactions.IndexOf(searchTrx);
                    Transactions.RemoveAt(index);
                }
                Transactions.Insert(index, trxCopy);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        [RelayCommand]
        public async Task SendMoney()
        {
            await Shell.Current.GoToAsync(nameof(SendMoneyPopup));
        }

        [RelayCommand]
        public async Task QrCode()
        {
            await Shell.Current.GoToAsync(nameof(QrCodePage));
        }

        [RelayCommand]
        public async Task ReceiveMoney()
        {
            CommonFunctions.StartNfcListening();
        }

        public void SetTheme(bool isDarkTheme)
        {
            Application.Current.UserAppTheme = isDarkTheme ? AppTheme.Dark : AppTheme.Light;
            AnonymousWalletLable = isDarkTheme ? "Anonymous Wallet" : "KYC Wallet";
            Preferences.Set(Constants.IsAnonymousMode, isDarkTheme);
            UpdateBalance();
            LoadTransactionsAsync();
        }

        private async Task CheckUserReceivedTrx(string data)
        {
            try
            {
                var trxData = System.Text.Json.JsonSerializer.Deserialize<ServerTransaction>(data);
                if (trxData != null)
                {
                    var anonymousTrxStatus = false;
                    var trxContent = new StringContent(data, Encoding.UTF8, "application/json");
                    var transactionObj = trxData.Trx;
                    if (trxData.IsAnonymous)
                    {
                        // decrypt the transaction
                        if (trxData.TrxEncryptedSate == null || trxData.TrxEncryptedSate.Length <=0 ||
                            trxData.TrxBlind == null || trxData.TrxBlind.Length <= 0)
                        {
                            // set transactin as failed
                            return;
                        }
                        string? privateKey = await SecureStorage.Default.GetAsync(Constants.PrivateKeyStr);
                        var trxStr = CryptoOperations.DecryptWithPrivateKey(privateKey, trxData.TrxEncryptedSate, trxData.TrxBlind, trxData.TrxIV);
                        var trx = System.Text.Json.JsonSerializer.Deserialize<Transaction>(trxStr);
                        if (trx == null)
                        {
                            return;
                        }
                        transactionObj = trx;
                        // generate the receiver proof
                        var input = new
                        {
                            receiver_balance = anonymousBalance,
                            transaction_amount = trx.Amount,
                            balance_max = Constants.MaxAnonymousWalletBal
                        };
                        var payload = new
                        {
                            name = "receiver",
                            input
                        };
                        using (var client = new HttpClient())
                        {
                            string payloadString = System.Text.Json.JsonSerializer.Serialize(payload);
                            var zkpContent = new StringContent(payloadString, Encoding.UTF8, "application/json");
                            var awRes = await client.PostAsync($"{Constants.ApiURL}/transaction/generateProof", zkpContent);
                            if (!awRes.IsSuccessStatusCode)
                            {
                                return;
                            }

                            var zkpRes = await awRes.Content.ReadAsStringAsync();
                            trxData.ReceiverZkp = zkpRes;

                            var receiverNewAccountState = new
                            {
                                reqId = trxData.TrxId,
                                newBal = anonymousBalance + trx.Amount,
                                denominations = trx.Denominations,
                                oldBal = anonymousBalance,
                                trxAmount = trx.Amount,
                                desc = "Received Money",
                            };
                            var receiverActStateStr = System.Text.Json.JsonSerializer.Serialize(receiverNewAccountState);
                            var (receiverEncryptedSate, receiverBlind, receiverIV) = CryptoOperations.EncryptWithPublicKey(CommonFunctions.WalletPublicKey, receiverActStateStr);
                            trxData.ReceiverAccountState = receiverEncryptedSate;
                            trxData.ReceiverBlind = receiverBlind;
                            trxData.ReceiverIV = receiverIV;

                            var completeTrxStr = JsonConvert.SerializeObject(trxData);
                            var completeTrxContent = new StringContent(completeTrxStr, Encoding.UTF8, "application/json");
                            var trxRes = await client.PostAsync($"{Constants.ApiURL}/transaction/processTx", completeTrxContent);
                            anonymousTrxStatus = trxRes.IsSuccessStatusCode;
                        }
                    }

                    if (!trxData.IsAnonymous || anonymousTrxStatus)
                    {
                        transactionObj.AmtColor = "#0B6623";
                        transactionObj.Desc = "Received";
                        transactionObj.Status = "Complete";
                        await _databaseContext.AddItemAsync<Transaction>(transactionObj);
                        
                        if (transactionObj.IsAnonymous)
                        {
                            anonymousBalance += transactionObj.Amount;
                            await SecureStorage.SetAsync(Constants.AnonymousBalStr, anonymousBalance.ToString());
                        }
                        else
                        {
                            normalBalance += transactionObj.Amount;
                            await SecureStorage.SetAsync(Constants.NormalBalStr, normalBalance.ToString());
                        }

                        await LoadTransactionsAsync();
                        UpdateBalance();

                        var moneyReceivedList = JObject.Parse(transactionObj.Denominations);
                        var denominationStr = transactionObj.IsAnonymous ? Constants.AnonymousDenominationsStr : Constants.NormalDenominationsStr;
                        var moneyAvailableJson = await SecureStorage.Default.GetAsync(denominationStr);
                        Collection<Denomination> moneyAvailableCollection;
                        if (!string.IsNullOrWhiteSpace(moneyAvailableJson))
                        {
                            moneyAvailableCollection = JsonConvert.DeserializeObject<Collection<Denomination>>(moneyAvailableJson);
                            foreach (var item in moneyAvailableCollection)
                            {
                                var note = (int)moneyReceivedList[item.Name];
                                if (note != -1)
                                {
                                    item.MaxLimit += note;
                                }
                            }
                        }
                        else
                        {
                            moneyAvailableCollection = new Collection<Denomination>();
                            foreach (var item in CommonFunctions.GetDenominations())
                            {
                                var note = (int)moneyReceivedList[item.Name];
                                if (note != -1)
                                {
                                    item.MaxLimit += note;
                                }

                                moneyAvailableCollection.Add(item);
                            }
                        }
                        await SecureStorage.Default.SetAsync(denominationStr, JsonConvert.SerializeObject(moneyAvailableCollection));

                        using (var client = new HttpClient())
                        {
                            await client.PostAsync($"{Constants.ApiURL}/transaction/receiverToSender", trxContent);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task UpdateTrxStatus(string data)
        {
            //query based on request id
            try {
                if (string.IsNullOrEmpty(data))
                {
                    return;
                }
                var trxData = await _databaseContext.GetItemByKeyAsync<Transaction>(data);
                if (trxData == null || trxData.ReqId == null || trxData.Amount == 0)
                {
                    return;
                }

                trxData.AmtColor = "#0B6623";
                await UpdateTransaction(trxData);

                if (trxData.IsAnonymous)
                {
                    anonymousBalance -= trxData.Amount;
                    anonymousUnclearedBal -= trxData.Amount;
                    await SecureStorage.SetAsync(Constants.AnonymousBalStr, anonymousBalance.ToString());
                    await SecureStorage.SetAsync(Constants.AnonymousUnclrBalStr, anonymousUnclearedBal.ToString());
                }
                else
                {
                    normalBalance -= trxData.Amount;
                    normalUnclearedBal -= trxData.Amount;
                    await SecureStorage.SetAsync(Constants.NormalBalStr, normalBalance.ToString());
                    await SecureStorage.SetAsync(Constants.NormalUnClrBalStr, normalUnclearedBal.ToString());
                }
                UpdateBalance();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}