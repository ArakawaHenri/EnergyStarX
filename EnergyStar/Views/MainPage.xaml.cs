using EnergyStar.Helpers;
using EnergyStar.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

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
        if (((App)Microsoft.UI.Xaml.Application.Current).ESService != null && ((App)Microsoft.UI.Xaml.Application.Current).ESService.IsAlive)
        {
            EnergyStarToggle.IsChecked = true;
            EnergyStarStatusText.Text = "EnergyStar: On";
        }
        else
        {
            EnergyStarStatusText.Text = "EnergyStar: Off";
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e) => base.OnNavigatedTo(e);

    private async void ShowMessageBox(string title, string text)
    {
        ContentDialog cd = new()
        {
            Title = title,
            Content = text,
            CloseButtonText = "OK".GetLocalized(),
            XamlRoot = Content.XamlRoot
        };
        await cd.ShowAsync();
    }

    private void ToggleButton_Checked(object sender, RoutedEventArgs e)
    {
        if (Environment.OSVersion.Version.Build < 22000)
        {
            ShowMessageBox("Error", "You are running on an unsupported platform.");
            return;
        }

        try
        {
            ((App)Microsoft.UI.Xaml.Application.Current).ESService = new(new ThreadStart(EnergyManager.EnergyManager.MainService));
            ((App)Microsoft.UI.Xaml.Application.Current).ESService.Start();
            EnergyStarStatusText.Text = "EnergyStar: On";
        }
        catch (Exception ex)
        {
            ShowMessageBox("Error", ex.Message);
        }
    }

    private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
    {
        try
        {
            if (((App)Microsoft.UI.Xaml.Application.Current).ESService == null || !((App)Microsoft.UI.Xaml.Application.Current).ESService.IsAlive)
            {
                ShowMessageBox("Error", "EnergyStar is not running.");
                EnergyManager.EnergyManager.BoostAllInfluencedProcesses();
            }
            else
            {
                EnergyManager.EnergyManager.StopService();
            }
            EnergyStarStatusText.Text = "EnergyStar: Off";
        }
        catch (Exception ex)
        {
            ShowMessageBox("Error", ex.Message);
        }
    }
}
