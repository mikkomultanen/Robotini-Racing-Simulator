using UniRx;
using System;
using UnityEngine;
public class Observables
{
    public static IObservable<Unit> Delay(TimeSpan delay)
    {
        return Observable.Delay(Observable.Return(new Unit()), delay).ObserveOnMainThread();
    }

    public static IObservable<SecondsRemaining> Countdown(int seconds)
    {
        return Observable
            .Interval(TimeSpan.FromSeconds(1))
            .Merge(Observable.Return(0L))
            .Scan(seconds, (acc, _) => acc - 1)
            .TakeWhile(v => v >= 0)
            .Select(s => new SecondsRemaining(s))
            .ObserveOnMainThread();
    }
}