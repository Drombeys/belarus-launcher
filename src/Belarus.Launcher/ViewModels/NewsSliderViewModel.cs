using System.Collections.ObjectModel;

using Belarus.Launcher.Core.Manager;
using Belarus.Launcher.Core.Models;
using Belarus.Launcher.Core.Storage;
using Belarus.Launcher.Services;

using Microsoft.Extensions.Logging;

using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace Belarus.Launcher.ViewModels;

public partial class NewsSliderViewModel : ReactiveObject
{
    private readonly ILogger<NewsSliderViewModel>? _logger;
    private readonly ILauncherStorage _launcherStorage;
    private readonly IApplicationLocaleManager _localeManager;
    private readonly ViewModelLocator _viewModelLocator;

    [Reactive] public partial int NumPage { get; set; }
    [Reactive] public partial NewsViewModel? SelectedNewsViewModel { get; private set; }
    [Reactive] public partial LinkViewModel LinkViewModel { get; set; }
    [Reactive] public partial ObservableCollection<NewsViewModel>? News { get; set; }

    public ReactiveCommand<RxVoid, RxVoid> GoNext { get; set; }
    public ReactiveCommand<RxVoid, RxVoid> GoBack { get; set; }

    public NewsSliderViewModel(ILogger<NewsSliderViewModel>? logger, ViewModelLocator viewModelLocator,
        ILauncherStorage launcherStorage, IApplicationLocaleManager localeManager)
    {
        News = [new(localeManager.GetStringByKey("LocalizedStrings.Warning"),
                            localeManager.GetStringByKey("LocalizedStrings.LoadNews"))];

        _logger = logger;
        _viewModelLocator = viewModelLocator;
        _launcherStorage = launcherStorage;
        _localeManager = localeManager;
        LinkViewModel = viewModelLocator.LinkViewModel;

        SetupBinding();
        SetupCommands();

        GoNext = GoNext ?? throw new NullReferenceException(nameof(GoNext));
        GoBack = GoBack ?? throw new NullReferenceException(nameof(GoBack));
    }

    private void ReloadNews(string locale)
    {
        _logger?.LogInformation("Reload News");

        if (string.IsNullOrEmpty(locale))
        {
            _logger?.LogError("Locale not set");
            return;
        }

        if (_launcherStorage.NewsContents is null)
        {
            _logger?.LogError("News content is null");
            return;
        }

        var news = _launcherStorage.NewsContents
            .FirstOrDefault(x => x.Locale != null && x.Locale.Key.Equals(locale));
        if (news is not null)
        {
            SetNews(news.NewsContents!);
        }
        else
        {
            _logger?.LogError("News collection is empty");
        }
    }

    private void SetupCommands()
    {
        var canExecuteBack = this.WhenAnyValue(x => x.NumPage,
                (numPage) => numPage != 0)
            .ObserveOn(RxSchedulers.MainThreadScheduler);
        var canExecuteNext = this.WhenAnyValue(x => x.NumPage,
                (numPage) => News != null && numPage != News.Count - 1 && News.Count != 0)
            .ObserveOn(RxSchedulers.MainThreadScheduler);

        GoNext = ReactiveCommand.Create(GoNextImpl, canExecuteNext);
        GoBack = ReactiveCommand.Create(GoBackImpl, canExecuteBack);

        var canLoadNews = this.WhenAnyValue(x => x._launcherStorage.NewsContents)
            .Any(news => news != null && news.Any());

        var reloadNewsCommand = ReactiveCommand.Create<string>((lang) =>
        {
            _logger?.LogInformation("Language has been changed!");
            ReloadNews(lang);
        }, canLoadNews);
        this.WhenAnyValue(x => x._localeManager.Locale)
            .InvokeCommand(reloadNewsCommand);
    }

    private void SetupBinding()
    {
        this.WhenAnyValue(x => x.NumPage)
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Where(x => News != null && x >= 0 && x < News.Count)
            .Subscribe(x =>
            {
                if (News is not null && News.Any())
                {
                    SelectedNewsViewModel = News[x];
                }
            });

        this.WhenAnyValue(x => x._launcherStorage.NewsContents)
            .Where(news => news != null && !string.IsNullOrEmpty(_localeManager.Locale) && news.Any())
            .Subscribe(_ =>
            {
                var locale = _localeManager.Locale;
                ReloadNews(locale);
            });
    }

    private void GoNextImpl()
    {
        if (News is null)
        {
            return;
        }
        if (NumPage < News.Count - 1)
        {
            NumPage++;
        }
    }

    private void GoBackImpl()
    {
        if (NumPage > 0)
        {
            NumPage--;
        }
    }

    public void SetNews(IEnumerable<NewsContent> newsContents)
    {
        News = [];

        foreach (var content in newsContents)
        {
            News.Add(new NewsViewModel(content!.Title, content.Description));
        }

        NumPage = News.Count - 1;
        SelectedNewsViewModel = News[NumPage];
    }
}
