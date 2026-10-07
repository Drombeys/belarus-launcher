namespace Belarus.Launcher;

public static class ObservableExtensions
{
    public static IDisposable Subscribe<T>(this IObservable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Subscribe(new AnonymousObserver<T>(_ => { }));
    }

    public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> onNext)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(onNext);
        return source.Subscribe(new AnonymousObserver<T>(onNext));
    }

    private sealed class AnonymousObserver<T>(Action<T> onNext) : IObserver<T>
    {
        public void OnNext(T value) => onNext(value);
        public void OnError(Exception error) { }
        public void OnCompleted() { }
    }
}
