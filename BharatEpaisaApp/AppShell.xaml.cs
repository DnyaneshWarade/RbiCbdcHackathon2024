using BharatEpaisaApp.Pages;
using BharatEpaisaApp.Pages.Popups;

namespace BharatEpaisaApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
            Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
            Routing.RegisterRoute(nameof(SendMoneyPopup), typeof(SendMoneyPopup));
        }
    }
}
