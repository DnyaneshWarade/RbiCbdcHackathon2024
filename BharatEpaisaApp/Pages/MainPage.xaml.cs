using CommunityToolkit.Maui.Views;
using BharatEpaisaApp.Pages.Popups;
using BharatEpaisaApp.ViewModels;

namespace BharatEpaisaApp.Pages
{
    public partial class MainPage : ContentPage
    {
        MainViewModel viewModel;
        public MainPage(MainViewModel vm)
        {
            InitializeComponent();
            BindingContext = viewModel = vm;
        }

        private void LoadMoney_Clicked(object sender, EventArgs e)
        {
            this.ShowPopup(new LoadMoneyPopup());
        }

        private void Switch_Toggled(object sender, ToggledEventArgs e)
        {
            viewModel.SetTheme(e.Value);
        }
    }
}