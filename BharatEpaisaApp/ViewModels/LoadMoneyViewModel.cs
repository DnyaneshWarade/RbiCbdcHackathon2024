﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using BharatEpaisaApp.Helper;
using BharatEpaisaApp.Database.Models;
using Newtonsoft.Json;

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
                if (Amount < 0)
                {
                    Error = "Enter valid amount";
                    return;
                }
                var reqId = CommonFunctions.GetEpochTime();
                var message = "{" + $"\"requestId\": \"{reqId}\",\"action\":\"loadMoney\",\"amount\": {Amount}, \"to\": {CommonFunctions.LoggedInMobileNo}, \"pin\": {CommonFunctions.LoggedInMobilePin}, \"status\": \"In Process\", \"desc\": \"Load money\"" + "}";
                //CommonFunctions.SendSmsToServer(message);
                // get the previous load money and add current one in it
                var moneyAvailableJson = await SecureStorage.Default.GetAsync("denominations");
                Collection<Denomination> moneyAvailableCollection;
                if (!string.IsNullOrWhiteSpace(moneyAvailableJson))
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
                await SecureStorage.Default.SetAsync("denominations", JsonConvert.SerializeObject(moneyAvailableCollection));
                Transaction newItem = new Transaction { ReqId = reqId.ToString(), Amount = Amount, To = CommonFunctions.LoggedInMobileNo, Status = "In Process", Desc = "Load money" };

                var navigationParameter = new Dictionary<string, object>
                                            {
                                                { "transaction", newItem }
                                            };
                await Shell.Current.GoToAsync("..", true, navigationParameter);
                ClosePopup?.Invoke(this, EventArgs.Empty);
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
