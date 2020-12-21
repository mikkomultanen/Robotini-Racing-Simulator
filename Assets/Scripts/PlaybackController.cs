using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

public class PlaybackController : MonoBehaviour
{
    private void OnEnable()
    {
        if (ModeController.Mode == SimulatorMode.Playback)
        {
            Debug.Log("Initializing playback");
            StartCoroutine(GetRaceLog());
        }
    }

    IEnumerator GetRaceLog()
    {
        UnityWebRequest www = UnityWebRequest.Get("http://localhost:8000/race-capture.json");
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
            GameStatus[] states = lines.Select(line => JsonUtility.FromJson<GameStatus>(line)).ToArray();
            Debug.Log(JsonUtility.ToJson(states[0]));

            GameStatus previous = null;
            foreach (GameStatus status in states)
            {
                if (previous != null)
                {
                    float delay = status.timestamp - previous.timestamp;
                    Debug.Log("Waiting " + delay);
                    yield return new WaitForSeconds(delay);
                }
                // TODO: now apply the status!
                previous = status;
            }
        }
    }
}
