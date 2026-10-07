using System.Collections.ObjectModel;

using DynamicData;

using Belarus.Launcher.Core.Manager;
using Belarus.Launcher.Core.Models;
using Belarus.Launcher.Core.Storage;
using Belarus.Launcher.Services;
using Belarus.Launcher.ViewModels.Validators;

using Microsoft.Extensions.Logging;

using ReactiveUI;
using ReactiveUI.Primitives.Disposables;
using ReactiveUI.SourceGenerators;

namespace Belarus.Launcher.ViewModels;

public partial class AuthorizationViewModel : ReactiveValidationObject, IDisposable
{
    private readonly ILogger<AuthorizationViewModel>? _logger;
    private readonly ILauncherStorage _launcherStorage;
    private readonly IWindowManager _windowManager;
    private readonly UserManager _userManager;
    private readonly AuthenticationViewModelValidator _authenticationViewModelValidator;
    private readonly LauncherViewModel _launcherViewModel;

    private MultipleDisposable? _disposables = null;

    public IApplicationLocaleManager Localization { get; private set; }
    [Reactive] public partial ObservableCollection<Locale> Languages { get; set; } = new();

    [Reactive] public partial Locale SelectedLanguage { get; set; } = new();

    [Reactive] public partial string Username { get; set; } = string.Empty;

    public ReactiveCommand<string, RxVoid> UpdateInterfaceCommand { get; private set; } = null!;
    public ReactiveCommand<MainWindowViewModel, RxVoid> ShowLauncher { get; private set; } = null!;
    public ReactiveCommand<RxVoid, RxVoid> Close { get; private set; } = null!;

    public AuthorizationViewModel(ILogger<AuthorizationViewModel>? logger,
        ILauncherStorage launcherStorage, IApplicationLocaleManager localization,
        IWindowManager windowManager, UserManager userManager,
        AuthenticationViewModelValidator authenticationViewModelValidator,
        ViewModelLocator viewModelLocator)
    {
        _logger = logger;
        _launcherStorage = launcherStorage;
        Localization = localization;
        _windowManager = windowManager;
        _userManager = userManager;
        _authenticationViewModelValidator = authenticationViewModelValidator;
        _launcherViewModel = viewModelLocator.LauncherViewModel;

        SetupBinding();
    }

    public void ShowLauncherImpl(MainWindowViewModel mainWindowViewModel)
    {
        var username = Username.Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new Exception(Localization.GetStringByKey("LocalizedStrings.UsernameNotEntered"));
        }

        if (_userManager is null)
        {
            throw new NullReferenceException("User manager object is null");
        }
        if (_userManager.UserSettings is null)
        {
            throw new NullReferenceException("User settings object is null");
        }
        if (_userManager.UserSettings.Locale is null)
        {
            throw new NullReferenceException("User settings locale object is null");
        }

        _userManager.UserSettings.Username = username;
        _userManager.UserSettings.Locale = SelectedLanguage;
        _userManager.Save();

        if (!File.Exists(PathStorage.GameUser))
        {
            using var writer = new StreamWriter(PathStorage.GameUser, true);
            writer.WriteLine($"language {SelectedLanguage.Key}");
        }

        // TODO: Удалить в следующем релизе
        if (!File.Exists(PathStorage.LegacyGameUser))
        {
            if (!Directory.Exists(DirectoryStorage.LegacyUserData))
            {
                Directory.CreateDirectory(DirectoryStorage.LegacyUserData);
            }

            using var writer = new StreamWriter(PathStorage.LegacyGameUser, true);
            writer.WriteLine($"language {SelectedLanguage.Key}");
        }
        _launcherViewModel.SelectMenu();
        mainWindowViewModel.ShowLauncherImpl();
    }

    private void SetupBinding()
    {
        _logger?.LogInformation("Call AuthorizationViewModel::SetupBinding()");

        if (_userManager is null)
        {
            throw new NullReferenceException("User manager object is null");
        }
        if (_userManager.UserSettings is null)
        {
            throw new NullReferenceException("User settings object is null");
        }
        if (_userManager.UserSettings.Locale is null)
        {
            throw new NullReferenceException("User settings locale object is null");
        }

        Languages.AddRange(_launcherStorage.Locales);
        if (_userManager.UserSettings.Locale.Key == string.Empty)
        {
            SelectedLanguage = Languages[0];
        }
        else
        {
            SelectedLanguage = Languages.FirstOrDefault(x => x.Key.Equals(_userManager.UserSettings.Locale.Key)) ?? Languages[0];
        }

        UpdateInterfaceCommand = ReactiveCommand.Create<string>(key =>
        {
            Localization.SetLocale(key);
            _userManager.UserSettings.Locale = SelectedLanguage;
            _disposables?.Dispose();
            SetupValidation();
        });

        this.WhenAnyValue(x => x.SelectedLanguage.Key)
            .Where(key => !string.IsNullOrEmpty(key))
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .InvokeCommand(UpdateInterfaceCommand);

        ShowLauncher = ReactiveCommand.Create<MainWindowViewModel>(ShowLauncherImpl, this.IsValid());
        Close = ReactiveCommand.Create(_windowManager.Close);
    }

    private void SetupValidation()
    {
        _disposables = [
            _authenticationViewModelValidator.EnsureUsernameNotEmpty(this),
            _authenticationViewModelValidator.EnsureUsernameCorrectLength(this),
            _authenticationViewModelValidator.EnsureUsernameCorrectCharacters(this),
        ];
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
