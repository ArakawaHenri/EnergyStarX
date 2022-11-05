using System.Reflection;
using System.Windows.Input;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using EnergyStarX.Contracts.Services;
using EnergyStarX.Helpers;
using EnergyStarX.Services;
using Microsoft.UI.Xaml;

using IWshRuntimeLibrary;

using Windows.ApplicationModel;

namespace EnergyStarX.ViewModels;

public partial class SettingsViewModel : ObservableRecipient
{
    private readonly IThemeSelectorService _themeSelectorService;
    private readonly ILocalSettingsService _localSettingsService;
    private readonly EnergyManagerService _energyManagerService;
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

    private bool _runWithSystem = false;

    public bool RunWithSystem
    {
        get => _runWithSystem;
        set => SetProperty(ref _runWithSystem, value);
    }

    private readonly StartupTask? startupTask;

    public bool _startWithSystem
    {
        get
        {
            if (RuntimeHelper.IsMSIX)
            {
                return (startupTask != null && (startupTask.State == StartupTaskState.Enabled || startupTask.State == StartupTaskState.EnabledByPolicy));
            }
            else
            {
                return IsAutoStart();
            }
        }
        set
        {
            if (RuntimeHelper.IsMSIX)
            {
                if (value)
                {
                    if (startupTask != null && startupTask.State != StartupTaskState.DisabledByPolicy && startupTask.State != StartupTaskState.DisabledByUser)
                    {
                        startupTask.RequestEnableAsync().GetAwaiter().GetResult();
                    }
                    else
                    {
                        //ShowMessageBox("Error", "ManuallyDisabled".GetLocalized());
                        //if (sender is Microsoft.UI.Xaml.Controls.CheckBox checkbox)
                        //{
                        //    checkbox.IsChecked = false;
                        //}
                    }
                }
                else
                {
                    if (startupTask != null && startupTask.State != StartupTaskState.EnabledByPolicy)
                    {
                        startupTask.Disable();
                    }
                    else
                    {
                        //ShowMessageBox("Error", "EnabledByPolicy".GetLocalized());
                        //if (sender is Microsoft.UI.Xaml.Controls.CheckBox checkbox)
                        //{
                        //    checkbox.IsChecked = true;
                        //}
                    }
                }
            }
            else
            {
                SetAutoStart(value);
            }
        }
    }

    public bool StartWithSystem
    {
        get => _startWithSystem;
        set => SetProperty(_startWithSystem, value, this, (settingsViewModel, newvalue) => settingsViewModel._startWithSystem = newvalue);
    }

    public SettingsViewModel(IThemeSelectorService themeSelectorService, ILocalSettingsService localSettingsService, EnergyManagerService energyManagerService)
    {
        _themeSelectorService = themeSelectorService;
        _localSettingsService = localSettingsService;
        _energyManagerService = energyManagerService;
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
        try
        {
            startupTask = StartupTask.GetAsync("EnergyStarX").GetAwaiter().GetResult();
        }
        catch (Exception)
        {
        }
    }

    //[RelayCommand]
    //public async Task RunOnStartChecked()
    //{
    //    await _localSettingsService.SaveSettingAsync<bool>("RunOnStart", true);
    //}

    //public async void RunOnStartUnchecked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    //{
    //    await _localSettingsService.SaveSettingAsync<bool>("RunOnStart", false);
    //}

    public bool RunOnStart
    {
        get => _energyManagerService.RunOnStart;
        set => SetProperty(_energyManagerService.RunOnStart, value, _energyManagerService, (energyManagerService, newvalue) => energyManagerService.RunOnStart = newvalue);
    }

    public bool ThrottleWhenPluggedIn
    {
        get => _energyManagerService.ThrottleWhenPluggedIn;
        set => SetProperty(_energyManagerService.ThrottleWhenPluggedIn, value, _energyManagerService, (energyManagerService, newvalue) => energyManagerService.ThrottleWhenPluggedIn = newvalue);
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

    private const string QuickName = "EnergyStarX";

    private static string SystemStartPath => Environment.GetFolderPath(Environment.SpecialFolder.Startup);

    private static string AppPath
    {
        get
        {
            if (!string.IsNullOrEmpty(Environment.ProcessPath))
            {
                return Environment.ProcessPath;
            }
            throw new Exception("MissiingPrecessPath");
        }
    }

    private static string DesktopPath => Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

    public void SetAutoStart(bool onOff = true)
    {
        if (onOff)
        {
            var shortcutPaths = GetLinkFromFolder(SystemStartPath, AppPath);
            if (shortcutPaths.Count >= 2)
            {
                for (var i = 1; i < shortcutPaths.Count; i++)
                {
                    DeleteFile(shortcutPaths[i]);
                }
            }
            else if (shortcutPaths.Count < 1)
            {
                CreateShortcut(SystemStartPath, QuickName, AppPath, "----ms-protocol:ms-encodedlaunch:App?ContractId=Windows.StartupTask&TaskId=EnergyStar", "AppDescription".GetLocalized());
            }
        }
        else
        {
            var shortcutPaths = GetLinkFromFolder(SystemStartPath, AppPath);
            if (shortcutPaths.Count > 0)
            {
                for (var i = 0; i < shortcutPaths.Count; i++)
                {
                    DeleteFile(shortcutPaths[i]);
                }
            }
        }
        //创建桌面快捷方式
        //CreateDesktopLink(desktopPath, QuickName, appAllPath);
    }

    private bool CreateShortcut(string directory, string shortcutName, string targetPath, string arguments = "", string description = "", string iconLocation = "")
    {
        try
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            var shortcutPath = Path.Combine(directory, string.Format("{0}.lnk", shortcutName));
            var shell = new IWshRuntimeLibrary.WshShell();
            var shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = targetPath;
            shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
            shortcut.Arguments = arguments;
            shortcut.WindowStyle = 1;
            shortcut.Description = description;
            shortcut.IconLocation = string.IsNullOrWhiteSpace(iconLocation) ? targetPath : iconLocation;
            shortcut.Save();
            return true;
        }
        catch (Exception ex)
        {
            var temp = ex.Message;
            temp = "";
        }
        return false;
    }

    private bool IsAutoStart()
    {
        return GetLinkFromFolder(SystemStartPath, AppPath).Count > 0;
    }

    private List<string> GetLinkFromFolder(string directory, string targetPath)
    {
        var tempStrs = new List<string>();
        tempStrs.Clear();
        string tempStr;
        var files = Directory.GetFiles(directory, "*.lnk");
        if (files == null || files.Length < 1)
        {
            return tempStrs;
        }
        for (var i = 0; i < files.Length; i++)
        {
            tempStr = GetAppPathFromLink(files[i]);
            if (tempStr.ToLowerInvariant() == targetPath.ToLowerInvariant())
            {
                tempStrs.Add(files[i]);
            }
        }
        return tempStrs;
    }

    private string GetAppPathFromLink(string shortcutPath)
    {
        if (System.IO.File.Exists(shortcutPath))
        {
            var shell = new WshShell();
            var shortct = (IWshShortcut)shell.CreateShortcut(shortcutPath);
            return shortct.TargetPath;
        }
        else
        {
            return "";
        }
    }

    private static void DeleteFile(string path)
    {
        if (System.IO.File.GetAttributes(path) == FileAttributes.Directory)
        {
            Directory.Delete(path, true);
        }
        else
        {
            System.IO.File.Delete(path);
        }
    }

    public void CreateDesktopLink(string desktopPath = "", string quickName = "", string appPath = "")
    {
        var shortcutPaths = GetLinkFromFolder(desktopPath, appPath);
        if (shortcutPaths.Count < 1)
        {
            CreateShortcut(desktopPath, quickName, appPath, "AppDescription".GetLocalized());
        }
    }
}
