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
using UniRx;

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
            //StartCoroutine(FetchRaceLogOverHTTP("http://localhost:8000/race.log"));
#endif
        }

        /*
        // To simulate web player user clicking on races
        Observables.Delay(TimeSpan.FromSeconds(1)).Subscribe(_ => {
            PlayUrl("https://robotini-race-results.s3.eu-west-1.amazonaws.com/race_results/2021-camp1/CI-1616499914/race.log?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=AKIASOYCV2RQYKFUIQ47%2F20210324%2Feu-west-1%2Fs3%2Faws4_request&X-Amz-Date=20210324T074833Z&X-Amz-Expires=604800&X-Amz-Signature=efbe162ee43f1345e57bdfa4895a1e0c1b0a705beb84d609b14503dacc3bbfc9&X-Amz-SignedHeaders=host");
        });
        Observables.Delay(TimeSpan.FromSeconds(15)).Subscribe(_ => {
            PlayUrl("https://robotini-race-results.s3.eu-west-1.amazonaws.com/race_results/2021-camp1/1616492743/race.log?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=AKIASOYCV2RQYKFUIQ47%2F20210324%2Feu-west-1%2Fs3%2Faws4_request&X-Amz-Date=20210324T074833Z&X-Amz-Expires=604800&X-Amz-Signature=8ffb55eecddfbb510c019a46f8aac1a8ae20286d1e10917f1c11ca28f23a8401&X-Amz-SignedHeaders=host");
        });
        */

    }

    // Called from the page javascript!
    public void PlayUrl(string url) 
    {
        Debug.Log("Loading race: " + url);
        StartCoroutine(FetchRaceLogOverHTTP(url));
    }

    private void Update()
    {
        if (events == null) return;
        if (index >= events.Length) return;
        while (pollForNext()); // Apply all events due
        position += Time.deltaTime;
    }

    private bool pollForNext() {
        if (index >= events.Length) return false;
        GameEvent nextEvent = events[index];
        if (nextEvent is SecondsRemaining) {
            // Skip waiting for these
            index++;
            Debug.Log("Skipping " + nextEvent);
            return true;
        }
        position = Math.Max(position, nextEvent.timestamp - maxWait);
        var diff = nextEvent.timestamp - position;
        if (diff > 0) return false; // Wait until proper time
        index++;
        ApplyEvent(nextEvent);
        return true;
    }

    

    IEnumerator FetchRaceLogOverHTTP(string raceLogUrl)
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

        UnityWebRequest www = UnityWebRequest.Get(raceLogUrl);
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
        }
        else
        {
            Debug.Log("Loaded race");
            // Show results as text
            string text = www.downloadHandler.text;

            string[] lines = text.Split('\n');
            this.events = lines
                .Where(line => line.Trim().Length > 0)
                .Select(line => GameEvent.FromJson(line))
                .ToArray();
            this.position = 0;
            this.index = 0;
            Debug.Log("Race contains " + this.events.Length + " events");
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
        this.position = 0;
        this.index = 0;
        Debug.Log("Race contains " + this.events.Length + " events");
    }
}