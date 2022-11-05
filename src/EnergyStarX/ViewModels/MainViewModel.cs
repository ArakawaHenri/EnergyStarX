using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnergyStarX.Services;
using Microsoft.Windows.System.Power;

namespace EnergyStarX.ViewModels;

public partial class MainViewModel : ObservableRecipient
{
    private readonly EnergyManagerService _energyManagerService;

    public MainViewModel(EnergyManagerService energyManagerService)
    {
        _energyManagerService = energyManagerService;
        EnergyManagerService.StatusChanged += EnergyManagerService_StatusChanged;
    }

    [ObservableProperty]
    private static string statusText = "EnergyStarX: Off";

    [ObservableProperty]
    private static bool isChecked = false;

    [RelayCommand]
    private void ToggleButtonChecked()
    {
        if (!isChecked)
        {
            _energyManagerService.Enable();
        }
        else
        {
            _energyManagerService.Disable();
        }
    }

    //public void ToggleButton_Checked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    //{
    //    _energyManagerService.Enable();
    //}

    //public void ToggleButton_Unchecked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    //{
    //    _energyManagerService.Disable();
    //}

    public void EnergyManagerService_StatusChanged(object? sender, EnergyManagerService.ServiceStatus e)
    {
        if (e.IsThrottling)
        {
            StatusText = "EnergyStarX: On";
            IsChecked = true;
        }
        else if (e.PowerSourceKind == PowerSourceKind.AC && e.IsEnabled)
        {
            StatusText = "EnergyStarX: Paused";
            IsChecked = true;
        }
        else
        {
            StatusText = "EnergyStarX: Off";
            IsChecked = false;
        }
    }
}
