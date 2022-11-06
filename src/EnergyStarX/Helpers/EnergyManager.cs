using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;

namespace EnergyStarX.Helpers;

internal class EnergyManager
{
    public static readonly HashSet<string> BypassProcessList = new()
    {
        // Not ourselves
        "ESX.exe".ToLowerInvariant(),
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

    private static readonly int szControlBlock;
    private static readonly IntPtr pThrottleOn;
    private static readonly IntPtr pThrottleOff;

    static EnergyManager()
    {
        szControlBlock = Marshal.SizeOf<PROCESS_POWER_THROTTLING_STATE>();
        pThrottleOn = Marshal.AllocHGlobal(szControlBlock);
        pThrottleOff = Marshal.AllocHGlobal(szControlBlock);

        var throttleState = new PROCESS_POWER_THROTTLING_STATE
        {
            Version = 1u,
            ControlMask = 0x01u,
            StateMask = 0x01u,
        };

        var unthrottleState = new PROCESS_POWER_THROTTLING_STATE
        {
            Version = 1u,
            ControlMask = 0x01u,
            StateMask = 0x00u,
        };

        Marshal.StructureToPtr(throttleState, pThrottleOn, false);
        Marshal.StructureToPtr(unthrottleState, pThrottleOff, false);
    }

    private static unsafe void ToggleEfficiencyMode(SafeHandle hProcess, bool enable)
    {
        PInvoke.SetProcessInformation(hProcess, PROCESS_INFORMATION_CLASS.ProcessPowerThrottling,
            (void*)(enable ? pThrottleOn : pThrottleOff), (uint)szControlBlock);
        PInvoke.SetPriorityClass(hProcess, enable ? PROCESS_CREATION_FLAGS.IDLE_PRIORITY_CLASS : PROCESS_CREATION_FLAGS.NORMAL_PRIORITY_CLASS);
    }

    private static unsafe string GetProcessNameFromHandle(SafeHandle hProcess)
    {
        var capacity = 1024u;
        var sb = (char*)NativeMemory.Alloc(capacity);

        if (PInvoke.QueryFullProcessImageName(hProcess, 0, sb, ref capacity))
        {
            var str = Marshal.PtrToStringAuto((IntPtr)sb);
            NativeMemory.Free(sb);
            if (str != null)
            {
                return Path.GetFileName(str);
            }
        }
        return "";
    }

    public static unsafe void HandleForegroundEvent(HWND hwnd)
    {
        var procId = (uint*)NativeMemory.Alloc(sizeof(uint));
        var windowThreadId = PInvoke.GetWindowThreadProcessId(hwnd, procId);
        // This is invalid, likely a process is dead, or idk
        if (windowThreadId == 0u || *procId == 0u)
        {
            NativeMemory.Free(procId);
            return;
        }

        var procHandle = new SafeProcessHandle(PInvoke.OpenProcess(
            (PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION | PROCESS_ACCESS_RIGHTS.PROCESS_SET_INFORMATION), false, *procId), true);
        if (procHandle.IsInvalid)
        {
            NativeMemory.Free(procId);
            return;
        }

        // Get the process
        var appName = GetProcessNameFromHandle(procHandle);

        // UWP needs to be handled in a special case
        if (appName == UWPFrameHostApp)
        {
            var found = false;
            PInvoke.EnumChildWindows(hwnd, (innerHwnd, lparam) =>
            {
                if (found)
                {
                    return true;
                }
                var innerProcId = (uint*)NativeMemory.Alloc(sizeof(uint));
                if (PInvoke.GetWindowThreadProcessId(innerHwnd, innerProcId) > 0u)
                {
                    if (*procId == *innerProcId)
                    {
                        NativeMemory.Free(innerProcId);
                        return true;
                    }

                    var innerProcHandle = PInvoke.OpenProcess((PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION | PROCESS_ACCESS_RIGHTS.PROCESS_SET_INFORMATION), false, *innerProcId);
                    if (innerProcHandle.Value == IntPtr.Zero)
                    {
                        NativeMemory.Free(innerProcId);
                        return true;
                    }

                    // Found. Set flag, reinitialize handles and call it a day
                    found = true;
                    procHandle.Close();
                    procHandle = new SafeProcessHandle(innerProcHandle, true);
                    NativeMemory.Free(procId);
                    procId = innerProcId;
                    appName = GetProcessNameFromHandle(procHandle);
                    return true;
                }
                NativeMemory.Free(innerProcId);
                return true;
            }, IntPtr.Zero);
        }

        // Boost the current foreground app, and then impose EcoQoS for previous foreground app
        var bypass = BypassProcessList.Contains(appName.ToLowerInvariant());
        if (!bypass)
        {
            Console.WriteLine($"Boost {appName} ({*procId})");
            ToggleEfficiencyMode(procHandle, false);
        }

        if (pendingProcPid != 0)
        {
            Console.WriteLine($"Throttle {pendingProcName} ({pendingProcPid})");

            var prevProcHandle = new SafeProcessHandle(PInvoke.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_SET_INFORMATION, false, pendingProcPid), true);
            if (!prevProcHandle.IsInvalid)
            {
                ToggleEfficiencyMode(prevProcHandle, true);
                prevProcHandle.Close();
                pendingProcPid = 0;
                pendingProcName = "";
            }
        }

        if (!bypass)
        {
            pendingProcPid = *procId;
            pendingProcName = appName;
        }

        procHandle.Close();
        NativeMemory.Free(procId);
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
            var hProcess = new SafeProcessHandle(PInvoke.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_SET_INFORMATION, false, (uint)proc.Id), true);
            ToggleEfficiencyMode(hProcess, true);
            hProcess.Close();
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
            var hProcess = new SafeProcessHandle(PInvoke.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_SET_INFORMATION, false, (uint)proc.Id), true);
            ToggleEfficiencyMode(hProcess, false);
            hProcess.Close();
        }
    }
}