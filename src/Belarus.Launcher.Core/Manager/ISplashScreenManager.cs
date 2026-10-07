using System.ComponentModel;

using Belarus.Launcher.Models;

namespace Belarus.Launcher.Core.Manager;

public interface ISplashScreenManager : INotifyPropertyChanged
{
    CancellationToken CancellationToken { get; }
    InformationMessage SplashScreenMessage { get; }
    int CurrentProgress { get; set; }
    int MaxProgress { get; set; }
    void UpdateInformation(InformationMessage splashScreenMessage);
    void Cancel();
}
