using BharatEpaisaApp.Database.Models;
using BharatEpaisaApp.ViewModels;
using CommunityToolkit.Maui.Views;

namespace BharatEpaisaApp.Pages.Popups;

public partial class SendMoneyPopup : ContentPage
{
    SendMoneyViewModel vm = new SendMoneyViewModel();
    public SendMoneyPopup()
	{
		InitializeComponent();
		BindingContext = vm;
    }

    private void OnStepperValueChanged(object sender, EventArgs e)
    {
        Stepper stepper = (Stepper)sender;
        Denomination item = (Denomination)stepper.BindingContext;
        if (item != null && item.MaxLimit < item.Quantity)
        {
            --item.Quantity;
            return;
        }
        vm.OnStepperValueCahnged();
    }

    private async void ScanQr_Clicked(object sender, EventArgs e)
    {
        var scanQr = new ScanQrPopup();
        var result = await this.ShowPopupAsync(scanQr);
        if (result != null)
        {
            vm.ReceiverMobileNo = result.ToString();
        }
    }
}