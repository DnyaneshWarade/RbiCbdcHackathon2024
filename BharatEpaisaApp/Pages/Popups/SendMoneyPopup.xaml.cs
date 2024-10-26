using BharatEpaisaApp.Database.Models;
using BharatEpaisaApp.ViewModels;

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
}