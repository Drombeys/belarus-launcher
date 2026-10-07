using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using Belarus.Launcher.Core.FileHashVerification;
using Belarus.Launcher.Core.Logger;
using Belarus.Launcher.Core.Manager;
using Belarus.Launcher.Core.Services;
using Belarus.Launcher.Core.Storage;
using Belarus.Launcher.Injection;
using Belarus.Launcher.Models;
using Belarus.Launcher.Services;
using Belarus.Launcher.ViewModels;
using Belarus.Launcher.Views;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Serilog;

namespace Belarus.Launcher;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider = ConfigureServices()
        .BuildServiceProvider();

    private static ServiceCollection ConfigureServices()
    {
        var services = new ServiceCollection();
        var pathLog = Path.Combine(DirectoryStorage.LauncherLogs, FileNameStorage.LauncherLog);
        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(LogManager.CreateLogger(pathLog)));
        services.AddTransient<IConfiguration>(x =>
        {
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<App>()
                .Build();
            return configuration;
        });

        services.AddPresentationServices();
        services.AddValidators();
        services.AddManagers();
        services.AddServices();

        services.AddTransient<IHashProvider, Md5HashProvider>();
        services.AddTransient<IWebsiteLauncher, WebsiteLauncher>();
        services.AddTransient<HashChecker>();
        services.AddSingleton<ILauncherStorage, MemoryLauncherStorage>();
        services.AddSingleton<ViewModelLocator>();

        return services;
    }

    public override void Initialize()
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<App>>();

        try
        {
            AvaloniaXamlLoader.Load(this);
        }
        catch (Exception exception)
        {
            logger.LogCritical("{Message}", exception.Message);
            logger.LogInformation("{StackTrace}", exception.StackTrace);
            throw;
        }
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                await InitializeApplicationAsync(desktop);
            }
            catch (OperationCanceledException)
            {
                desktop.Shutdown();
                return;
            }
            catch (Exception exception)
            {
                HandleStartupError(desktop, exception);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task InitializeApplicationAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var initializerManager = _serviceProvider.GetRequiredService<InitializerManager>();
        var userManager = _serviceProvider.GetRequiredService<UserManager>();
        UserManager.MigratorSettings();

        var splashScreenManager = _serviceProvider.GetRequiredService<ISplashScreenManager>();
        splashScreenManager.MaxProgress = 4;

        await userManager.LoadAsync(splashScreenManager.CancellationToken);
        initializerManager.InitializeLocale();

        var mainViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
        desktop.MainWindow = new MainWindow
        {
            DataContext = mainViewModel
        };
        desktop.MainWindow.Show();

        mainViewModel.ShowSplashScreenImpl();

        await initializerManager.InitializeAsync(splashScreenManager);
        await mainViewModel.InitializeAsync(splashScreenManager);
    }

    private void HandleStartupError(IClassicDesktopStyleApplicationLifetime desktop, Exception exception)
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<App>>();
        logger.LogCritical(exception, "Failed to initialize the launcher");

        if (!TryShowStartupError(desktop, logger))
        {
            desktop.Shutdown();
        }
    }

    private bool TryShowStartupError(IClassicDesktopStyleApplicationLifetime desktop, ILogger<App> logger)
    {
        try
        {
            var localeManager = _serviceProvider.GetRequiredService<IApplicationLocaleManager>();
            var title = localeManager.GetStringByKey("LocalizedStrings.ErrorTitle");
            var description = localeManager.GetStringByKey("LocalizedStrings.StartupError");

            if (desktop.MainWindow is null)
            {
                var errorWindow = new Window
                {
                    Title = title,
                    Width = 460,
                    Height = 180,
                    CanResize = false,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Content = new TextBlock
                    {
                        Margin = new Thickness(24),
                        Text = description,
                        TextWrapping = TextWrapping.Wrap,
                    },
                };

                desktop.MainWindow = errorWindow;
                errorWindow.Show();
                return true;
            }

            var splashScreenManager = _serviceProvider.GetRequiredService<ISplashScreenManager>();
            _serviceProvider.GetRequiredService<MainWindowViewModel>().ShowSplashScreenImpl();
            splashScreenManager.UpdateInformation(new InformationMessage(title, description));

            return true;
        }
        catch (Exception displayException)
        {
            logger.LogError(displayException, "Failed to display the startup error");
            return false;
        }
    }
}
