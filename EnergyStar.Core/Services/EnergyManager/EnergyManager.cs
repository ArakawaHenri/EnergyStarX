using EnergyStar.Interop;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
namespace EnergyStar;

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

    private static uint pendingProcPid = 0;
    private static string pendingProcName = "";

    private static readonly IntPtr pThrottleOn = IntPtr.Zero;
    private static readonly IntPtr pThrottleOff = IntPtr.Zero;
    private static readonly int szControlBlock = 0;

    private static IntPtr serviceThreadId = IntPtr.Zero;

    static EnergyManager()
    {
        szControlBlock = Marshal.SizeOf<Win32Interop.PROCESS_POWER_THROTTLING_STATE>();
        pThrottleOn = Marshal.AllocHGlobal(szControlBlock);
        pThrottleOff = Marshal.AllocHGlobal(szControlBlock);

        var throttleState = new Win32Interop.PROCESS_POWER_THROTTLING_STATE
        {
            Version = Win32Interop.PROCESS_POWER_THROTTLING_STATE.PROCESS_POWER_THROTTLING_CURRENT_VERSION,
            ControlMask = Win32Interop.ProcessorPowerThrottlingFlags.PROCESS_POWER_THROTTLING_EXECUTION_SPEED,
            StateMask = Win32Interop.ProcessorPowerThrottlingFlags.PROCESS_POWER_THROTTLING_EXECUTION_SPEED,
        };

        var unthrottleState = new Win32Interop.PROCESS_POWER_THROTTLING_STATE
        {
            Version = Win32Interop.PROCESS_POWER_THROTTLING_STATE.PROCESS_POWER_THROTTLING_CURRENT_VERSION,
            ControlMask = Win32Interop.ProcessorPowerThrottlingFlags.PROCESS_POWER_THROTTLING_EXECUTION_SPEED,
            StateMask = Win32Interop.ProcessorPowerThrottlingFlags.None,
        };

        Marshal.StructureToPtr(throttleState, pThrottleOn, false);
        Marshal.StructureToPtr(unthrottleState, pThrottleOff, false);
    }

    private static void ToggleEfficiencyMode(IntPtr hProcess, bool enable)
    {
        Win32Interop.SetProcessInformation(hProcess, Win32Interop.PROCESS_INFORMATION_CLASS.ProcessPowerThrottling,
            enable ? pThrottleOn : pThrottleOff, (uint)szControlBlock);
        Win32Interop.SetPriorityClass(hProcess, enable ? Win32Interop.PriorityClass.IDLE_PRIORITY_CLASS : Win32Interop.PriorityClass.NORMAL_PRIORITY_CLASS);
    }

    private static string GetProcessNameFromHandle(IntPtr hProcess)
    {
        int capacity = 1024;
        var sb = new StringBuilder(capacity);

        if (Win32Interop.QueryFullProcessImageName(hProcess, 0, sb, ref capacity))
        {
            return Path.GetFileName(sb.ToString());
        }

        return "";
    }

    public static void HandleForegroundEvent(IntPtr hwnd)
    {
        var windowThreadId = Win32Interop.GetWindowThreadProcessId(hwnd, out uint procId);
        // This is invalid, likely a process is dead, or idk
        if (windowThreadId == 0 || procId == 0) return;

        var procHandle = Win32Interop.OpenProcess(
            (uint)(Win32Interop.ProcessAccessFlags.QueryLimitedInformation | Win32Interop.ProcessAccessFlags.SetInformation), false, procId);
        if (procHandle == IntPtr.Zero) return;

        // Get the process
        var appName = GetProcessNameFromHandle(procHandle);

        // UWP needs to be handled in a special case
        if (appName == UWPFrameHostApp)
        {
            var found = false;
            Win32Interop.EnumChildWindows(hwnd, (innerHwnd, lparam) =>
            {
                if (found) return true;
                if (Win32Interop.GetWindowThreadProcessId(innerHwnd, out uint innerProcId) > 0)
                {
                    if (procId == innerProcId) return true;

                    var innerProcHandle = Win32Interop.OpenProcess((uint)(Win32Interop.ProcessAccessFlags.QueryLimitedInformation |
                        Win32Interop.ProcessAccessFlags.SetInformation), false, innerProcId);
                    if (innerProcHandle == IntPtr.Zero) return true;

                    // Found. Set flag, reinitialize handles and call it a day
                    found = true;
                    Win32Interop.CloseHandle(procHandle);
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
            Console.WriteLine($"Boost {appName}");
            ToggleEfficiencyMode(procHandle, false);
        }

        if (pendingProcPid != 0)
        {
            Console.WriteLine($"Throttle {pendingProcName}");

            var prevProcHandle = Win32Interop.OpenProcess((uint)Win32Interop.ProcessAccessFlags.SetInformation, false, pendingProcPid);
            if (prevProcHandle != IntPtr.Zero)
            {
                ToggleEfficiencyMode(prevProcHandle, true);
                Win32Interop.CloseHandle(prevProcHandle);
                pendingProcPid = 0;
                pendingProcName = "";
            }
        }

        if (!bypass)
        {
            pendingProcPid = procId;
            pendingProcName = appName;
        }

        Win32Interop.CloseHandle(procHandle);
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
            var hProcess = Win32Interop.OpenProcess((uint)Win32Interop.ProcessAccessFlags.SetInformation, false, (uint)proc.Id);
            ToggleEfficiencyMode(hProcess, true);
            Win32Interop.CloseHandle(hProcess);
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
            var hProcess = Win32Interop.OpenProcess((uint)Win32Interop.ProcessAccessFlags.SetInformation, false, (uint)proc.Id);
            ToggleEfficiencyMode(hProcess, false);
            Win32Interop.CloseHandle(hProcess);
        }
    }

    private static readonly CancellationTokenSource cts = new();

    private static async void AutoThrottleProc()
    {
        Console.WriteLine("Automatic throttling service started.");
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
                Console.WriteLine("Automatic throttling service stopped.");
                break;
            }
        }
    }
    
    public static void MainService()
    {
        serviceThreadId = Win32Interop.GetCurrentThreadId();
        Win32Interop.SubscribeToWindowEvents();
        ThrottleAllUserBackgroundProcesses();

        Thread autoThrottleThread = new (new ThreadStart(AutoThrottleProc));
        autoThrottleThread.Start();

        while (true)
        {
            if (Win32Interop.GetMessage(out Win32Interop.Win32WindowForegroundMessage msg, IntPtr.Zero, 0, 0))
            {
                if (msg.Message == Win32Interop.CUSTOM_QUIT)
                {
                    Console.WriteLine("Quitting...");
                    //cts.Cancel();
                    break;
                }
                Win32Interop.TranslateMessage(ref msg);
                Win32Interop.DispatchMessage(ref msg);
            }
        }
    }
    
    public static void StopService()
    {
        Win32Interop.PostThreadMessage(serviceThreadId, Win32Interop.CUSTOM_QUIT, IntPtr.Zero, IntPtr.Zero);
        Win32Interop.UnsubscribeWindowEvents();
        cts.Cancel();
        BoostAllInfluencedProcesses();
        serviceThreadId = IntPtr.Zero;
    }
}
