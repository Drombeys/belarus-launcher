using Belarus.Launcher.Core.Manager;
using Belarus.Launcher.Core.Models;

using ReactiveUI.SourceGenerators;

namespace Belarus.Launcher.ViewModels;

public partial class MenuViewModel : ViewModelBase
{
    private readonly IWindowManager _windowManager;
    private readonly UserSettings _userSettings;
    private readonly DownloadManager _downloadService;

    public LauncherViewModel? LauncherViewModel { get; set; }

    private bool _isStartServer = false;

    [Reactive] public partial bool IsVisibleDownload { get; set; } = true;
    [Reactive] public partial bool IsVisiblePlayGame { get; set; } = false;
    [Reactive] public partial bool IsDownloadStart { get; set; } = false;
    [Reactive] public partial bool IsDownloadCheak { get; set; } = false;

    public ReactiveCommand<RxVoid, RxVoid> Close { get; private set; } = null!;
    public ReactiveCommand<RxVoid, RxVoid> PlayGame { get; private set; } = null!;
    public ReactiveCommand<RxVoid, RxVoid> StartServer { get; private set; } = null!;
    public ReactiveCommand<RxVoid, RxVoid> CheckUpdates { get; private set; } = null!;
    public ReactiveCommand<RxVoid, RxVoid> StartDownload { get; private set; } = null!;

    public MenuViewModel(IWindowManager windowManager, UserSettings userSettings, DownloadManager downloadService)
    {
        _downloadService = downloadService;
        _windowManager = windowManager;
        _userSettings = userSettings;

        SetupCommands();
    }

    private void SetupCommands()
    {
        var result = _downloadService.CheckFiles();
        if (result)
        {
            IsVisiblePlayGame = File.Exists(Directory.GetCurrentDirectory() + "binaries\\xrengine.exe");
            IsVisibleDownload = !IsVisiblePlayGame;
        }
        else
        {
            IsVisibleDownload = false;
            IsVisiblePlayGame = true;
        }

        Close = ReactiveCommand.Create(_windowManager.Close);
        PlayGame = ReactiveCommand.Create(PlayGameImpl);
        StartServer = ReactiveCommand.Create(StartServerImpl);
        CheckUpdates = ReactiveCommand.CreateFromTask(DownloadsImpl);
        StartDownload = ReactiveCommand.CreateFromTask(DownloadsImpl);
    }

    private async Task DownloadsImpl()
    {
        IsVisibleDownload = false;
        IsVisiblePlayGame = false;
        IsDownloadStart = false;
        IsDownloadCheak = true;

        await Task.Run(() =>
        {
            if (_downloadService.CheckFiles())
            {
                IsDownloadStart = true;
                IsDownloadCheak = false;

                _downloadService.CheckFiles(true);
            }

            IsDownloadStart = false;
            IsVisiblePlayGame = true;
            IsVisibleDownload = false;
            IsDownloadCheak = false;
        });
    }

    private void StartServerImpl()
    {
        var serverProcess = Core.Launcher.Launch(path: @"binaries\xrEngine.exe", arguments: [
            "-dedicated",
            "-i",
            @"-start server(belarus_lobby/fmp/timelimit=60) client(localhost)",
        ]);
        serverProcess?.Start();
        _isStartServer = true;
    }

    private void PlayGameImpl()
    {
        if (_userSettings is null)
        {
            throw new NullReferenceException("User manager object is null");
        }

        if (_isStartServer)
        {
            var gameProcess = Core.Launcher.Launch(path: @"binaries\xrEngine.exe", arguments: [
                @$"-start client(localhost/name={_userSettings.Username})"
            ]);
            gameProcess?.Start();
            _windowManager.Close();
            return;
        }

        LauncherViewModel?.StartGame();
    }
}
