using System.Runtime.InteropServices;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

using Windows.UI;

namespace EnergyStarX.Helpers;

// Helper class to workaround custom title bar bugs.
// DISCLAIMER: The resource key names and color values used below are subject to change. Do not depend on them.
// https://github.com/microsoft/TemplateStudio/issues/4516
internal class TitleBarHelper
{
    private const int WAINACTIVE = 0x00;
    private const int WAACTIVE = 0x01;
    private const int WMACTIVATE = 0x0006;

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, IntPtr lParam);

    public static void UpdateTitleBar(ElementTheme theme)
    {
        if (App.MainWindow.ExtendsContentIntoTitleBar)
        {
            if (theme == ElementTheme.Light || (theme == ElementTheme.Default && Application.Current.RequestedTheme == ApplicationTheme.Light))
            {
                Application.Current.Resources["WindowCaptionForeground"] = new SolidColorBrush(Colors.Black);
                Application.Current.Resources["WindowCaptionForegroundDisabled"] = new SolidColorBrush(Color.FromArgb(0x66, 0x00, 0x00, 0x00));
                Application.Current.Resources["WindowCaptionButtonBackgroundPointerOver"] = new SolidColorBrush(Color.FromArgb(0x33, 0x00, 0x00, 0x00));
                Application.Current.Resources["WindowCaptionButtonBackgroundPressed"] = new SolidColorBrush(Color.FromArgb(0x66, 0x00, 0x00, 0x00));
                Application.Current.Resources["WindowCaptionButtonStrokePointerOver"] = new SolidColorBrush(Colors.Black);
                Application.Current.Resources["WindowCaptionButtonStrokePressed"] = new SolidColorBrush(Colors.Black);
            }
            else if (theme == ElementTheme.Dark || (theme == ElementTheme.Default && Application.Current.RequestedTheme == ApplicationTheme.Dark))
            {
                Application.Current.Resources["WindowCaptionForeground"] = new SolidColorBrush(Colors.White);
                Application.Current.Resources["WindowCaptionForegroundDisabled"] = new SolidColorBrush(Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF));
                Application.Current.Resources["WindowCaptionButtonBackgroundPointerOver"] = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF));
                Application.Current.Resources["WindowCaptionButtonBackgroundPressed"] = new SolidColorBrush(Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF));
                Application.Current.Resources["WindowCaptionButtonStrokePointerOver"] = new SolidColorBrush(Colors.White);
                Application.Current.Resources["WindowCaptionButtonStrokePressed"] = new SolidColorBrush(Colors.White);
            }

            Application.Current.Resources["WindowCaptionBackground"] = new SolidColorBrush(Colors.Transparent);
            Application.Current.Resources["WindowCaptionBackgroundDisabled"] = new SolidColorBrush(Colors.Transparent);

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            if (hwnd == GetActiveWindow())
            {
                SendMessage(hwnd, WMACTIVATE, WAINACTIVE, IntPtr.Zero);
                SendMessage(hwnd, WMACTIVATE, WAACTIVE, IntPtr.Zero);
            }
            else
            {
                SendMessage(hwnd, WMACTIVATE, WAACTIVE, IntPtr.Zero);
                SendMessage(hwnd, WMACTIVATE, WAINACTIVE, IntPtr.Zero);
            }
        }
    }
}
