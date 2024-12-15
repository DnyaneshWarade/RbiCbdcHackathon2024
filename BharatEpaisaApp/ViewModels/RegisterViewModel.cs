using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BharatEpaisaApp.Helper;
using BharatEpaisaApp.Pages;
using System.Text;
using BharatEpaisaApp.Database.Models;
using Newtonsoft.Json;

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
                    // Create the private account and store in keystore
                    (string publicKey, string privateKey) = CryptoOperations.GenerateECCKeyPair();
                    CommonFunctions.WalletPublicKey = publicKey;
                    await SecureStorage.SetAsync(Constants.PublicKeyStr, publicKey);
                    await SecureStorage.SetAsync(Constants.PrivateKeyStr, privateKey);
                    string deviceModel = DeviceInfo.Model;

                    // First store the anonymous wallet in details in server
                    var token = Preferences.Get("DeviceToken", "");
                    CommonFunctions.CloudMessaginToken = token;
                    AnonymousWallet aWallet = new AnonymousWallet("", token, publicKey, true);
                    string awPayload = JsonConvert.SerializeObject(aWallet);
                    HttpClient client = new HttpClient();
                    var awContent = new StringContent(awPayload, Encoding.UTF8, "application/json");
                    var awRes = await client.PostAsync($"{Constants.ApiURL}/user/updateUserCloudMsgToken", awContent);
                    if (awRes.IsSuccessStatusCode)
                    {
                        var hash = CryptoOperations.ComputeSha256Hash(mobileNo);
                        User user = new User("", firstName, lastName, mobileNo, pin, hash, deviceModel, true);
                        string payload = JsonConvert.SerializeObject(user);
                        var content = new StringContent(payload, Encoding.UTF8, "application/json");
                        var res = await client.PostAsync($"{Constants.ApiURL}/user/updateUser", content);
                        if (res.IsSuccessStatusCode)
                        {
                            CommonFunctions.LoggedInMobileNo = MobileNo;
                            CommonFunctions.LoggedInMobilePin = Pin;
                            await SecureStorage.Default.SetAsync("mobileNo", MobileNo);
                            await SecureStorage.Default.SetAsync("pin", Pin);
                            await Shell.Current.GoToAsync($"//{nameof(MainPage)}");
                        }
                    }
                    else
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
