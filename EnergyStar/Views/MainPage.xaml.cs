using EnergyStar.ViewModels;

using Microsoft.UI.Xaml.Controls;
using Windows.Services.Maps;

namespace EnergyStar.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel
    {
        get;
    }

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        InitializeComponent();
    }

    private static Thread? ESService;

    private async void ShowMessageBox(string title, string text)
    {
        ContentDialog cd = new()
        {
            Title = title,
            Content = text,
            CloseButtonText = "OK",
            XamlRoot = Content.XamlRoot
        };
        await cd.ShowAsync();
    }

    private void StartService_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ESService != null && ESService.IsAlive)
        {
            ShowMessageBox("Thread is already running", "EnergyStar is already running.");
        }
        else
        {
            // Well, this program only works for Windows Version starting with Cobalt...
            // Nickel or higher will be better, but at least it works in Cobalt
            //
            // In .NET 5.0 and later, System.Environment.OSVersion always returns the actual OS version.
            if (Environment.OSVersion.Version.Build < 22000)
            {
                ShowMessageBox("Unsupported platform", "Windows 11 22H2 (or above) and modern chips are required.");
                // ERROR_CALL_NOT_IMPLEMENTED
                return;
            }
            ESService = new(new ThreadStart(EnergyManager.MainService));
            ESService.Start();
        }
    }

    private void StopService_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ESService == null || !ESService.IsAlive)
        {
            ShowMessageBox("Thread not running", "EnergyStar is not running.");
            EnergyManager.BoostAllInfluencedProcesses();
        }
        else
        {
            EnergyManager.StopService();
        }
    }
}
