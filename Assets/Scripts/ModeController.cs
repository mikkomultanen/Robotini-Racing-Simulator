using UnityEngine;
using System.Collections;

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
            Debug.Log("Environment " + Application.platform + " detected. Using Development mode.");
            Mode = SimulatorMode.Development;
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
