using Belarus.Launcher.Core.Helpers;
using Belarus.Launcher.Core.Manager;
using Belarus.Launcher.Core.Storage;

using Microsoft.Extensions.Logging;

using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace Belarus.Launcher.ViewModels;

public partial class GameMenuViewModel : ReactiveObject
{
    private readonly ILogger<GameMenuViewModel>? _logger;
    private readonly IWindowManager _windowManager;
    private readonly ILauncherStorage _launcherStorage;
    private readonly UserManager _userManager;

    public IApplicationLocaleManager Localization { get; private set; }
    public ReactiveCommand<MainWindowViewModel, RxVoid> PlayGame { get; private set; }
    public ReactiveCommand<RxVoid, RxVoid> StartServer { get; private set; }
    public ReactiveCommand<LauncherViewModel, RxVoid> CheckUpdates { get; private set; }
    public ReactiveCommand<RxVoid, RxVoid> Close { get; private set; }

    [Reactive] public partial bool IsStartServer { get; set; } = false;

    public GameMenuViewModel(ILogger<GameMenuViewModel>? logger, IWindowManager windowManager,
        UserManager userManager, ILauncherStorage launcherStorage, IApplicationLocaleManager localeManager)
    {
        Localization = localeManager;
        _logger = logger;
        _windowManager = windowManager;
        _userManager = userManager;
        _launcherStorage = launcherStorage;

        SetupCommands();

        PlayGame = PlayGame ?? throw new NullReferenceException(nameof(PlayGame));
        StartServer = StartServer ?? throw new NullReferenceException(nameof(StartServer));
        CheckUpdates = CheckUpdates ?? throw new NullReferenceException(nameof(CheckUpdates));
        Close = Close ?? throw new NullReferenceException(nameof(Close));
    }


    private void SetupCommands()
    {
        var canExecuteServer = this.WhenAnyValue(x => x.IsStartServer,
                startServer => startServer == false)
            .ObserveOn(RxSchedulers.MainThreadScheduler);
        var isGitHubConnection = this.WhenAnyValue(x => x._launcherStorage.IsCheckGitHubConnection);

        PlayGame = ReactiveCommand.Create<MainWindowViewModel>(PlayGameImpl);
        StartServer = ReactiveCommand.Create(StartServerImpl, canExecuteServer);
        CheckUpdates = ReactiveCommand.CreateFromTask<LauncherViewModel>(CheckUpdatesImplAsync, isGitHubConnection);
        Close = ReactiveCommand.Create(_windowManager.Close);
    }

    private async Task CheckUpdatesImplAsync(LauncherViewModel launcherViewModel)
    {
        await launcherViewModel.SelectUpdateMenuAsync();
    }

    private void PlayGameImpl(MainWindowViewModel mainWindowViewModel)
    {
        if (_userManager is null)
        {
            throw new NullReferenceException("User manager object is null");
        }
        if (_userManager.UserSettings is null)
        {
            throw new NullReferenceException("User settings object is null");
        }

        ProcessHelper.KillServers();

        if (IsStartServer)
        {
            var launch = Core.Launcher.Launch(path: @"binaries\xrEngine.exe",
                arguments: [
                    @$"-start client(localhost/name={_userManager.UserSettings.Username})"
                ]);

            if (launch == null)
            {
                return;
            }
            launch.Start();

            _windowManager.Close();
        }
        else
        {
            mainWindowViewModel.ShowStartGameImpl();
        }
    }

    private void StartServerImpl()
    {
        ProcessHelper.KillAllXrEngine();

        var launch = Core.Launcher.Launch(path: @"binaries\xrEngine.exe",
            arguments: [
                "-dedicated",
                "-i",
                @"-start server(belarus_lobby/fmp/timelimit=60) client(localhost)",
            ]);

        if (launch is null)
        {
            return;
        }

        launch.Exited += LaunchOnExited;
        launch.Start();

        IsStartServer = true;
    }

    private void LaunchOnExited(object? sender, EventArgs e)
    {
        IsStartServer = false;
    }
}
