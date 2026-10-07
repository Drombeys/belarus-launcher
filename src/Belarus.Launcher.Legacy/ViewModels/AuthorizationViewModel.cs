using Belarus.Launcher.Core.Manager;
using Belarus.Launcher.Core.Models;
using Belarus.Launcher.Legacy.Manager;

using ReactiveUI.SourceGenerators;

namespace Belarus.Launcher.ViewModels;

public partial class AuthorizationViewModel : ViewModelBase, IRoutableViewModel
{
    private readonly IWindowManager _windowManager;
    private readonly LauncherViewModel _launcherViewModel;
    private readonly UserSettings _userSettings;

    [Reactive] public partial string UserName { get; set; } = string.Empty;

    public ReactiveCommand<RxVoid, RxVoid> Next { get; private set; } = null!;
    public ReactiveCommand<RxVoid, RxVoid> Close { get; private set; } = null!;

    public string? UrlPathSegment { get; set; } = "";

    public IScreen HostScreen { get; set; } = null!;

    public AuthorizationViewModel(IWindowManager windowManager, LauncherViewModel launcherViewModel, UserSettings userSettings)
    {
        _windowManager = windowManager;

        ArgumentNullException.ThrowIfNull(launcherViewModel);

        _userSettings = userSettings;

        _launcherViewModel = launcherViewModel;
        _launcherViewModel.HostScreen = HostScreen;

        SetupBinding();
    }

    private void SetupBinding()
    {
        var canCreateUser = this.WhenAnyValue(x => x.UserName,
            (nickname) => !string.IsNullOrWhiteSpace(nickname) && nickname.Length <= 22)
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .DistinctUntilChanged();

        Next = ReactiveCommand.Create(NextImpl, canCreateUser);
        Close = ReactiveCommand.Create(_windowManager.Close);
    }

    private void NextImpl()
    {
        ArgumentNullException.ThrowIfNull(HostScreen);

        if (string.IsNullOrWhiteSpace(UserName))
        {
            throw new Exception("Имя пользователя не введено!");
        }

        _userSettings.Username = UserName;
        ConfigManager.SaveSettings(_userSettings);

        HostScreen.Router.Navigate.Execute(_launcherViewModel);
    }
}
