using UniRx;
using System;
using UnityEngine;

public class EventBus {
    private static Subject<System.Object> subject = new Subject<System.Object>();

    public static void Publish<T>(T x)
    {
        subject.OnNext(x);        
    }

    public static IObservable<T> Receive<T>() where T : class
    {
        return subject
            .Where(x => x is T)
            .Select(x => x as T)
            .ObserveOnMainThread();
    }

    public static IObservable<T> ReceiveAllAs<T>() where T : class
    {
        return subject
            .Select(x => x as T)
            .ObserveOnMainThread();
    }

    public static void Subscribe<T>(MonoBehaviour b, Action<T> f) where T : class
    {
        Receive<T>().Subscribe(f).AddTo(b);
    }

    public static void Subscribe<T>(MonoBehaviour b, Action f) where T : class
    {
        Subscribe<T>(b, _ => f());
    }

}