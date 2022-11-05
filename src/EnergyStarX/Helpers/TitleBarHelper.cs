using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Windows.Win32;

namespace EnergyStarX.Helpers;

// Helper class to workaround custom title bar bugs.
// DISCLAIMER: The resource key names and color values used below are subject to change. Do not depend on them.
// https://github.com/microsoft/TemplateStudio/issues/4516
internal class TitleBarHelper
{
    private const uint WAINACTIVE = 0x00u;
    private const uint WAACTIVE = 0x01u;
    private const uint WMACTIVATE = 0x06u;

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

            var hwnd = new Windows.Win32.Foundation.HWND(WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow));
            if (hwnd == PInvoke.GetActiveWindow())
            {
                PInvoke.SendMessage(hwnd, WMACTIVATE, WAINACTIVE, IntPtr.Zero);
                PInvoke.SendMessage(hwnd, WMACTIVATE, WAACTIVE, IntPtr.Zero);
            }
            else
            {
                PInvoke.SendMessage(hwnd, WMACTIVATE, WAACTIVE, IntPtr.Zero);
                PInvoke.SendMessage(hwnd, WMACTIVATE, WAINACTIVE, IntPtr.Zero);
            }
        }
    }
}
