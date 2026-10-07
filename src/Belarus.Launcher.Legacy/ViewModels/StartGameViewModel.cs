using Belarus.Launcher.Core.Manager;
using Belarus.Launcher.Core.Models;
using Belarus.Launcher.Legacy.Manager;

using ReactiveUI.SourceGenerators;

namespace Belarus.Launcher.ViewModels;

public partial class StartGameViewModel : ViewModelBase, IRoutableViewModel
{
    private readonly IWindowManager _windowManager;
    private readonly UserSettings _userSettings;

    [Reactive] public partial string IpAddress { get; set; } = string.Empty;

    public ReactiveCommand<RxVoid, RxVoid> StartGame { get; private set; } = null!;
    public ReactiveCommand<RxVoid, RxVoid> Back { get; private set; } = null!;

    public string? UrlPathSegment { get; set; } = "StartGameViewModel";

    public IScreen HostScreen { get; set; } = null!;

    public StartGameViewModel(IWindowManager windowManager, UserSettings userSettings)
    {
        _windowManager = windowManager;
        _userSettings = userSettings;

        IpAddress = _userSettings.IpAddress ?? "";

        SetupBinding();
    }

    private void SetupBinding()
    {
        var canStartGame = this.WhenAnyValue(x => x.IpAddress,
            (ip) => !string.IsNullOrWhiteSpace(ip))
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .DistinctUntilChanged();

        StartGame = ReactiveCommand.Create(StartGameImpl, canStartGame);
        Back = ReactiveCommand.Create(BackImpl);
    }

    private void StartGameImpl()
    {
        if (_userSettings is null)
        {
            throw new NullReferenceException("User manager object is null");
        }

        if (string.IsNullOrWhiteSpace(IpAddress))
        {
            throw new Exception("Ip-адрес не введен!");
        }

        _userSettings.IpAddress = IpAddress;
        ConfigManager.SaveSettings(_userSettings);

        var gameProcess = Core.Launcher.Launch(path: @"binaries\xrEngine.exe", arguments: [
            @$"-start -center_screen -silent_error_mode client({_userSettings.IpAddress}/name={_userSettings.Username})"
        ]);
        gameProcess?.Start();
        BackImpl();

        return;
    }

    private void BackImpl()
    {
        _windowManager.Close();
    }
}
