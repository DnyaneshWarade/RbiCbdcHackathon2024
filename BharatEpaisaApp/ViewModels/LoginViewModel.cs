using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using BharatEpaisaApp.Database;
using BharatEpaisaApp.Helper;
using BharatEpaisaApp.Pages;

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
            //var dbContext = new AppDbContext();
            //dbContext.Database.Migrate();
        }

        [RelayCommand]
        async Task Login()
        {
            if (validateInput())
            {
                IsLoading = true;
                string storedMobileNo = await SecureStorage.Default.GetAsync("mobileNo");
                string storedPin = await SecureStorage.Default.GetAsync("pin");
                //if (storedMobileNo == MobileNo && storedPin == Pin)
                {
                    CommonFunctions.LoggedInMobileNo = MobileNo;
                    CommonFunctions.LoggedInMobilePin = Pin;
                    await Shell.Current.GoToAsync($"//{nameof(MainPage)}");
                }
                //else
                //{
                //    Error = "Invalid mobile number or pin";
                //}
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
