using System.Collections.ObjectModel;
using System.ComponentModel;

using Belarus.Launcher.Core.Models;

namespace Belarus.Launcher.Core.Storage;

public interface ILauncherStorage : INotifyPropertyChanged
{
    GitHubRelease? GitHubRelease { get; set; }
    IList<Locale> Locales { get; }
    ObservableCollection<LangNewsContent>? NewsContents { get; set; }
    ObservableCollection<WebResource>? WebResources { get; set; }
    bool IsCheckGitHubConnection { get; set; }
    bool IsGameReleaseCurrent { get; set; }
    bool IsUserAuthorized { get; set; }
}
