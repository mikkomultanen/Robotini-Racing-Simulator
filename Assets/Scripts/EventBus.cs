using UniRx;
using System;
using UnityEngine;

public class EventBus {
    public static void Publish<T>(T x)
    {
        MessageBroker.Default.Publish(x);
    }

    public static IObservable<T> Receive<T>()
    {
        return MessageBroker.Default.Receive<T>().ObserveOnMainThread();
    }

    public static void Subscribe<T>(MonoBehaviour b, Action<T> f)
    {
        Receive<T>().Subscribe(f).AddTo(b);
    }
}