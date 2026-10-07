using System;

namespace Belarus.Launcher;

public static class ObservableExtensions
{
    public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> onNext)
        where T : notnull
    {
        return ReactiveUI.Primitives.SubscribeExtensions.Subscribe(source, onNext);
    }
}
