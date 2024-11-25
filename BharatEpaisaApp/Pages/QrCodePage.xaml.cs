using BharatEpaisaApp.ViewModels;

namespace BharatEpaisaApp.Pages;

public partial class QrCodePage : ContentPage
{
    QrCodeViewModel vm = new QrCodeViewModel();

    public QrCodePage()
	{
		InitializeComponent();
        BindingContext = vm;
    }
}