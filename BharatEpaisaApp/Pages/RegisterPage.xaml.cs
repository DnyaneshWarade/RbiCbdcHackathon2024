using BharatEpaisaApp.ViewModels;

namespace BharatEpaisaApp.Pages;

public partial class RegisterPage : ContentPage
{
	public RegisterPage(RegisterViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}