using System.Diagnostics;
using EnergyStar.Helpers;
using EnergyStar.ViewModels;
using IWshRuntimeLibrary;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel;

namespace EnergyStar.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel
    {
        get;
    }

    private bool AutoStart;
    private StartupTask? startupTask;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (RuntimeHelper.IsMSIX)
        {
            try
            {
                startupTask = StartupTask.GetAsync("EnergyStar").GetAwaiter().GetResult();
            }
            catch (Exception)
            {
            }
            if (startupTask != null && (startupTask.State == StartupTaskState.Enabled || startupTask.State == StartupTaskState.EnabledByPolicy))
            {
                AutoStart = true;
            }
            else
            {
                AutoStart = false;
            }
        }
        else
        {
            AutoStart = IsAutoStart();
        }
    }

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

    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        InitializeComponent();
        if (RuntimeHelper.IsMSIX)
        {
            try
            {
                startupTask = StartupTask.GetAsync("EnergyStar").GetAwaiter().GetResult();
            }
            catch (Exception)
            {
            }
            if (startupTask != null && (startupTask.State == StartupTaskState.Enabled || startupTask.State == StartupTaskState.EnabledByPolicy))
            {
                AutoStart = true;
            }
            else
            {
                AutoStart = false;
            }
        }
        else
        {
            AutoStart = IsAutoStart();
        }
    }

    private void Startup_Checked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (RuntimeHelper.IsMSIX)
        {
            if (startupTask != null && startupTask.State != StartupTaskState.DisabledByPolicy && startupTask.State != StartupTaskState.DisabledByUser)
            {
                AutoStart = startupTask.RequestEnableAsync().GetAwaiter().GetResult() == StartupTaskState.Enabled || startupTask.RequestEnableAsync().GetResults() == StartupTaskState.EnabledByPolicy;
            }
            else
            {
                ShowMessageBox("Error", "ManuallyDisabled".GetLocalized());
                if (sender is Microsoft.UI.Xaml.Controls.CheckBox checkbox)
                {
                    checkbox.IsChecked = false;
                }
            }
        }
        else
        {
            SetAutoStart();
            AutoStart = IsAutoStart();
        }
    }

    private void Startup_Unchecked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (RuntimeHelper.IsMSIX)
        {
            if (startupTask != null && startupTask.State != StartupTaskState.EnabledByPolicy)
            {
                startupTask.Disable();
                AutoStart = false;
            }
            else
            {
                ShowMessageBox("Error", "EnabledByPolicy".GetLocalized());
                if (sender is Microsoft.UI.Xaml.Controls.CheckBox checkbox)
                {
                    checkbox.IsChecked = true;
                }
            }
        }
        else
        {
            SetAutoStart(false);
            AutoStart = IsAutoStart();
        }
    }

    private const string QuickName = "EnergyStar X";

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
                CreateShortcut(SystemStartPath, QuickName, AppPath, "AppDescription".GetLocalized());
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

    private bool CreateShortcut(string directory, string shortcutName, string targetPath, string description = "", string iconLocation = "")
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
