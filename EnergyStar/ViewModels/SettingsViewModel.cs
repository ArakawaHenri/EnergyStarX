using System.Reflection;
using System.Windows.Input;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using EnergyStar.Contracts.Services;
using EnergyStar.Helpers;

using Microsoft.UI.Xaml;

using Windows.ApplicationModel;

namespace EnergyStar.ViewModels;

public class SettingsViewModel : ObservableRecipient
{
    private readonly IThemeSelectorService _themeSelectorService;
    private readonly ISettingsService _settingsService;
    private ElementTheme _elementTheme;
    private string _versionDescription;

    public ElementTheme ElementTheme
    {
        get => _elementTheme;
        set => SetProperty(ref _elementTheme, value);
    }

    public string VersionDescription
    {
        get => _versionDescription;
        set => SetProperty(ref _versionDescription, value);
    }

    public ICommand SwitchThemeCommand
    {
        get;
    }

    public SettingsViewModel(IThemeSelectorService themeSelectorService, ISettingsService settingsService)
    {
        _settingsService = settingsService;
        _themeSelectorService = themeSelectorService;
        _elementTheme = _themeSelectorService.Theme;
        _versionDescription = GetVersionDescription();

        SwitchThemeCommand = new RelayCommand<ElementTheme>(
            async (param) =>
            {
                if (ElementTheme != param)
                {
                    ElementTheme = param;
                    await _themeSelectorService.SetThemeAsync(param);
                }
            });
    }

    public async void AlwaysThrottle_Checked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await _settingsService.SaveSettingsAsync("AlwaysThrottle", "true");
        EnergyManager.EnergyManager.AlwaysThrottle = true;
    }

    public async void AlwaysThrottle_Unchecked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await _settingsService.SaveSettingsAsync("AlwaysThrottle", "false");
        EnergyManager.EnergyManager.AlwaysThrottle = false;
    }

    private static string GetVersionDescription()
    {
        Version version;

        if (RuntimeHelper.IsMSIX)
        {
            var packageVersion = Package.Current.Id.Version;

            version = new(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
        }
        else
        {
            version = Assembly.GetExecutingAssembly().GetName().Version!;
        }

        return $"{"AppDisplayName".GetLocalized()} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }
}
