using System;
using UnityEngine;
using System.IO;

[Serializable]
public class RaceParameters : GameEvent
{
    public int lapCount = 5;
    public int autoStartQualifyingSeconds = 30;
    public int qualifyingDurationSeconds = 30;
    public int autoStartRaceSeconds = 10;
    public int raceTimeoutSeconds = 300;
    public int raceTimeoutAfterWinnerSeconds = 60;
    public CarInfo[] cars;
    public string track = "oval";
    public string visibility = "team";

    public string mode = "development";

    public string raceLogFile = "race.log";
    public string raceResultFile = "race-result.json";
    public string memoryLogFile = "memory.log";
    public string botCommandLogFile = "";

    public static RaceParameters readRaceParameters()
    {
#if UNITY_WEBGL
        Debug.Log("WebGL -> No default track");
        var raceParams = new RaceParameters();
        raceParams.track = null;
        return raceParams;
#else
        try
        {
            Debug.Log("Reading RaceParameters.json");
            var reader = new StreamReader("RaceParameters.json");
            var p = JsonUtility.FromJson<RaceParameters>(reader.ReadToEnd());
            return p;
        }
        catch (FileNotFoundException)
        {
            Debug.Log("RaceParameters.json not found");
            return new RaceParameters();
        }
#endif
    }
}