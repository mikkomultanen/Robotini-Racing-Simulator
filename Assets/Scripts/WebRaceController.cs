using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using UniRx;

public class WebRaceController : MonoBehaviour
{
    public RaceController raceController;
    private readonly Dictionary<string, WebCarSocket> cars = new Dictionary<string, WebCarSocket>();

#if UNITY_WEBGL
    [DllImport("__Internal")]
    private static extern void SendMessageToWebAsJSON(string str);
#endif

    public void SendToWeb(GameEvent msg) {
        string jsonString = JsonUtility.ToJson(msg);        
#if UNITY_EDITOR
        Debug.Log("Would send to web: " + jsonString);
#elif UNITY_WEBGL
        SendMessageToWebAsJSON(jsonString);
#else
        Debug.Log("Would send to web: " + jsonString);
#endif

    }

    private void Start()
    {
        raceController = FindObjectOfType<RaceController>();
        // Uncomment to simulate what happens with a web bot
        //SimulateWebClient();

#if UNITY_WEBGL
        EventBus.Subscribe<LapCompleted>(this, SendToWeb);
#endif

    }

    void SendFromWeb(string msgJson) {
        var e = GameEvent.FromJson(msgJson);
        if (e is CarLogin l) {
            cars.Add(l.name, new WebCarSocket(l, this));
            // Just send something for now
            SendToWeb(RaceParameters.readRaceParameters());
        } else if (e is WebCarCommand c) {
            var name = c.carName;
            var socket = cars[name];
            socket.EnqueueCommand(c.command);
        } else if (e is CarDisconnected d) {
            var name = d.car.name;
            var socket = cars[name];
            socket.Close();
        } else if (e is RaceParameters) {
            FindObjectOfType<TrackController>().LoadTrack(((RaceParameters)e).track);
        } else {
            EventBus.Publish(e);
        }        
    }

    void StartFreePractice()
    {
        FindObjectOfType<RaceController>().StartFreePractice();
    }

    void SimulateWebClient() {
        Observables.Delay(TimeSpan.FromSeconds(1)).Subscribe(_ => {
            StartFreePractice();
            SendFromWeb("{\"type\":\"CarLogin\",\"teamId\":\"j\u00E4s\u00E4\",\"name\":\"J\u00E4s\u00E4Bot\",\"color\":\"#FFFF00\",\"imageWidth\":8}");
        });
    }
}
