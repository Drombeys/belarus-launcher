using Belarus.Launcher.Core.Helpers;
using Belarus.Launcher.Core.Services;
using Belarus.Launcher.Core.Storage;
using Belarus.Launcher.Core.Validators;
using Belarus.Launcher.Services;

using Microsoft.Extensions.Logging;

using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace Belarus.Launcher.ViewModels;

public partial class LauncherViewModel : ReactiveObject
{
    private readonly ILogger<LauncherViewModel>? _logger;
    private readonly IWebsiteLauncher _websiteLauncher;
    private readonly DownloadMenuViewModel _downloadMenuViewModel;
    private readonly GameMenuViewModel _gameMenuViewModel;
    private readonly GameDirectoryValidator _directoryValidator;

    public string AppVersion { get; set; }
    public string CompanyName { get; set; }

    [Reactive] public partial ReactiveObject? PageMenuViewModel { get; set; }
    [Reactive] public partial NewsSliderViewModel NewsSliderViewModel { get; set; }

    public ReactiveCommand<RxVoid, RxVoid>? OpenMainRepositoryUriCommand { get; set; }
    public ReactiveCommand<RxVoid, RxVoid>? OpenOrganizationUriCommand { get; set; }

    public LauncherViewModel(ILogger<LauncherViewModel>? logger, ViewModelLocator viewModelLocator,
        GameDirectoryValidator directoryValidator, IWebsiteLauncher websiteLauncher)
    {
        _logger = logger;
        _downloadMenuViewModel = viewModelLocator.DownloadMenuViewModel;
        _gameMenuViewModel = viewModelLocator.GameMenuViewModel;
        NewsSliderViewModel = viewModelLocator.NewsSliderViewModel;

        _directoryValidator = directoryValidator;
        _websiteLauncher = websiteLauncher;

        AppVersion = ApplicationHelper.GetAppVersion();
        CompanyName = (char) 0169 + ApplicationHelper.GetCompanyName();

        SetupCommands();
    }

    private void SetupCommands()
    {
        OpenMainRepositoryUriCommand = ReactiveCommand.Create(() => OpenUrl(UriStorage.LauncherUri.AbsoluteUri));
        OpenOrganizationUriCommand = ReactiveCommand.Create(() => OpenUrl(UriStorage.AuthorUri.AbsoluteUri));
    }

    private void OpenUrl(string uri) => _websiteLauncher.OpenWebsite(uri);

    public void SelectMenu()
    {
        PageMenuViewModel = _directoryValidator.IsDirectoryValid() ? _gameMenuViewModel : _downloadMenuViewModel;
    }

    public async Task SelectUpdateMenuAsync()
    {
        PageMenuViewModel = _downloadMenuViewModel;
        await _downloadMenuViewModel.UpdateAsync(this);
    }
}
