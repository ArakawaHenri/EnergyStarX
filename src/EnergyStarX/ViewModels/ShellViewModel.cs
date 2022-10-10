using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnergyStarX.Contracts.Services;
using EnergyStarX.Helpers;
using EnergyStarX.Views;
using Microsoft.UI.Xaml.Navigation;

namespace EnergyStarX.ViewModels;

public partial class ShellViewModel : ObservableRecipient
{
    private bool _isBackEnabled;
    private object? _selected;

    public INavigationService NavigationService
    {
        get;
    }

    public INavigationViewService NavigationViewService
    {
        get;
    }

    public bool IsBackEnabled
    {
        get => _isBackEnabled;
        set => SetProperty(ref _isBackEnabled, value);
    }

    public object? Selected
    {
        get => _selected;
        set => SetProperty(ref _selected, value);
    }

    [ObservableProperty]
    private System.Drawing.Icon taskbarIcon;

    [ObservableProperty]
    private string taskbarIconToolTip = "AppDisplayName".GetLocalized();

    [RelayCommand]
    private static void ShowWindow()
    {
        App.MainWindow.Activate();
        App.MainWindow.BringToFront();
    }

    [RelayCommand]
    private static void Exit()
    {
        Environment.Exit(0);
    }

    public ShellViewModel(INavigationService navigationService, INavigationViewService navigationViewService)
    {
        NavigationService = navigationService;
        NavigationService.Navigated += OnNavigated;
        NavigationViewService = navigationViewService;
        taskbarIcon = new System.Drawing.Icon(Path.Combine(AppContext.BaseDirectory, "Assets/WindowIcon.ico"));
    }

    private void OnNavigated(object sender, NavigationEventArgs e)
    {
        IsBackEnabled = NavigationService.CanGoBack;

        if (e.SourcePageType == typeof(SettingsPage))
        {
            Selected = NavigationViewService.SettingsItem;
            return;
        }

        var selectedItem = NavigationViewService.GetSelectedItem(e.SourcePageType);
        if (selectedItem != null)
        {
            Selected = selectedItem;
        }
    }
}
