using EnergyStar.Activation;
using EnergyStar.Contracts.Services;
using EnergyStar.Core.Contracts.Services;
using EnergyStar.Core.Services;
using EnergyStar.Models;
using EnergyStar.Notifications;
using EnergyStar.Services;
using EnergyStar.ViewModels;
using EnergyStar.Views;
using H.NotifyIcon.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace EnergyStar;

// To learn more about WinUI 3, see https://docs.microsoft.com/windows/apps/winui/winui3/.
public partial class App : Application
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    public IHost Host
    {
        get;
    }

    public static T GetService<T>()
        where T : class
    {
        if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public static WindowEx MainWindow { get; } = new MainWindow();

    public App()
    {
        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
        ConfigureServices((context, services) =>
        {
            // Default Activation Handler
            services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

            // Other Activation Handlers
            services.AddTransient<IActivationHandler, AppNotificationActivationHandler>();

            // Services
            services.AddSingleton<IAppNotificationService, AppNotificationService>();
            services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
            services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddTransient<INavigationViewService, NavigationViewService>();

            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // Core Services
            services.AddSingleton<IFileService, FileService>();

            // Views and ViewModels
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<SettingsPage>();
            services.AddTransient<PowerPlanViewModel>();
            services.AddTransient<PowerPlanPage>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<MainPage>();
            services.AddTransient<ShellPage>();
            services.AddTransient<ShellViewModel>();

            // Configuration
            services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));
        }).
        Build();

        App.GetService<IAppNotificationService>().Initialize();

        UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // TODO: Log and handle exceptions as appropriate.
        // https://docs.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.unhandledexception.
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        //App.GetService<IAppNotificationService>().Show(string.Format("AppNotificationSamplePayload".GetLocalized(), AppContext.BaseDirectory));
        //await App.GetService<IActivationService>().ActivateAsync(args);

        MainWindow.SetWindowSize(1070, 575);
        if (AppInstance.GetCurrent().GetActivatedEventArgs().Kind != ExtendedActivationKind.StartupTask)
        {
            await App.GetService<IActivationService>().ActivateAsync(args);
            //MainWindow.Hide();
        }
        else
        {
            await App.GetService<IActivationService>().SilentActivateAsync(args);
        }
        Thread createIconThread = new(new ThreadStart(CreateIcon));
        createIconThread.Start();
    }

    public bool RunOnStart = false;
    public Thread? ESService;

    private static void CreateIcon()
    {
        using var icon = new System.Drawing.Icon(Path.Combine(AppContext.BaseDirectory, "Assets/WindowIcon.ico"));
        using var trayIcon = new TrayIconWithContextMenu
        {
            Icon = icon.Handle,
            ToolTip = "EnergyStar",
        };
        trayIcon.ContextMenu = new PopupMenu
        {
            Items =
            {
                new PopupMenuItem("Exit", (sender, args) =>
                {
                    trayIcon.Dispose();
                    Environment.Exit(0);
                }),
            },
        };
        trayIcon.Create();
        trayIcon.MainWindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow);
        object Locker = new();
        lock (Locker)
        {
            Monitor.Wait(Locker);
        }
        trayIcon.Dispose();
    }
}
