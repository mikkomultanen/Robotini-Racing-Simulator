using UnityEngine;
using System;

public enum SimulatorMode
{        
    Development,
    Race,
    Playback
}

public class ModeController : MonoBehaviour
{
    public static SimulatorMode Mode;

    static ModeController()
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            Debug.Log("WebGL player detected. Playback mode it is.");
            Mode = SimulatorMode.Playback;
        }
        else
        {
            Debug.Log("Environment " + Application.platform + " detected.");
            string mode = RaceParameters.readRaceParameters().mode;
            switch (mode) {
                case "development":
                    Mode = SimulatorMode.Development; break;
                case "race":
                    Mode = SimulatorMode.Race; break;
                case "playback":
                    Mode = SimulatorMode.Playback; break;
                default:
                    throw new Exception("Illegal mode " + mode + ", expecting race or development");
            }
        }
    }

    // Use this for initialization
    void Start()
    {
        Application.runInBackground = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!Application.runInBackground) {
             Application.runInBackground = true;
             Debug.Log("Re-Setting Application.runInBackground to TRUE at: " + Time.time);
        }
    }
}
