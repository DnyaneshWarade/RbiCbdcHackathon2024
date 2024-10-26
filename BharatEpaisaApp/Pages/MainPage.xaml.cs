using CommunityToolkit.Maui.Views;
using BharatEpaisaApp.Pages.Popups;
using BharatEpaisaApp.ViewModels;

namespace BharatEpaisaApp.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        private void LoadMoney_Clicked(object sender, EventArgs e)
        {
            this.ShowPopup(new LoadMoneyPopup());
        }
    }
}