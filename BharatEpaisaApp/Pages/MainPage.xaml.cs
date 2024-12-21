using CommunityToolkit.Maui.Views;
using BharatEpaisaApp.Pages.Popups;
using BharatEpaisaApp.ViewModels;
using HybridWebView;
using System.Text.Json.Nodes;
using System.Text.Json;

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

        private async void LoadMoney_Clicked(object sender, EventArgs e)
        {
            this.ShowPopup(new LoadMoneyPopup());
            //var input = new
            //{
            //    balance= 500,
            // amount = 200,
            // balance_max = 2000
            //};
            //string inputString = JsonSerializer.Serialize(input);
            ////var output = await myHybridWebView.InvokeJsMethodAsync<string>("calculateProof", "load_money", inputString);
            //var output = await myHybridWebView.InvokeJavaScriptAsync<Dictionary<string, string>>
            //    ("calculateProof", HybridSampleJSContext.Default.DictionaryStringString, "load_money", inputString);
            //await Dispatcher.DispatchAsync(async () =>
            //{
            //    await DisplayAlert("JS Message", output.ToString(), "Ok");
            //});
        }

        private void Switch_Toggled(object sender, ToggledEventArgs e)
        {
            viewModel.SetTheme(e.Value);
        }

        private async void HybridWebView_RawMessageReceived(object sender, HybridWebView.HybridWebViewRawMessageReceivedEventArgs e)
        {
            var message = e.Message;
            await Dispatcher.DispatchAsync(async () =>
            {
                await DisplayAlert("JS Message", message, "Ok");
            });
        }
    }
}