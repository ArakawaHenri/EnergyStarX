using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;

namespace EnergyStarX.Helpers;

internal class HookManager
{
    private const uint WINEVENT_INCONTEXT = 0b100u;
    private const uint WINEVENT_OUTOFCONTEXT = 0b000u;
    private const uint WINEVENT_SKIPOWNPROCESS = 0b010u;
    private const uint WINEVENT_SKIPOWNTHREAD = 0b001u;

    private const uint EVENT_SYSTEM_FOREGROUND = 3u;

    private static UnhookWinEventSafeHandle windowEventHook = new((IntPtr)0);
    // Explicitly declare it to prevent GC
    // See: https://stackoverflow.com/questions/6193711/call-has-been-made-on-garbage-collected-delegate-in-c
    private static readonly WINEVENTPROC hookProcDelegate = WindowEventCallback;

    public static void SubscribeToWindowEvents()
    {
        if (windowEventHook.IsInvalid)
        {
            windowEventHook = Windows.Win32.PInvoke.SetWinEventHook(
                EVENT_SYSTEM_FOREGROUND, // eventMin
                EVENT_SYSTEM_FOREGROUND, // eventMax
                null,             // hmodWinEventProc
                hookProcDelegate,        // lpfnWinEventProc
                0u,                       // idProcess
                0u,                       // idThread
                WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);

            if (windowEventHook.IsInvalid)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
    }

    public static void UnsubscribeWindowEvents()
    {
        if (!windowEventHook.IsInvalid)
        {
            windowEventHook.Close();
        }
    }

    public static void WindowEventCallback(HWINEVENTHOOK hWinEventHook, uint eventType,
        HWND hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        EnergyManager.HandleForegroundEvent(hwnd);
    }

    public delegate void WinEventProc(IntPtr hWinEventHook, uint eventType,
        HWND hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
}
