using Belarus.Launcher.Core.Manager;
using Belarus.Launcher.ViewModels.Validators;

using Microsoft.Extensions.Logging;

using ReactiveUI;
using ReactiveUI.Primitives.Disposables;
using ReactiveUI.SourceGenerators;

namespace Belarus.Launcher.ViewModels;

public partial class StartGameViewModel : ReactiveValidationObject, IDisposable
{
    private readonly ILogger<StartGameViewModel>? _logger;
    private readonly IWindowManager _windowManager;
    private readonly UserManager _userManager;
    private readonly StartGameViewModelValidator _startGameViewModelValidator;
    private MultipleDisposable? _disposables;

    public IApplicationLocaleManager Localization { get; private set; }
    [Reactive] public partial string IpAddress { get; set; }

    public ReactiveCommand<RxVoid, RxVoid> StartGame { get; private set; } = null!;
    public ReactiveCommand<MainWindowViewModel, RxVoid> Back { get; private set; } = null!;

    public StartGameViewModel(ILogger<StartGameViewModel>? logger, UserManager userManager,
        IWindowManager windowManager, IApplicationLocaleManager localization,
        StartGameViewModelValidator startGameViewModelValidator)
    {
        Localization = localization;
        _logger = logger;
        _logger?.LogInformation("StartGameViewModel ctor");

        _userManager = userManager;

        if (_userManager is null)
        {
            throw new NullReferenceException("User manager object is null");
        }
        if (_userManager.UserSettings is null)
        {
            throw new NullReferenceException("User settings object is null");
        }

        IpAddress = _userManager.UserSettings.IpAddress;

        _windowManager = windowManager;
        _startGameViewModelValidator = startGameViewModelValidator;

        SetupCommands();
    }

    private void SetupCommands()
    {
        StartGame = ReactiveCommand.Create(StartGameImpl, this.IsValid());
        Back = ReactiveCommand.Create<MainWindowViewModel>(BackImpl);

        Localization.WhenAnyValue(x => x.Locale)
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(_ =>
            {
                _disposables?.Dispose();
                SetupValidation();
            });
    }

    private void SetupValidation()
    {
        _logger?.LogInformation("StartGameViewModel: setup validation");

        _disposables = [
            _startGameViewModelValidator.EnsureIpAddressNotEmpty(this),
            _startGameViewModelValidator.EnsureValidIpAddressOrUrl(this)
        ];
    }

    private void StartGameImpl()
    {
        if (_userManager is null)
        {
            throw new NullReferenceException("User manager object is null");
        }
        if (_userManager.UserSettings is null)
        {
            throw new NullReferenceException("User settings object is null");
        }

        if (string.IsNullOrWhiteSpace(IpAddress))
        {
            throw new Exception(Localization.GetStringByKey("LocalizedStrings.NoIpAddressEntered"));
        }

        _userManager.UserSettings.IpAddress = IpAddress;
        _userManager.Save();

        var process = Core.Launcher.Launch(path: @"binaries\xrEngine.exe",
            arguments: [
                @$"-start -center_screen -silent_error_mode client({_userManager.UserSettings.IpAddress}/name={ _userManager.UserSettings.Username})"
            ]);

        process?.Start();
        _windowManager.Close();
    }

    private void BackImpl(MainWindowViewModel mainWindowViewModel)
    {
        mainWindowViewModel.ShowLauncherImpl();
    }

    protected new virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            ValidationContext.Dispose();

            if (_disposables is not null)
            {
                _disposables?.Dispose();
                _disposables = null;
            }
        }
    }

    public new void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
