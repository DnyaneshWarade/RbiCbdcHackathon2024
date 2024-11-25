using BharatEpaisaApp.Helper;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BharatEpaisaApp.ViewModels
{
    public partial class QrCodeViewModel : ObservableObject
    {
        [ObservableProperty]
        string walletPublicKey;

        public QrCodeViewModel()
        {
            walletPublicKey = CommonFunctions.WalletPublicKey;
        }
    }
}
