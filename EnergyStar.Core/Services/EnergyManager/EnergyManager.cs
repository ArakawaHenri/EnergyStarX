using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using EnergyManager.Interop;
using Microsoft.Win32;

namespace EnergyManager;

public class EnergyManager
{
    public static readonly HashSet<string> BypassProcessList = new()
    {
        // Not ourselves
        "EnergyStar.exe".ToLowerInvariant(),
        // Edge has energy awareness
        "msedge.exe",
        "WebViewHost.exe".ToLowerInvariant(),
        // UWP Frame has special handling, should not be throttled
        "ApplicationFrameHost.exe".ToLowerInvariant(),
        // Fire extinguisher should not catch fire
        "taskmgr.exe",
        "procmon.exe",
        "procmon64.exe",
        // Widgets
        "Widgets.exe".ToLowerInvariant(),
        // System shell
        "dwm.exe",
        "explorer.exe",
        "ShellExperienceHost.exe".ToLowerInvariant(),
        "StartMenuExperienceHost.exe".ToLowerInvariant(),
        "SearchHost.exe".ToLowerInvariant(),
        "sihost.exe",
        "fontdrvhost.exe",
        // IME
        "ChsIME.exe".ToLowerInvariant(),
        "ctfmon.exe",
#if DEBUG
        // Visual Studio
        "devenv.exe",
#endif
        // System Service - they have their awareness
        "csrss.exe",
        "smss.exe",
        "svchost.exe",
        // WUDF
        "WUDFRd.exe".ToLowerInvariant(),
    };
    // Speical handling needs for UWP to get the child window process
    public const string UWPFrameHostApp = "ApplicationFrameHost.exe";

    public static bool IsAcConnected { get; set; } = false;
    public static bool AlwaysThrottle { get; set; } = false;

    private static uint pendingProcPid = 0;
    private static string pendingProcName = "";

    private static readonly IntPtr pThrottleOn = IntPtr.Zero;
    private static readonly IntPtr pThrottleOff = IntPtr.Zero;
    private static readonly int szControlBlock = 0;

    private static IntPtr serviceThreadId = IntPtr.Zero;

    static EnergyManager()
    {
        szControlBlock = Marshal.SizeOf<Win32API.PROCESS_POWER_THROTTLING_STATE>();
        pThrottleOn = Marshal.AllocHGlobal(szControlBlock);
        pThrottleOff = Marshal.AllocHGlobal(szControlBlock);

        var throttleState = new Win32API.PROCESS_POWER_THROTTLING_STATE
        {
            Version = Win32API.PROCESS_POWER_THROTTLING_STATE.PROCESS_POWER_THROTTLING_CURRENT_VERSION,
            ControlMask = Win32API.ProcessorPowerThrottlingFlags.PROCESS_POWER_THROTTLING_EXECUTION_SPEED,
            StateMask = Win32API.ProcessorPowerThrottlingFlags.PROCESS_POWER_THROTTLING_EXECUTION_SPEED,
        };

        var unthrottleState = new Win32API.PROCESS_POWER_THROTTLING_STATE
        {
            Version = Win32API.PROCESS_POWER_THROTTLING_STATE.PROCESS_POWER_THROTTLING_CURRENT_VERSION,
            ControlMask = Win32API.ProcessorPowerThrottlingFlags.PROCESS_POWER_THROTTLING_EXECUTION_SPEED,
            StateMask = Win32API.ProcessorPowerThrottlingFlags.None,
        };

        Marshal.StructureToPtr(throttleState, pThrottleOn, false);
        Marshal.StructureToPtr(unthrottleState, pThrottleOff, false);
    }

    private static void ToggleEfficiencyMode(IntPtr hProcess, bool enable)
    {
        Win32API.SetProcessInformation(hProcess, Win32API.PROCESS_INFORMATION_CLASS.ProcessPowerThrottling,
            enable ? pThrottleOn : pThrottleOff, (uint)szControlBlock);
        Win32API.SetPriorityClass(hProcess, enable ? Win32API.PriorityClass.IDLE_PRIORITY_CLASS : Win32API.PriorityClass.NORMAL_PRIORITY_CLASS);
    }

    private static string GetProcessNameFromHandle(IntPtr hProcess)
    {
        int capacity = 1024;
        var sb = new StringBuilder(capacity);

        if (Win32API.QueryFullProcessImageName(hProcess, 0, sb, ref capacity))
        {
            return Path.GetFileName(sb.ToString());
        }

        return "";
    }

    public static void HandleForegroundEvent(IntPtr hwnd)
    {
        if (IsAcConnected && !AlwaysThrottle) return;
        var windowThreadId = Win32API.GetWindowThreadProcessId(hwnd, out uint procId);
        // This is invalid, likely a process is dead, or idk
        if (windowThreadId == 0 || procId == 0) return;

        var procHandle = Win32API.OpenProcess(
            (uint)(Win32API.ProcessAccessFlags.QueryLimitedInformation | Win32API.ProcessAccessFlags.SetInformation), false, procId);
        if (procHandle == IntPtr.Zero) return;

        // Get the process
        var appName = GetProcessNameFromHandle(procHandle);

        // UWP needs to be handled in a special case
        if (appName == UWPFrameHostApp)
        {
            var found = false;
            Win32API.EnumChildWindows(hwnd, (innerHwnd, lparam) =>
            {
                if (found) return true;
                if (Win32API.GetWindowThreadProcessId(innerHwnd, out uint innerProcId) > 0)
                {
                    if (procId == innerProcId) return true;

                    var innerProcHandle = Win32API.OpenProcess((uint)(Win32API.ProcessAccessFlags.QueryLimitedInformation |
                        Win32API.ProcessAccessFlags.SetInformation), false, innerProcId);
                    if (innerProcHandle == IntPtr.Zero) return true;

                    // Found. Set flag, reinitialize handles and call it a day
                    found = true;
                    Win32API.CloseHandle(procHandle);
                    procHandle = innerProcHandle;
                    procId = innerProcId;
                    appName = GetProcessNameFromHandle(procHandle);
                }

                return true;
            }, IntPtr.Zero);
        }

        // Boost the current foreground app, and then impose EcoQoS for previous foreground app
        var bypass = BypassProcessList.Contains(appName.ToLowerInvariant());
        if (!bypass)
        {
            Debug.WriteLine($"Boost {appName} ({procId})");
            ToggleEfficiencyMode(procHandle, false);
        }

        if (pendingProcPid != 0)
        {
            Debug.WriteLine($"Throttle {pendingProcName} ({pendingProcPid})");

            var prevProcHandle = Win32API.OpenProcess((uint)Win32API.ProcessAccessFlags.SetInformation, false, pendingProcPid);
            if (prevProcHandle != IntPtr.Zero)
            {
                ToggleEfficiencyMode(prevProcHandle, true);
                Win32API.CloseHandle(prevProcHandle);
                pendingProcPid = 0;
                pendingProcName = "";
            }
        }

        if (!bypass)
        {
            pendingProcPid = procId;
            pendingProcName = appName;
        }

        Win32API.CloseHandle(procHandle);
    }

    public static void ThrottleAllUserBackgroundProcesses()
    {
        var runningProcesses = Process.GetProcesses();
        var currentSessionID = Process.GetCurrentProcess().SessionId;

        var sameAsThisSession = runningProcesses.Where(p => p.SessionId == currentSessionID);
        foreach (var proc in sameAsThisSession)
        {
            if (proc.Id == pendingProcPid) continue;
            if (BypassProcessList.Contains($"{proc.ProcessName}.exe".ToLowerInvariant())) continue;
            var hProcess = Win32API.OpenProcess((uint)Win32API.ProcessAccessFlags.SetInformation, false, (uint)proc.Id);
            ToggleEfficiencyMode(hProcess, !IsAcConnected || AlwaysThrottle);
            Win32API.CloseHandle(hProcess);
        }
    }

    public static void BoostAllInfluencedProcesses()
    {
        var runningProcesses = Process.GetProcesses();
        var currentSessionID = Process.GetCurrentProcess().SessionId;

        var sameAsThisSession = runningProcesses.Where(p => p.SessionId == currentSessionID);
        foreach (var proc in sameAsThisSession)
        {
            if (proc.Id == pendingProcPid) continue;
            if (BypassProcessList.Contains($"{proc.ProcessName}.exe".ToLowerInvariant())) continue;
            var hProcess = Win32API.OpenProcess((uint)Win32API.ProcessAccessFlags.SetInformation, false, (uint)proc.Id);
            ToggleEfficiencyMode(hProcess, false);
            Win32API.CloseHandle(hProcess);
        }
    }

    private static readonly CancellationTokenSource cts = new();

    private static async void AutoThrottleProc()
    {
        Debug.WriteLine("Automatic throttling service started.");
        while (!cts.IsCancellationRequested)
        {
            try
            {
                var ThrottleTimer = new PeriodicTimer(TimeSpan.FromMinutes(5));
                await ThrottleTimer.WaitForNextTickAsync(cts.Token);
                EnergyManager.ThrottleAllUserBackgroundProcesses();
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Automatic throttling service stopped.");
                break;
            }
        }
    }

    private static void SystemEventsPowerModeChanged()
    {
        Win32API.SYSTEM_POWER_STATUS powerStatus = Win32API.GetSystemPowerStatus();

        switch (powerStatus.ACLineStatus)
        {
            case Win32API.SYSTEM_POWER_STATUS.AC_LINE_STATUS_OFFLINE:
                Debug.WriteLine("AC disconnected");
                EnergyManager.IsAcConnected = false;
                break;
            case Win32API.SYSTEM_POWER_STATUS.AC_LINE_STATUS_ONLINE:
                Debug.WriteLine("AC connected");
                EnergyManager.IsAcConnected = true;
                break;
            default:
                break;
        }

        EnergyManager.ThrottleAllUserBackgroundProcesses();
    }

    public static void MainService()
    {
        SystemEventsPowerModeChanged();
        serviceThreadId = Win32API.GetCurrentThreadId();
        Win32API.SubscribeToWindowEvents();
        ThrottleAllUserBackgroundProcesses();

        Thread autoThrottleThread = new(new ThreadStart(AutoThrottleProc));
        autoThrottleThread.Start();

#pragma warning disable CA1416 // Validate platform compatibility
        SystemEvents.PowerModeChanged += new PowerModeChangedEventHandler((object sender, PowerModeChangedEventArgs e) => SystemEventsPowerModeChanged());
#pragma warning restore CA1416 // Validate platform compatibility

        while (true)
        {
            if (Win32API.GetMessage(out Win32API.Win32WindowForegroundMessage msg, IntPtr.Zero, 0, 0))
            {
                if (msg.Message == Win32API.CUSTOM_QUIT)
                {
                    Debug.WriteLine("MainService Quitting...");
                    //cts.Cancel();
                    break;
                }
                Win32API.TranslateMessage(ref msg);
                Win32API.DispatchMessage(ref msg);
            }
        }
    }

    public static void StopService()
    {
        Win32API.PostThreadMessage(serviceThreadId, Win32API.CUSTOM_QUIT, IntPtr.Zero, IntPtr.Zero);
        Win32API.UnsubscribeWindowEvents();
        cts.Cancel();
        BoostAllInfluencedProcesses();
        serviceThreadId = IntPtr.Zero;
        Debug.WriteLine("GUI called to stop");
    }
}
