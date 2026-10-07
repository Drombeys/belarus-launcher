using Belarus.Launcher.Core.Manager;
using Belarus.Launcher.Models;

using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace Belarus.Launcher.ViewModels;

public partial class SplashScreenViewModel : ReactiveObject
{
    private readonly IWindowManager _windowManager;

    public IApplicationLocaleManager Localization { get; private set; }
    public ISplashScreenManager SplashScreen { get; set; }
    [Reactive] public partial InformationMessage InformationMessage { get; set; }
    [Reactive] public partial int Progress { get; set; }
    public int MaxProgress { get; private set; }

    public ReactiveCommand<RxVoid, RxVoid> Cancel { get; set; }

    public SplashScreenViewModel(IWindowManager windowManager, ISplashScreenManager splashScreen,
        IApplicationLocaleManager localization)
    {
        _windowManager = windowManager;
        SplashScreen = splashScreen;
        Localization = localization;
        Progress = SplashScreen.CurrentProgress;
        MaxProgress = SplashScreen.MaxProgress;

        SplashScreen.WhenAnyValue(
            x => x.CurrentProgress,
            x => x.SplashScreenMessage)
            .Subscribe(values =>
            {
                Progress = values.Property1;
                InformationMessage = values.Property2;
            });

        Cancel = ReactiveCommand.Create(CancelImpl);
    }


    private void CancelImpl()
    {
        SplashScreen.Cancel();
        _windowManager.Close();
    }
}
