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
    public CarInfo[] cars;
    public string track;

    public string mode = "development";

    public string raceLogFile = "race.log";
    public string raceResultFile = "race-result.json";

    public static RaceParameters readRaceParameters()
    {
        try
        {
            var reader = new StreamReader("RaceParameters.json");
            var p = JsonUtility.FromJson<RaceParameters>(reader.ReadToEnd());
            return p;
        }
        catch (FileNotFoundException)
        {
            return new RaceParameters();
        }
    }
}