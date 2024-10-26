using CommunityToolkit.Maui.Views;
using BharatEpaisaApp.Database.Models;
using BharatEpaisaApp.ViewModels;

namespace BharatEpaisaApp.Pages.Popups;

public partial class LoadMoneyPopup : Popup
{
    LoadMoneyViewModel vm = new LoadMoneyViewModel();
    public LoadMoneyPopup()
	{
		InitializeComponent();
		BindingContext = vm;
        vm.ClosePopup += ExecuteClosePopup;
	}

    private void ExecuteClosePopup(object sender, EventArgs e)
    {
		this.Close();
    }

    private void OnStepperValueChanged(object sender, EventArgs e)
    {
        vm.OnStepperValueCahnged();
    }
}