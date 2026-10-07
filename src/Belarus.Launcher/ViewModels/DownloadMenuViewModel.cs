using Belarus.Launcher.Core.Manager;
using Belarus.Launcher.Core.Services;
using Belarus.Launcher.Core.Storage;

using Microsoft.Extensions.Logging;

using ReactiveUI;
using ReactiveUI.SourceGenerators;

using ReactiveUI.Primitives.Signals;

namespace Belarus.Launcher.ViewModels;

public partial class DownloadMenuViewModel : ReactiveObject
{
    private readonly ILogger<DownloadMenuViewModel>? _logger;
    private readonly IWindowManager _windowManager;
    private readonly IDownloadResourcesService _downloadResourcesService;
    private readonly ILauncherStorage _launcherStorage;
    private CancellationTokenSource _cts;

    public IApplicationLocaleManager Localization { get; private set; }
    public CancellationToken CancellationToken { get; private set; }
    public ReactiveCommand<LauncherViewModel, RxVoid> StartDownload { get; private set; }
    public ReactiveCommand<RxVoid, RxVoid> Pause { get; private set; }
    public ReactiveCommand<RxVoid, RxVoid> Close { get; private set; }

    [Reactive] public partial int DownloadProgress { get; set; } = 0;
    [Reactive] public partial string StatusProgress { get; set; } = string.Empty;
    [Reactive] public partial string DownloadFileName { get; set; } = string.Empty;

    // Overall progress status
    [Reactive] public partial bool IsProgress { get; set; }

    //Download status
    [Reactive] public partial bool IsDownload { get; set; }

    public DownloadMenuViewModel(ILogger<DownloadMenuViewModel>? logger,
        IApplicationLocaleManager localization,
        IWindowManager windowManager,
        IDownloadResourcesService downloadResourcesService,
        ILauncherStorage launcherStorage)
    {
        Localization = localization;
        _logger = logger;
        _windowManager = windowManager;
        _downloadResourcesService = downloadResourcesService;
        _launcherStorage = launcherStorage;
        IsDownload = false;
        _cts = null!;

        SetupCommands();

        StartDownload = StartDownload ?? throw new NullReferenceException(nameof(StartDownload));
        Pause = Pause ?? throw new NullReferenceException(nameof(Pause));
        Close = Close ?? throw new NullReferenceException(nameof(Close));
    }

    public async Task UpdateAsync(LauncherViewModel launcherViewModel)
    {
        await StartDownload.Execute(launcherViewModel).ToTask();
    }

    private void SetupCommands()
    {
        var isGitHubConnection = this.WhenAnyValue(x => x._launcherStorage.IsCheckGitHubConnection);

        StartDownload = ReactiveCommand.CreateFromTask<LauncherViewModel>(DownloadsImplAsync, isGitHubConnection);
        Pause = ReactiveCommand.Create(PauseImpl);
        Close = ReactiveCommand.Create(CloseImpl);

        StartDownload.ThrownExceptions.Merge(Close.ThrownExceptions)
            .Throttle(TimeSpan.FromMilliseconds(250), RxSchedulers.MainThreadScheduler)
            .Subscribe(OnCommandException);
    }

    private void PauseImpl()
    {
        ClearData();
        _cts?.Cancel();
    }

    private void ClearData()
    {
        DownloadProgress = 0;
        DownloadFileName = string.Empty;
        IsProgress = false;
        IsDownload = false;
    }

    private void CloseImpl()
    {
        PauseImpl();

        _windowManager.Close();
    }

    private async Task DownloadsImplAsync(LauncherViewModel launcherViewModel)
    {
        _cts = new();
        CancellationToken = _cts.Token;
        ClearData();

        IProgress<int> progress = new Progress<int>(percentage =>
        {
            DownloadProgress = percentage;
        });

        IsProgress = true;
        StatusProgress = Localization.GetStringByKey("LocalizedStrings.IntegrityChecking");

        var filesDownload = await _downloadResourcesService.GetFilesForDownloadAsync(progress, CancellationToken);

        if (CancellationToken.IsCancellationRequested)
        {
            launcherViewModel.SelectMenu();
            PauseImpl();
            return;
        }

        if (filesDownload is not null && filesDownload.Any())
        {
            var countFiles = filesDownload.Count;
            var numberFile = 0;
            IsDownload = true;
            foreach (var file in filesDownload)
            {
                numberFile++;
                StatusProgress = Localization.GetStringByKey("LocalizedStrings.Files") +
                                 $": {numberFile} / {countFiles}";
                DownloadFileName = Path.GetFileName(file.Key);
                await _downloadResourcesService.DownloadAsync(file.Key, file.Value, progress, CancellationToken);

                if (CancellationToken.IsCancellationRequested)
                {
                    PauseImpl();
                    launcherViewModel.SelectMenu();
                    return;
                }

                progress.Report(0);
            }
        }

        progress.Report(0);
        ClearData();

        launcherViewModel.SelectMenu();
    }

    private void OnCommandException(Exception exception)
    {
        ClearData();

        _logger?.LogError("{Message}", exception.Message);
        _logger?.LogError("{Message}", exception.StackTrace);
    }
}
