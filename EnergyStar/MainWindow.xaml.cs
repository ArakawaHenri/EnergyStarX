using System.Runtime.InteropServices;
using EnergyStar.Helpers;
using H.NotifyIcon.Core;
using Microsoft.UI.Windowing;

namespace EnergyStar;

public sealed partial class MainWindow : WindowEx
{
    private readonly IntPtr hWnd;
    public MainWindow()
    {
        InitializeComponent();

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/WindowIcon.ico"));
        Content = null;
        Title = "AppDisplayName".GetLocalized();

        hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        appWindow.Closing += AppWindow_Closing;
    }

    //private double GetDpiScalingFactor()
    //{
    //    var windowNative = this.As<IWindowNative>();
    //    var hwnd = windowNative.WindowHandle;

    //    var dpi = GetDpiForWindow(hwnd);
    //    return (float)dpi / 96;
    //}

    //[DllImport("user32.dll")]
    //private static extern uint GetDpiForWindow(IntPtr hwnd);

    private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        args.Cancel = true;
        var window = App.MainWindow;
        if (window.Visible)
        {
            window.Hide();
        }
        Thread createIconThread = new(new ThreadStart(CreateIcon));
        createIconThread.Start();
    }

    public void CreateIcon()
    {
        using var icon = new System.Drawing.Icon(Path.Combine(AppContext.BaseDirectory, "Assets/WindowIcon.ico"));
        using var trayIcon = new TrayIconWithContextMenu
        {
            Icon = icon.Handle,
            ToolTip = "EnergyStar",
        };
        trayIcon.ContextMenu = new PopupMenu
        {
            Items =
            {
                new PopupMenuItem("Exit", (sender, args) =>
                {
                    trayIcon.Dispose();
                    Environment.Exit(0);
                }),
            },
        };
        trayIcon.Create();
        trayIcon.MainWindowHandle = hWnd;
        trayIcon.MainWindowLocker = new();
        lock (trayIcon.MainWindowLocker)
        {
            Monitor.Wait(trayIcon.MainWindowLocker);
        }
        trayIcon.Dispose();
    }
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("EECDBF0E-BAE9-4CB6-A68E-9598E1CB57BB")]
internal interface IWindowNative
{
    IntPtr WindowHandle
    {
        get;
    }
}
