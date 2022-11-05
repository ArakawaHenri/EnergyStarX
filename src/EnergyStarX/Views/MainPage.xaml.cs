using EnergyStarX.Helpers;
using EnergyStarX.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace EnergyStarX.Views;

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
        if (Environment.OSVersion.Version.Build >= 22000)
        {
            PlatformNote.Message = "SupportedSystem".GetLocalized();
            PlatformNote.Severity = InfoBarSeverity.Success;
            MainToggle.IsEnabled = true;
        }
    }

    //protected override void OnNavigatedTo(NavigationEventArgs e)
    //{
    //    base.OnNavigatedTo(e);
    //    ViewModel.EnergyManagerService_StatusChanged(this, EnergyManagerService.Status);
    //}

    //protected override void OnNavigatedFrom(NavigationEventArgs e)
    //{
    //    base.OnNavigatedFrom(e);
    //    ViewModel.EnergyManagerService_StatusChanged(this, EnergyManagerService.Status);
    //}
}
