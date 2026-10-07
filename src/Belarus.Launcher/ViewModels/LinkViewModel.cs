using System.Collections.ObjectModel;

using DynamicData;

using Belarus.Launcher.Core.Models;
using Belarus.Launcher.Core.Services;
using Belarus.Launcher.Core.Storage;

using Microsoft.Extensions.Logging;

using ReactiveUI;

namespace Belarus.Launcher.ViewModels;

public partial class LinkViewModel : ReactiveObject
{
    private readonly ILogger<LinkViewModel>? _logger;
    private readonly IWebsiteLauncher _websiteLauncher;
    private readonly ILauncherStorage _launcherStorage;

    public ObservableCollection<WebResource> WebResources { get; set; } = [];
    public ReactiveCommand<string, RxVoid> OpenUrlCommand { get; set; }

    public LinkViewModel(ILogger<LinkViewModel>? logger, IWebsiteLauncher websiteLauncher,
        ILauncherStorage launcherStorage)
    {
        _logger = logger;
        _websiteLauncher = websiteLauncher;
        _launcherStorage = launcherStorage;

        OpenUrlCommand = ReactiveCommand.Create<string>(OpenUrl);

        this.WhenAnyValue(x => x._launcherStorage.WebResources)
            .Where(webRes => webRes != null && webRes.Any())
            .Subscribe((n) => Init());
    }

    private void Init()
    {
        if (_launcherStorage.WebResources is not null)
        {
            _logger?.LogInformation("Web resources are initialized");
            WebResources.AddRange(_launcherStorage.WebResources);
        }
        else
        {
            _logger?.LogError("Web resources is null!");
        }
    }

    private void OpenUrl(string url) => _websiteLauncher.OpenWebsite(url);
}
