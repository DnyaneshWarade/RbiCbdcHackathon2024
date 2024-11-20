using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BharatEpaisaApp.Database;
using BharatEpaisaApp.Database.Models;
using BharatEpaisaApp.Pages.Popups;
using System.Collections.ObjectModel;
using System.Text.Json;

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
        
        public MainViewModel(DatabaseContext databaseContext)
        {
            _databaseContext = databaseContext;
            LoadTransactionsAsync();
            LoadBalance();

            // Subscribe to notification click events
            MessagingCenter.Subscribe<object, string>(this, "NotificationClicked", (sender, data) =>
            {
                // Handle the notification click event
                CheckUserReceivedTrx(data);
            });

            // Subscribe to notification click events
            MessagingCenter.Subscribe<object, string>(this, "status", async (sender, data) =>
            {
                // Handle the notification click event
                await CheckUserReceivedTrx(data);
            });
        }

        private async Task LoadBalance()
        {
            var balStr = await SecureStorage.GetAsync("balance");
            if (double.TryParse(balStr, out var bal))
                Balance = bal;

            var unclearBalStr = await SecureStorage.GetAsync("unclearedBal");
            if (double.TryParse(unclearBalStr, out var unclearBal))
                UnclearedBal = unclearBal;

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
                    UnclearedBal += trx.Amount;
                    await SecureStorage.Default.SetAsync("unclearedBal", UnclearedBal.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        [RelayCommand]
        public async Task ClearData()
        {
            try
            {
                await _databaseContext.DeleteAllAsync<Transaction>();
                Transactions.Clear();
                UnclearedBal = Balance = 0;
                await SecureStorage.SetAsync("unclearedBal", UnclearedBal.ToString());
                await SecureStorage.SetAsync("balance", Balance.ToString());
                await SecureStorage.SetAsync("denominations", string.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public async Task LoadTransactionsAsync()
        {
            var trxs = await _databaseContext.GetAllAsync<Transaction>();

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
                var index = Transactions.IndexOf(trx);
                Transactions.RemoveAt(index);
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

        public void SetTheme(bool isDarkTheme)
        {
            Application.Current.UserAppTheme = isDarkTheme ? AppTheme.Dark : AppTheme.Light;
        }

        private async Task CheckUserReceivedTrx(string data)
        {
            Dictionary<string, string> dataDict = JsonSerializer.Deserialize<Dictionary<string, string>>(data);
            var rId = dataDict["requestId"];
            var sendMonetTrx = Transactions.FirstOrDefault(t => t.ReqId == rId, null);
            if (sendMonetTrx != null && sendMonetTrx.Status != "Complete" && dataDict["status"] == "success")
            {
                sendMonetTrx.AmtColor = "#0B6623";
                await UpdateTransaction(sendMonetTrx);
                Balance += sendMonetTrx.Amount;
                UnclearedBal -= sendMonetTrx.Amount;
                await SecureStorage.SetAsync("balance", Balance.ToString());
                await SecureStorage.SetAsync("unclearedBal", UnclearedBal.ToString());
            }
        }

    }
}