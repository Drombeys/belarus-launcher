using System;

namespace Belarus.Launcher;

public static class ObservableExtensions
{
    public static IDisposable Subscribe<T>(this IObservable<T> source)
    {
        return ReactiveUI.Primitives.SubscribeExtensions.Subscribe(source);
    }

    public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> onNext)
    {
        return ReactiveUI.Primitives.SubscribeExtensions.Subscribe(source, onNext);
    }
}

