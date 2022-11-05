using EnergyStarX.Contracts.Services;
using EnergyStarX.Helpers;
using Microsoft.Windows.System.Power;

namespace EnergyStarX.Services;

public class EnergyManagerService
{
    private static bool IsThrottling = false;

    private static bool IsEnabled = false;

    public record ServiceStatus(bool IsEnabled, bool IsThrottling, PowerSourceKind PowerSourceKind);

    public static ServiceStatus Status => new(IsEnabled, IsThrottling, PowerManager.PowerSourceKind);

    public static event EventHandler<ServiceStatus>? StatusChanged;

    private bool throttleWhenPluggedIn = false;

    public bool ThrottleWhenPluggedIn
    {
        get => throttleWhenPluggedIn;
        set
        {
            lock (locker)
            {
                throttleWhenPluggedIn = value;
                if (IsEnabled)
                {
                    PowerManager_PowerSourceKindChanged(this, new object());
                }
                _localSettingsService.SaveSettingAsync<bool>("ThrottleWhenPluggedIn", value);
            }
        }
    }

    private static bool runOnStart = false;

    public bool RunOnStart
    {
        get => runOnStart;
        set
        {
            lock (locker)
            {
                runOnStart = value;
                _localSettingsService.SaveSettingAsync<bool>("RunOnStart", value);
            }
        }
    }

    private readonly ILocalSettingsService _localSettingsService;

    private static CancellationTokenSource cts = new();

    private static readonly object locker = new();

    private static async void AutoThrottleProc()
    {
        while (!cts.IsCancellationRequested)
        {
            try
            {
                PeriodicTimer ThrottleTimer = new(TimeSpan.FromMinutes(5));
                await ThrottleTimer.WaitForNextTickAsync(cts.Token);
                EnergyManager.ThrottleAllUserBackgroundProcesses();
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private static void StartThrottling(object? sender = null)
    {
        try
        {
            lock (locker)
            {
                if (!IsThrottling)
                {
                    cts = new CancellationTokenSource();
                    HookManager.SubscribeToWindowEvents();
                    EnergyManager.ThrottleAllUserBackgroundProcesses();
                    Thread AutoThrottleThread = new(new ThreadStart(AutoThrottleProc));
                    AutoThrottleThread.Start();
                    IsThrottling = true;
                }
            }
        }
        finally
        {
            StatusChanged?.Invoke(sender, Status);
        }
    }

    private static void StopThrottling(object? sender = null)
    {
        try
        {
            lock (locker)
            {
                if (IsThrottling)
                {
                    cts.Cancel();
                    HookManager.UnsubscribeWindowEvents();
                    EnergyManager.BoostAllInfluencedProcesses();
                    IsThrottling = false;
                }
            }
        }
        finally
        {
            StatusChanged?.Invoke(sender, Status);
        }
    }

    private void PowerManager_PowerSourceKindChanged(object? sender, object e)
    {
        lock (locker)
        {
            if (PowerManager.PowerSourceKind == PowerSourceKind.DC || throttleWhenPluggedIn)
            {
                StartThrottling(this);
            }
            else
            {
                StopThrottling(this);
            }
        }
    }

    public void Enable()
    {
        lock (locker)
        {
            IsEnabled = true;
            PowerManager_PowerSourceKindChanged(this, new object());
            PowerManager.PowerSourceKindChanged += PowerManager_PowerSourceKindChanged;
        }
    }

    public async Task Initialize()
    {
        RunOnStart = await _localSettingsService.ReadSettingAsync<bool>("RunOnStart");
        IsEnabled = RunOnStart;
        throttleWhenPluggedIn = await _localSettingsService.ReadSettingAsync<bool>("ThrottleWhenPluggedIn");
        if (IsEnabled)
        {
            lock (locker)
            {
                //HookManager.SubscribeToWindowEvents();
                //ApplyBypassProcessList(LocalSettings.BypassProcessListString);
                PowerManager_PowerSourceKindChanged(this, new object());
                PowerManager.PowerSourceKindChanged += PowerManager_PowerSourceKindChanged;
            }
        }
    }

    public void Disable()
    {
        lock (locker)
        {
            IsEnabled = false;
            PowerManager.PowerSourceKindChanged -= PowerManager_PowerSourceKindChanged;
            StopThrottling();
        };
    }

    public EnergyManagerService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
    }
}
