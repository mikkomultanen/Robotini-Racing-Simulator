using System;
using UnityEngine;
using System.IO;

[Serializable]
public class RaceParameters
{
    public int lapCount = 5;
    public int autoStartQualifyingSeconds = 30;
    public int qualifyingDurationSeconds = 30;
    public int autoStartRaceSeconds = 10;
    public int raceTimeoutSeconds = 300;
    public CarInfo[] cars;
    public SplineMesh.SplineNode[] trackNodes;

    public string mode = "development";

    public string raceLogFile = "race.log";

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