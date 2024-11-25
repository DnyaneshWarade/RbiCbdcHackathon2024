using Microsoft.Extensions.Logging;
using BharatEpaisaApp.Pages;
using BharatEpaisaApp.Pages.Popups;
using BharatEpaisaApp.ViewModels;
using CommunityToolkit.Maui;
using BharatEpaisaApp.Database;
using ZXing.Net.Maui.Controls;

namespace BharatEpaisaApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseBarcodeReader()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            builder.Services.AddSingleton<LoginPage>();
            builder.Services.AddSingleton<LoginViewModel>();
            builder.Services.AddSingleton<RegisterPage>();
            builder.Services.AddSingleton<RegisterViewModel>();
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<MainViewModel>();
            builder.Services.AddSingleton<LoadMoneyViewModel>();
            builder.Services.AddSingleton<DatabaseContext>();
            builder.Services.AddTransient<SendMoneyPopup>();

            return builder.Build();
        }
    }
}
