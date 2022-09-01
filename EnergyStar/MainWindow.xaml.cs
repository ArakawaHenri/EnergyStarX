using EnergyStar.Helpers;
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
    }
}
