using EnergyStarX.Activation;
using EnergyStarX.Contracts.Services;
using EnergyStarX.Core.Contracts.Services;
using EnergyStarX.Core.Services;
using EnergyStarX.Models;
using EnergyStarX.Services;
using EnergyStarX.ViewModels;
using EnergyStarX.Views;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;

namespace EnergyStarX;

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
        if (!Microsoft.Windows.AppLifecycle.AppInstance.FindOrRegisterForKey("EnergyStarXInstanceKey1145141919810").IsCurrent)
        {
            //mainInstance.RedirectActivationToAsync(Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs()).GetAwaiter();
            var xmlPayload = new string($@"
        <toast>    
            <visual>    
                <binding template=""ToastGeneric"">    
                    <text>EnergyStar X is already running!</text>
                    <text>There is another EnergyStar X Instance, exiting...</text>    
                </binding>
            </visual>  
        </toast>");
            var toast = new Microsoft.Windows.AppNotifications.AppNotification(xmlPayload);
            Microsoft.Windows.AppNotifications.AppNotificationManager.Default.Show(toast);
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
        ConfigureServices((context, services) =>
        {
            // Default Activation Handler
            services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

            // Other Activation Handlers

            // Services
            services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
            services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
            services.AddSingleton<EnergyManagerService, EnergyManagerService>();
            services.AddTransient<INavigationViewService, NavigationViewService>();

            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // Core Services
            services.AddSingleton<IFileService, FileService>();

            // Views and ViewModels
            services.AddSingleton<SettingsViewModel>();
            services.AddTransient<SettingsPage>();
            services.AddSingleton<RulesViewModel>();
            services.AddTransient<RulesPage>();
            services.AddSingleton<MainViewModel>();
            services.AddTransient<MainPage>();
            services.AddTransient<ShellPage>();
            services.AddTransient<ShellViewModel>();

            // Configuration
            services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));
        }).
        Build();

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

        MainWindow.SetWindowSize(1070, 575);

        await App.GetService<IActivationService>().ActivateAsync(args);
    }
}
