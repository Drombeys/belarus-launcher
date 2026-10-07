using System.Diagnostics;

using Belarus.Launcher.Core.Helpers;
using Belarus.Launcher.Core.Models;
using Belarus.Launcher.Core.Services;
using Belarus.Launcher.Core.Storage;
using Belarus.Launcher.Models;

using Microsoft.Extensions.Logging;

namespace Belarus.Launcher.Core.Manager;

public class InitializerManager(
    ILogger<InitializerManager>? logger,
    HttpClient httpClient,
    IGitStorageApiService gitStorageApiService, UserManager userManager,
    IApplicationLocaleManager localeManager, ILauncherStorage launcherStorage,
    IReleaseComparerService<GitHubRelease> releaseComparerService,
    IUpdaterService updaterService)
{
    public void InitializeLocale()
    {
        if (userManager is null)
        {
            throw new NullReferenceException("User manager object is null");
        }
        if (userManager.UserSettings is null)
        {
            throw new NullReferenceException("User settings object is null");
        }
        if (userManager.UserSettings.Locale is null)
        {
            logger?.LogError("Locale was not set");
            userManager.UserSettings.Locale = launcherStorage.Locales[0];
        }

        localeManager.SetLocale(userManager.UserSettings.Locale.Key);
    }

    public async Task InitializeAsync(ISplashScreenManager splashScreenManager)
    {
        try
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if (userManager is null)
            {
                throw new NullReferenceException("User manager object is null");
            }
            if (userManager.UserSettings is null)
            {
                throw new NullReferenceException("User settings object is null");
            }
            if (userManager.UserSettings.Locale is null)
            {
                logger?.LogError("User settings locale object is null. Default locale will be selected");
                userManager.UserSettings.Locale = launcherStorage.Locales[0];
            }

            if (!Directory.Exists(DirectoryStorage.LauncherCache))
            {
                Directory.CreateDirectory(DirectoryStorage.LauncherCache);
            }

            splashScreenManager.UpdateInformation(new InformationMessage(
                localeManager.GetStringByKey("LocalizedStrings.Loading"),
                localeManager.GetStringByKey("LocalizedStrings.AccessingRepository")));

            launcherStorage.IsCheckGitHubConnection = await IsCheckGitHubConnectionAsync(splashScreenManager.CancellationToken);
            logger?.LogInformation("Check GitHub connection time: {Time}", stopwatch.ElapsedMilliseconds);

            if (launcherStorage.IsCheckGitHubConnection)
            {
                await HandleOnlineInitializationAsync(splashScreenManager, stopwatch);
            }
            else
            {
                splashScreenManager.UpdateInformation(new InformationMessage(
                    localeManager.GetStringByKey("LocalizedStrings.OfflineLoading"),
                    localeManager.GetStringByKey("LocalizedStrings.LoadLocalData")));

                await HandleOfflineInitializationAsync(splashScreenManager.CancellationToken);
            }

            stopwatch.Stop();
            logger?.LogInformation("Parsing time: {Time}", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            logger?.LogError("{Message}", ex.Message);
            logger?.LogError("{StackTrace}", ex.StackTrace);
            throw;
        }
    }

    private async Task HandleOnlineInitializationAsync(ISplashScreenManager splashScreenManager, Stopwatch stopwatch)
    {
        if (userManager is null)
        {
            throw new NullReferenceException("User manager object is null");
        }
        if (userManager.UserSettings is null)
        {
            throw new NullReferenceException("User settings object is null");
        }

        splashScreenManager.UpdateInformation(new InformationMessage(
            localeManager.GetStringByKey("LocalizedStrings.Loading"),
            localeManager.GetStringByKey("LocalizedStrings.CheckLauncherUpdate")));

        var isLauncherReleaseCurrent = await IsLauncherReleaseCurrentAsync(splashScreenManager.CancellationToken);
        logger?.LogInformation("Check launcher update time: {Time}", stopwatch.ElapsedMilliseconds);
        if (!isLauncherReleaseCurrent)
        {
            var pathLauncherUpdaterZip = Path.Combine(DirectoryStorage.CurrentDirectory, FileNameStorage.SBLauncherUpdaterZip);
            await updaterService.UpdaterAsync(UriStorage.LauncherApiUri, pathLauncherUpdaterZip, splashScreenManager.CancellationToken);

            var pathLauncherUpdater = Path.Combine(DirectoryStorage.CurrentDirectory, FileNameStorage.SBLauncherUpdater);
            var updater = Launcher.Launch(pathLauncherUpdater);
            updater?.Start();

            return;
        }

        splashScreenManager.UpdateInformation(new InformationMessage(
            localeManager.GetStringByKey("LocalizedStrings.Loading"),
            localeManager.GetStringByKey("LocalizedStrings.CheckGameUpdate")));
        launcherStorage.GitHubRelease = await gitStorageApiService.GetLastReleaseAsync(cancellationToken: splashScreenManager.CancellationToken);
        logger?.LogInformation("Check last release time: {Time}", stopwatch.ElapsedMilliseconds);

        launcherStorage.IsGameReleaseCurrent = await IsGameReleaseCurrentAsync(splashScreenManager.CancellationToken);
        launcherStorage.IsUserAuthorized = File.Exists(PathStorage.LauncherSetting);

        if (launcherStorage.IsGameReleaseCurrent)
        {
            splashScreenManager.UpdateInformation(new InformationMessage(
                localeManager.GetStringByKey("LocalizedStrings.Loading"),
                localeManager.GetStringByKey("LocalizedStrings.LoadLocalData")));

            await HandleGameReleaseCurrentAsync(splashScreenManager.CancellationToken);
        }
        else
        {

            await LoadRemoteContent(userManager.UserSettings.Locale, splashScreenManager.CancellationToken);
            await RemoteLoadWebResourcesAsync(cancellationToken: splashScreenManager.CancellationToken);
        }
    }

    private async Task HandleGameReleaseCurrentAsync(CancellationToken cancellationToken)
    {
        if (userManager is null)
        {
            throw new NullReferenceException("User manager object is null");
        }
        if (userManager.UserSettings is null)
        {
            throw new NullReferenceException("User settings object is null");
        }

        var contentNews = await LocaleLoadCacheAsync<LangNewsContent>(PathStorage.NewsCache, cancellationToken) ?? [];
        var isContentNews = contentNews.Any(x =>
            x.Locale is not null &&
            userManager.UserSettings.Locale is not null &&
            x.Locale.Key.Equals(userManager.UserSettings.Locale.Key));
        if (isContentNews)
        {
            launcherStorage.NewsContents = [.. contentNews];
        }
        else
        {
            await LoadRemoteContent(userManager.UserSettings.Locale, cancellationToken);
        }

        var contentRes = await LocaleLoadCacheAsync<WebResource>(PathStorage.WebResourcesCache, cancellationToken);
        if (contentRes is not null && contentRes.Count != 0)
        {
            launcherStorage.WebResources = [.. contentRes];
        }
        else
        {
            await RemoteLoadWebResourcesAsync(cancellationToken: cancellationToken);
        }
    }

    private async Task HandleOfflineInitializationAsync(CancellationToken cancellationToken = default)
    {
        if (userManager is null)
        {
            throw new NullReferenceException("User manager object is null");
        }
        if (userManager.UserSettings is null)
        {
            throw new NullReferenceException("User settings object is null");
        }

        if (launcherStorage.IsUserAuthorized)
        {
            launcherStorage.NewsContents = [.. LoadErrorNews(userManager.UserSettings.Locale) ?? []];
        }
        else
        {
            launcherStorage.NewsContents = [.. LoadErrorNews() ?? []];
        }

        launcherStorage.GitHubRelease = await FileDataHelper.LoadDataAsync<GitHubRelease>(PathStorage.CurrentRelease, cancellationToken);
    }

    private async Task LoadRemoteContent(Locale? locale, CancellationToken cancellationToken = default)
    {
        if (launcherStorage.IsUserAuthorized)
        {
            await RemoteLoadNewsAsync(locale, cancellationToken);
        }
        else
        {
            await RemoteLoadNewsAsync(cancellationToken: cancellationToken);
        }
    }

    private async Task<List<T>?> LocaleLoadCacheAsync<T>(string cachePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(cachePath))
            {
                return null;
            }

            return await FileDataHelper.LoadDataAsync<List<T>>(cachePath, cancellationToken);
        }
        catch (Exception ex)
        {
            logger?.LogError("{Message}", ex.Message);
            logger?.LogError("{StackTrace}", ex.StackTrace);
            throw;
        }
    }

    private List<LangNewsContent>? LoadErrorNews(Locale? locale = null)
    {
        // News in all languages
        var allNews = new List<LangNewsContent>();
        locale ??= launcherStorage.Locales[0];

        try
        {
            allNews.Add(new LangNewsContent(locale, [new NewsContent(
                localeManager.GetStringByKey("LocalizedStrings.ErrorTitle"),
                localeManager.GetStringByKey("LocalizedStrings.ErrorInternetDescription")
            )]));
        }
        catch (Exception ex)
        {
            logger?.LogError("{Message}", ex.Message);
            logger?.LogError("{StackTrace}", ex.StackTrace);
        }

        return allNews;
    }

    /// <summary>
    /// Checks the connection to github.com.
    /// </summary>
    /// <returns>True if the connection is established successfully, otherwise false.</returns>
    private async Task<bool> IsCheckGitHubConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync("https://github.com", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                logger?.LogInformation("Connection to github.com established");
                return true;
            }

            logger?.LogInformation("Failed to establish connection to github.com. Response code: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (HttpRequestException ex)
        {
            logger?.LogInformation("Failed to establish connection to github.com");

            logger?.LogError("{Message}", ex.Message);
            logger?.LogError("{StackTrace}", ex.StackTrace);
            return false;
        }
    }

    private async Task<bool> IsLauncherReleaseCurrentAsync(CancellationToken cancellationToken = default)
    {
        var tags = await gitStorageApiService.GetTagsAsync(UriStorage.LauncherApiUri, cancellationToken);
        if (tags is null)
        {
            return true;
        }

        var currentVersion = $"{ApplicationHelper.GetAppVersion()}";
        if (currentVersion[0] is not 'v')
        {
            currentVersion = currentVersion.Insert(0, "v");
        }

        var enumerable = tags.ToList();
        var countTag = enumerable.Count(x => x!.Name.Equals(currentVersion));
        if (countTag == 0)
        {
            // If there is no such release, we return true so that there is no looping
            return true;
        }

        var firstTag = enumerable.FirstOrDefault();
        return firstTag is null || firstTag.Name.Equals(currentVersion);
    }

    private async Task<bool> IsGameReleaseCurrentAsync(CancellationToken cancellationToken = default)
    {
        var gitStorageRelease = launcherStorage.GitHubRelease;

        if (File.Exists(PathStorage.CurrentRelease))
        {
            var releaseComparer = gitStorageRelease is not null && await releaseComparerService.IsComparerAsync(gitStorageRelease, cancellationToken);
            if (!releaseComparer)
            {
                await FileSystemHelper.WriteReleaseAsync(gitStorageRelease, PathStorage.CurrentRelease, cancellationToken);
                logger?.LogInformation("The releases don't match. Update required!");
                return false;
            }

            logger?.LogInformation("The releases are the same. No update required.");
            return true;
        }

        await FileSystemHelper.WriteReleaseAsync(gitStorageRelease, PathStorage.CurrentRelease, cancellationToken);
        logger?.LogInformation("The release configuration has not been previously saved");
        return false;
    }

    private async Task RemoteLoadWebResourcesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var contents = await gitStorageApiService
                .DownloadJsonAsync<IEnumerable<WebResource>>(FileNameStorage.WebResources, UriStorage.BelarusApiUri, cancellationToken);
            if (contents is not null)
            {
                var webResources = contents.ToList();
                await FileSystemHelper.WriteReleaseAsync(webResources, Path.Combine(DirectoryStorage.LauncherCache, FileNameStorage.WebResources), cancellationToken);
                // ReSharper disable once ArrangeObjectCreationWhenTypeNotEvident
                launcherStorage.WebResources = [.. webResources];
            }
        }
        catch (Exception ex)
        {
            logger?.LogError("{Message}", ex.Message);
            logger?.LogError("{StackTrace}", ex.StackTrace);
            throw;
        }
    }

    private async Task RemoteLoadNewsAsync(Locale? locale = null, CancellationToken cancellationToken = default)
    {
        // News in all languages
        var allNews = new List<LangNewsContent>();

        try
        {
            if (locale is null)
            {
                foreach (var lang in launcherStorage.Locales)
                {
                    var news = await gitStorageApiService
                        .DownloadJsonAsync<IEnumerable<NewsContent>>($"news_content_{lang.Key}.json", UriStorage.BelarusApiUri, cancellationToken);
                    AddNews(lang, allNews, news);
                }
            }
            else
            {
                var news = await gitStorageApiService
                    .DownloadJsonAsync<IEnumerable<NewsContent>>($"news_content_{locale.Key}.json", UriStorage.BelarusApiUri, cancellationToken);
                AddNews(locale, allNews, news);
            }
        }
        catch (Exception ex)
        {
            logger?.LogError("{Message}", ex.Message);
            logger?.LogError("{StackTrace}", ex.StackTrace);
        }

        await FileSystemHelper.WriteReleaseAsync(allNews, Path.Combine(DirectoryStorage.LauncherCache, "News.json"), cancellationToken);
        launcherStorage.NewsContents = [.. allNews];
    }

    private void AddNews(Locale? locale, List<LangNewsContent> allNews, IEnumerable<NewsContent>? news)
    {
        if (locale is null)
        {
            logger?.LogError("Failure to load locale!");
            return;
        }

        if (news is not null)
        {
            allNews.Add(new LangNewsContent(locale, news));
        }
        else
        {
            logger?.LogError("Failure to load news in {locale}", locale.Title);
        }
    }
}
