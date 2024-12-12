using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BharatEpaisaApp.Database;
using BharatEpaisaApp.Database.Models;
using BharatEpaisaApp.Pages.Popups;
using System.Collections.ObjectModel;
using BharatEpaisaApp.Helper;
using BharatEpaisaApp.Pages;
using System.Text;

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
                    using (var client = new HttpClient())
                    {
                        var trxContent = new StringContent(data, Encoding.UTF8, "application/json");
                        var awRes = await client.PostAsync($"{Constants.ApiURL}/transaction/processTx", trxContent);
                        if (awRes.IsSuccessStatusCode)
                        {
                            trxData.Trx.AmtColor = "#0B6623";
                            await UpdateTransaction(trxData.Trx);

                            if (trxData.Trx.IsAnonymous)
                            {
                                anonymousBalance += trxData.Trx.Amount;
                                await SecureStorage.SetAsync(Constants.AnonymousBalStr, anonymousBalance.ToString());
                            }
                            else
                            {
                                normalBalance += trxData.Trx.Amount;
                                await SecureStorage.SetAsync(Constants.NormalBalStr, normalBalance.ToString());
                            }
                            UpdateBalance();

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
            try {
                var trxData = System.Text.Json.JsonSerializer.Deserialize<Transaction>(data);
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