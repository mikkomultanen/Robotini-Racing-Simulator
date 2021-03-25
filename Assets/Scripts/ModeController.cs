using UnityEngine;
using System;

public enum SimulatorMode
{        
    Development,
    Race,
    Playback,
    RemoteControl
}

public class ModeController : MonoBehaviour
{
    private static bool dirty = true;
    private static SimulatorMode _mode;
    public static SimulatorMode Mode {
        get {
            if (dirty) {
                dirty = false;
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    Debug.Log("WebGL player detected. Playback mode it is.");
                    _mode = SimulatorMode.Playback;
                }
                else
                {
                    Debug.Log("Environment " + Application.platform + " detected.");
                    string mode = RaceParameters.readRaceParameters().mode;
                    switch (mode)
                    {
                        case "development":
                            _mode = SimulatorMode.Development; break;
                        case "race":
                            _mode = SimulatorMode.Race; break;
                        case "playback":
                            _mode = SimulatorMode.Playback; break;
                        case "remote":
                            _mode = SimulatorMode.RemoteControl; break;
                        default:
                            throw new Exception("Illegal mode " + mode + ", expecting race or development");
                    }
                }
            }
            return _mode;
        }
    }

    private void OnDisable()
    {
        dirty = true;
    }

    // Use this for initialization
    void Start()
    {
        Debug.Log("Setting Application.runInBackground to true");
        Application.runInBackground = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!Application.runInBackground) {
             Application.runInBackground = true;
             Debug.Log("Re-Setting Application.runInBackground to true");
        }
    }
}
