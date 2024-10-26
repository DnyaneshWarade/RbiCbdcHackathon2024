using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BharatEpaisaApp.Helper;
using BharatEpaisaApp.Pages;

namespace BharatEpaisaApp.ViewModels
{
    public partial class RegisterViewModel : ObservableObject
    {
        [ObservableProperty]
        string pin;

        [ObservableProperty]
        string confirmPin;

        [ObservableProperty]
        string mobileNo;

        [ObservableProperty]
        string firstName;

        [ObservableProperty]
        string lastName;

        [ObservableProperty]
        string error;

        [ObservableProperty]
        bool isLoading = false;

        [RelayCommand]
        async Task Register()
        {
            try {
                if (validateInput())
                {
                    //var reqId = CommonFunctions.GetEpochTime();
                    //var message = "{" + $"\"requestId\": \"{reqId}\", \"action\":\"register\",\"firstName\": \"{FirstName}\", \"lastName\": \"{LastName}\", \"mobileNo\": {MobileNo}, \"pin\": {Pin}, \"balance\": 0" + "}";
                    //CommonFunctions.SendSmsToServer(message);
                    //IsLoading = true;
                    ////wait for 10 sec in order to receive sms
                    //await Task.Delay(10000);
                    ////read sms
                    //do
                    //{
                    //    var searchStr = "We are happy to inform that your account & wallet has been created successfully.";
                    //    var res = SmsResponseEvaluator.GetDygnifySms(searchStr);
                    //    if (res.ValueKind != System.Text.Json.JsonValueKind.Undefined &&
                    //        res.GetProperty("r").GetString() == reqId.ToString())
                    //    {
                    //        if (res.GetProperty("s").GetBoolean())
                    //        {
                    //            CommonFunctions.LoggedInMobileNo = MobileNo;
                    //            CommonFunctions.LoggedInMobilePin = Pin;
                    //            await SecureStorage.Default.SetAsync("mobileNo", MobileNo);
                    //            await SecureStorage.Default.SetAsync("pin", Pin);
                    //            await Shell.Current.GoToAsync($"//{nameof(MainPage)}");
                    //        } else
                    //        {
                    //            Error = "User registration failed";
                    //        }

                    //        break;
                    //    }
                    //    await Task.Delay(2000);
                    //} while (true);
                    await Shell.Current.GoToAsync($"//{nameof(MainPage)}");
                    Error = "Didn't receive registration success within time, please try after sometime";
                    IsLoading = false;
                }
                else
                {
                    Error = "Enter valid parameters";
                }
            } catch (Exception ex) { Console.WriteLine(ex); IsLoading = false; }
        }

        private bool validateInput()
        {
            Error = string.Empty;
            return CommonFunctions.ValidatePhoneNumber(MobileNo) && validatePin() &&
                        !string.IsNullOrWhiteSpace(FirstName) && !string.IsNullOrWhiteSpace(LastName);
        }

        bool validatePin()
        {
            return !string.IsNullOrWhiteSpace(Pin) && !string.IsNullOrWhiteSpace(ConfirmPin) && string.Equals(Pin, ConfirmPin);
        }

        [RelayCommand]
        void ClearError()
        {
            Error = string.Empty;
        }
    }
}
