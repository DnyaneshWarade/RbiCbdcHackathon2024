using CommunityToolkit.Maui.Views;
using ZXing.Net.Maui;
using ZXing.QrCode.Internal;

namespace BharatEpaisaApp.Pages.Popups;

public partial class ScanQrPopup : Popup
{
	public ScanQrPopup()
	{
		InitializeComponent();
	}

    protected void BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        cameraBarcodeReaderView.IsDetecting = false;

        foreach (var barcode in e.Results)
        {
            Console.WriteLine($"Barcodes: {barcode.Format} -> {barcode.Value}");
            this.Close(barcode.Value);
            break;
        }
    }
}