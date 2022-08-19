using EnergyStar.Helpers;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using WinRT;
using System;
using System.Runtime.InteropServices;

namespace EnergyStar;

public sealed partial class MainWindow : WindowEx
{
    public MainWindow()
    {
        InitializeComponent();

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/WindowIcon.ico"));
        Content = null;
        Title = "AppDisplayName".GetLocalized();

        IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

        appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = (int)(1150.0 * GetDpiScalingFactor()), Height = (int)(600.0 * GetDpiScalingFactor()) });
    }

    private double GetDpiScalingFactor()
    {
        var windowNative = this.As<IWindowNative>();
        var hwnd = windowNative.WindowHandle;

        var dpi = GetDpiForWindow(hwnd);
        return (float)dpi / 96;
    }

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);
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
