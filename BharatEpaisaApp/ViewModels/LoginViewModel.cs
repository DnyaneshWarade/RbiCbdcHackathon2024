using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BharatEpaisaApp.Helper;
using BharatEpaisaApp.Pages;
using BharatEpaisaApp.Database.Models;
using Newtonsoft.Json;
using System.Text;

namespace BharatEpaisaApp.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        [ObservableProperty]
        string pin;

        [ObservableProperty]
        string mobileNo;

        [ObservableProperty]
        string error;

        [ObservableProperty]
        bool isLoading = false;

        public LoginViewModel()
        {
        }

        [RelayCommand]
        async Task Login()
        {
            if (validateInput())
            {
                IsLoading = true;
                string? storedMobileNo = await SecureStorage.Default.GetAsync("mobileNo");
                string? storedPin = await SecureStorage.Default.GetAsync("pin");
                
                if (storedMobileNo == MobileNo && storedPin == Pin)
                {
                    // get the cloud token
                    string? publicKey = await SecureStorage.Default.GetAsync("ECC_PublicKey");
                    if (!string.IsNullOrEmpty(publicKey))
                    {
                        CommonFunctions.WalletPublicKey = publicKey;
                        using (HttpClient client = new HttpClient())
                        {
                            try
                            {
                                var url = $"{Constants.ApiURL}/user/getUserCloudMsgToken?publicKey={publicKey}";
                                // Send GET request
                                HttpResponseMessage response = await client.GetAsync(url);

                                // Read and output the response content
                                AnonymousWallet? awObj = null;
                                if (response.IsSuccessStatusCode)
                                {
                                    string responseBody = await response.Content.ReadAsStringAsync();
                                    awObj = JsonConvert.DeserializeObject<AnonymousWallet>(responseBody);
                                }
                                var token = Preferences.Get("DeviceToken", "");
                                if (awObj == null || awObj.cloudMsgToken != token)
                                {
                                    if (awObj == null)
                                        awObj = new AnonymousWallet("", token, publicKey, true);
                                    else
                                        awObj.cloudMsgToken = token;

                                    string awPayload = JsonConvert.SerializeObject(awObj);
                                    var awContent = new StringContent(awPayload, Encoding.UTF8, "application/json");
                                    var awRes = await client.PostAsync($"{Constants.ApiURL}/user/updateUserCloudMsgToken", awContent);
                                    if (awRes.IsSuccessStatusCode)
                                    {
                                        Console.WriteLine("token updated successfully");
                                    }
                                }
                            }
                            catch (HttpRequestException e)
                            {
                                Console.WriteLine($"Request error: {e.Message}");
                            }
                        }
                    }

                    CommonFunctions.LoggedInMobileNo = MobileNo;
                    CommonFunctions.LoggedInMobilePin = Pin;
                    await Shell.Current.GoToAsync($"//{nameof(MainPage)}");
                }
                else
                {
                    Error = "Invalid mobile number or pin";
                }
                IsLoading = false;
            }
            else
            {
                Error = "Enter valid mobile no. and pin";
            }
        }

        private bool validateInput()
        {
            Error = string.Empty;
            return CommonFunctions.ValidatePhoneNumber(MobileNo) && !string.IsNullOrWhiteSpace(Pin);
        }

        [RelayCommand]
        void ClearError()
        {
            Error = string.Empty;
        }

        [RelayCommand]
        async Task Register()
        {
           await Shell.Current.GoToAsync(nameof(RegisterPage));
        }
    }
}
