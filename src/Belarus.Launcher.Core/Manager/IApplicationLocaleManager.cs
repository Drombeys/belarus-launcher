using System.ComponentModel;

namespace Belarus.Launcher.Core.Manager;

public interface IApplicationLocaleManager : INotifyPropertyChanged
{
    void SetLocale(string locale);
    string Locale { get; }
    string GetStringByKey(string key);
    string this[string key] { get; }
}
