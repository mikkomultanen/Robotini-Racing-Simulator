using UnityEngine;
using System.Collections;
using UniRx;

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
            Mode = SimulatorMode.Race;
        }
    }

    // Use this for initialization
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }
}
