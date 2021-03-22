using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;

public class PlaybackController : RemoteEventPlayer
{
    private float position = 0;
    private int index = 0;
    private GameEvent[] events;

    static float maxWait = 1; // 1 sec

    private void OnEnable()
    {
        Debug.Log("Playback init");
        if (ModeController.Mode == SimulatorMode.Playback)
        {
#if UNITY_EDITOR
            GetRaceLog(RaceParameters.readRaceParameters().raceLogFile);
#else
            StartCoroutine(GetRaceLog());
#endif
        }        
    }

    private void FixedUpdate()
    {
        if (events == null) return;
        if (index >= events.Length) return;
        while (pollForNext()); // Apply all events due
        position += Time.deltaTime;
    }

    private bool pollForNext() {
        if (index >= events.Length) return false;
        GameEvent nextEvent = events[index];
        position = Math.Max(position, nextEvent.timestamp - maxWait);
        var diff = nextEvent.timestamp - position;
        if (diff > 0) return false;
        index++;
        ApplyEvent(nextEvent);
        return true;
    }

    

    IEnumerator GetRaceLog()
    {
        Debug.Log("Initializing playback");

        // TODO: extract parameters and use raceId parameter to load /api/v1/race/:raceId 
/*
        var uri = new Uri(Application.absoluteURL);
        var ps = System.Web.HttpUtility.ParseQueryString(uri.Query);
        string root = Application.absoluteURL.Replace(uri.PathAndQuery, ""); // TODO: fails if there's no pathandquery :)
        Debug.Log("Root: " + root);
         Debug.Log("Params: " + ps.Keys.ToString());
         */

        UnityWebRequest www = UnityWebRequest.Get("http://localhost:8000/race.log");
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
        }
        else
        {
            // Show results as text
            string text = www.downloadHandler.text;

            string[] lines = text.Split('\n');
            this.events = lines
                .Where(line => line.Trim().Length > 0)
                .Select(line => GameEvent.FromJson(line))
                .ToArray();           
        }
    }

    void GetRaceLog(string fileName) {
        var reader = new StreamReader(fileName);
        List<string> lines = new List<string>();
        string line = null;
        while ((line = reader.ReadLine()) != null) {
            lines.Add(line);
        }
        this.events = lines
            .Where(line => line.Trim().Length > 0)
            .Select(line => GameEvent.FromJson(line))
            .ToArray();
    }
}