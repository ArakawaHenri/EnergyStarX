using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
namespace EnergyManager.Interop;

internal class Win32API
{
    //private const int WINEVENT_INCONTEXT = 4;
    private const int WINEVENT_OUTOFCONTEXT = 0;
    private const int WINEVENT_SKIPOWNPROCESS = 2;
    //private const int WINEVENT_SKIPOWNTHREAD = 1;
    private const int EVENT_SYSTEM_FOREGROUND = 3;
    public const uint PM_NOREMOVE = 0;
    public const uint PM_REMOVE = 1;
    //public const uint WM_USER = 0x0400;
    public const uint CUSTOM_QUIT = 0x0D00 + 0x0721;

    private static IntPtr windowEventHook;
    // Explicitly declare it to prevent GC
    // See: https://stackoverflow.com/questions/6193711/call-has-been-made-on-garbage-collected-delegate-in-c
    private static readonly WinEventProc hookProcDelegate = WindowEventCallback;

    public delegate bool WindowEnumProc(IntPtr hwnd, IntPtr lparam);

    public delegate void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    public static void SubscribeToWindowEvents()
    {
        if (windowEventHook == IntPtr.Zero)
        {
            windowEventHook = SetWinEventHook(
                EVENT_SYSTEM_FOREGROUND, // eventMin
                EVENT_SYSTEM_FOREGROUND, // eventMax
                IntPtr.Zero,             // hmodWinEventProc
                hookProcDelegate,        // lpfnWinEventProc
                0,                       // idProcess
                0,                       // idThread
                WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);

            if (windowEventHook == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
    }

    public static void UnsubscribeWindowEvents()
    {
        if (windowEventHook != IntPtr.Zero)
        {
            _ = UnhookWinEvent(windowEventHook);
            windowEventHook = IntPtr.Zero;
        }
    }

    public static void WindowEventCallback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        EnergyManager.HandleForegroundEvent(hwnd);
    }

    public static Win32API.SYSTEM_POWER_STATUS GetSystemPowerStatus()
    {
        IntPtr powerStatusPtr = Marshal.AllocHGlobal(Marshal.SizeOf<Win32API.SYSTEM_POWER_STATUS>());

        try
        {
            if (Win32API.GetSystemPowerStatus(powerStatusPtr))
            {
                return Marshal.PtrToStructure<Win32API.SYSTEM_POWER_STATUS>(powerStatusPtr);
            }

            return new Win32API.SYSTEM_POWER_STATUS();
        }
        finally
        {
            Marshal.FreeHGlobal(powerStatusPtr);
        }
    }

    [Flags]
    public enum ProcessAccessFlags : uint
    {
        All = 0x001F0FFF,
        Terminate = 0x00000001,
        CreateThread = 0x00000002,
        VirtualMemoryOperation = 0x00000008,
        VirtualMemoryRead = 0x00000010,
        VirtualMemoryWrite = 0x00000020,
        DuplicateHandle = 0x00000040,
        CreateProcess = 0x000000080,
        SetQuota = 0x00000100,
        SetInformation = 0x00000200,
        QueryInformation = 0x00000400,
        QueryLimitedInformation = 0x00001000,
        Synchronize = 0x00100000
    }

    public enum PROCESS_INFORMATION_CLASS
    {
        ProcessMemoryPriority,
        ProcessMemoryExhaustionInfo,
        ProcessAppMemoryInfo,
        ProcessInPrivateInfo,
        ProcessPowerThrottling,
        ProcessReservedValue1,
        ProcessTelemetryCoverageInfo,
        ProcessProtectionLevelInfo,
        ProcessLeapSecondInfo,
        ProcessInformationClassMax,
    }

    [Flags]
    public enum ProcessorPowerThrottlingFlags : uint
    {
        None = 0x0,
        PROCESS_POWER_THROTTLING_EXECUTION_SPEED = 0x1,
    }

    public enum PriorityClass : uint
    {
        ABOVE_NORMAL_PRIORITY_CLASS = 0x8000,
        BELOW_NORMAL_PRIORITY_CLASS = 0x4000,
        HIGH_PRIORITY_CLASS = 0x80,
        IDLE_PRIORITY_CLASS = 0x40,
        NORMAL_PRIORITY_CLASS = 0x20,
        PROCESS_MODE_BACKGROUND_BEGIN = 0x100000,// 'Windows Vista/2008 and higher
        PROCESS_MODE_BACKGROUND_END = 0x200000,//   'Windows Vista/2008 and higher
        REALTIME_PRIORITY_CLASS = 0x100
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_POWER_THROTTLING_STATE
    {
        public const uint PROCESS_POWER_THROTTLING_CURRENT_VERSION = 1;
        public uint Version;
        public ProcessorPowerThrottlingFlags ControlMask;
        public ProcessorPowerThrottlingFlags StateMask;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Win32WindowForegroundMessage
    {
        public IntPtr Hwnd;
        public uint Message;
        public IntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public System.Drawing.Point Point;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEM_POWER_STATUS
    {
        public const Byte AC_LINE_STATUS_OFFLINE = 0;           // AC adapter disconnected
        public const Byte AC_LINE_STATUS_ONLINE = 1;            // AC adapter connected
        public const Byte AC_LINE_STATUS_UNKNOWN = 255;

        public const Byte BATTERY_FLAG_HIGH = 1;                // the battery capacity is at more than 66 percent
        public const Byte BATTERY_FLAG_LOW = 2;                 // the battery capacity is at less than 33 percent
        public const Byte BATTERY_FLAG_CRITICAL = 4;            // the battery capacity is at less than five percent
        public const Byte BATTERY_FLAG_CHARGING = 8;            // Charging
        public const Byte BATTERY_FLAG_NO_SYSTEM_BATTERY = 128; // No system battery
        public const Byte BATTERY_FLAG_UNKNOWN = 255;           // Unable to read the battery flag information

        public const Byte BATTERY_LIFE_PERCENT_UNKNOWN = 255;

        public const Byte SYSTEM_STATUS_FLAG_BATTERY_SAVER_OFF = 0; // Battery saver is off.
        public const Byte SYSTEM_STATUS_FLAG_BATTERY_SAVER_ON = 1;  // Battery saver on. Save energy where possible.

        public Byte ACLineStatus;           // The AC power status.
        public Byte BatteryFlag;            // The battery charge status.
        public Byte BatteryLifePercent;     // The percentage of full battery charge remaining. This member can be a value in the range 0 to 100, or 255 if status is unknown.
        public Byte SystemStatusFlag;       // The status of battery saver.
        public UInt32 BatteryLifeTime;      // The number of seconds of battery life remaining, or –1 if remaining seconds are unknown or if the device is connected to AC power.
        public UInt32 BatteryFullLifeTime;  // The number of seconds of battery life when at full charge, or –1 if full battery lifetime is unknown or if the device is connected to AC power.

        public SYSTEM_POWER_STATUS()
        {
            ACLineStatus = AC_LINE_STATUS_UNKNOWN;
            BatteryFlag = BATTERY_FLAG_UNKNOWN;
            BatteryLifePercent = BATTERY_LIFE_PERCENT_UNKNOWN;
            SystemStatusFlag = 0;
            BatteryLifeTime = 0;
            BatteryFullLifeTime = 0;
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GetCurrentThreadId();

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, uint processId);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool QueryFullProcessImageName([In] IntPtr hProcess, [In] int dwFlags, [Out] StringBuilder lpExeName, ref int lpdwSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool SetProcessInformation([In] IntPtr hProcess, [In] PROCESS_INFORMATION_CLASS ProcessInformationClass, IntPtr ProcessInformation, uint ProcessInformationSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool SetPriorityClass(IntPtr handle, PriorityClass priorityClass);

    [DllImport("kernel32.dll", SetLastError = true)]
    [SuppressUnmanagedCodeSecurity]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetSystemPowerStatus(IntPtr lpSystemPowerStatus);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumChildWindows(IntPtr hwnd, WindowEnumProc callback, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWinEventHook(int eventMin, int eventMax, IntPtr hmodWinEventProc, WinEventProc lpfnWinEventProc, int idProcess, int idThread, int dwflags);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int UnhookWinEvent(IntPtr hWinEventHook);

    [DllImport("user32.dll")]
    public static extern bool PeekMessage(out Win32WindowForegroundMessage lpMsg, IntPtr hwnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

    [DllImport("user32.dll")]
    public static extern bool GetMessage(out Win32WindowForegroundMessage lpMsg, IntPtr hwnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    public static extern bool PostThreadMessage(IntPtr idThread, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool TranslateMessage(ref Win32WindowForegroundMessage lpMsg);

    [DllImport("user32.dll")]
    public static extern IntPtr DispatchMessage(ref Win32WindowForegroundMessage lpMsg);
}
