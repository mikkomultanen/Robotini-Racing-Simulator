using UnityEngine;
using TMPro;
using System;
using UniRx;

public class BigTextDisplay : MonoBehaviour
{
    void Update()
    {
        var events = EventBus.Receive<GameEvent>().Select(e =>
        {
            if (e is RaceLobbyInit)
            {
                return keepShowing("Race lobby, waiting for players...");
            }
            else if (e is CarConnected)
            {
                return showForAWhile("Connected: " + (e as CarConnected).car.name);
            }
            else if (e is CarDisconnected)
            {
                return showForAWhile("Disconnected: " + (e as CarDisconnected).car.name);
            }
            else if (e is QualifyingStart)
            {
                return keepShowing("Qualifying");
            }
            else if (e is StartingGridInit)
            {
                return keepShowing("Ready to race");
            }
            else if (e is RaceStart)
            {
                return showForAWhile("GO!");
            }
            else if (e is FreePracticeStart)
            {
                return keepShowing("Free practice");
            }
            return Observable.Never<string>();
        }).Switch();
        events.Subscribe(showBigText);
    }

    IObservable<string> showForAWhile(string text)
    {
        return Observable.Return(text).Concat(Observable.Delay(Observable.Return(""), TimeSpan.FromSeconds(1)));
    }

    IObservable<string> keepShowing(string text)
    {
        return Observable.Return(text);
    }

    void showBigText(string text)
    {
        gameObject.GetComponent<TextMeshProUGUI>().text = text;
    }
}
