using System;
using UnityEngine;

[Serializable]
public class GameEvent
{
    static private DateTime startTime = System.DateTime.Now;
    public static float NewTimeStamp()
    {
        return TimeDiff(System.DateTime.Now, startTime);
    }
    public static float TimeDiff(DateTime now, DateTime before) {
        return (float)((now - before).TotalSeconds);
    }
    [HideInInspector]
    public string type; // for deserialising as the correct type

    public GameEvent()
    {
        var type = this.GetType();
        this.type = type.FullName;
    }
    public float timestamp = NewTimeStamp();

    public static GameEvent FromJson(string json)
    {        
        var typeName = JsonUtility.FromJson<GameEvent>(json).type;
        // deserialise first as plain GameEvent to get the instance Type
        var type = System.Type.GetType(typeName);
        // deserialise as the correct type
        return (GameEvent)JsonUtility.FromJson(json, type);
    }
}

[Serializable]
public class CarStatus
{
    public CarStatus(string name, Vector3 position, Vector3 velocity, Quaternion rotation)
    {
        this.name = name;
        this.position = position;
        this.velocity = velocity;
        this.rotation = rotation;
    }
    public string name;
    public Vector3 position;
    public Vector3 velocity;
    public Quaternion rotation;
}

public class GameStatus: GameEvent
{
    public GameStatus(CarStatus[] cars)
    {
        this.cars = cars;
    }
    public CarStatus[] cars;
}

[Serializable]
public class CarInfo
{
    public string teamId;
    public string name;
    public string color;

    [NonSerialized]
    private Color _color = Color.clear;

    public CarInfo(string teamId, string name, string color)
    {
        this.teamId = teamId;
        this.name = name;
        this.color = color;
    }
    public LapCompleted ToLap()
    {
        return new LapCompleted(this, 0, float.NaN, float.NaN, 0, false);
    }
    public Color GetColor()
    {
        if (_color == Color.clear) {
            ColorUtility.TryParseHtmlString(color, out _color);
            _color.a = 1f;
        }
        return _color;
    }
}

public abstract class CarEvent: GameEvent {
    public CarInfo car;

    public CarEvent(CarInfo car)
    {
        this.car = car;
    }
}

[Serializable]
public class LapCompleted: CarEvent
{
    public int lapCount;
    public float lastLap;
    public float bestLap;
    public float totalTime;
    public bool dnf;

    public LapCompleted(CarInfo car, int lapCount, float lastLap, float bestLap, float totalTime, bool dnf): base(car)
    {
        this.car = car;
        this.lapCount = lapCount;
        this.lastLap = lastLap;
        this.bestLap = bestLap;
        this.totalTime = totalTime;
        this.dnf = dnf;
    }
}

public interface PhaseChange { }

public class CarFinished: CarEvent
{
    public CarFinished(CarInfo car): base(car) { }    
}

public class RaceWon: CarEvent
{
    public RaceWon(CarInfo car): base(car) { }  
}

public class RaceFinished: GameEvent, PhaseChange
{
    public LapCompleted[] standings;

    public RaceFinished(LapCompleted[] standings)
    {
        this.standings = standings;
    }
}

public class CarCrashed: CarEvent
{
    public CarCrashed(CarInfo car): base(car) { }
}

public class CarBumped: CarEvent
{
    public CarInfo other;
    public CarBumped(CarInfo car, CarInfo other): base(car)
    {
        this.other = other;
    }
}

public class CarReturnedToTrack: CarEvent {
    public CarReturnedToTrack(CarInfo car): base(car) { }
}

public class CurrentStandings: GameEvent {
    public LapCompleted[] standings;
    public bool qualifying;

    public CurrentStandings(LapCompleted[] standings, bool qualifying) {
        this.standings = standings;
        this.qualifying = qualifying;
    }
}

public class RaceLobbyInit: GameEvent, PhaseChange
{
    public RaceParameters raceParameters;

    public RaceLobbyInit(RaceParameters raceParameters)
    {
        this.raceParameters = raceParameters;
    }
}

public class SecondsRemaining : GameEvent
{
    public int secondsRemaining;

    public SecondsRemaining(int secondsRemaining)
    {
        this.secondsRemaining = secondsRemaining;
    }
}

public class QualifyingStart: GameEvent, PhaseChange
{
    public CarInfo[] cars;
    public QualifyingStart(CarInfo[] cars)
    {
        this.cars = cars;
    }
}

public class QualifyingResults : GameEvent
{
    public LapCompleted[] results;
    public QualifyingResults(LapCompleted[] results)
    {
        this.results = results;
    }
}

public class StartingGridInit : GameEvent, PhaseChange
{
    public CarInfo[] cars;

    public StartingGridInit(CarInfo[] cars) {
        this.cars = cars;
    }
}

public class RaceStart : GameEvent, PhaseChange
{

}

public class FreePracticeStart : GameEvent, PhaseChange
{

}

public class CarRemoved : CarEvent
{
    public CarRemoved(CarInfo car): base(car) {}
}

public class CarDisconnected : CarEvent
{
    public CarDisconnected(CarInfo car): base(car) {}
}

public class CarConnected : CarEvent
{
    public CarSocket socket; // <- non serializable object, not included in JSON
    public CarConnected(CarInfo car, CarSocket socket): base(car)
    {
        this.socket = socket;
    }
}

public abstract class UICommand: GameEvent {
}

public class MotorsToggle: UICommand
{
}

public class ProceedToNextPhase: UICommand
{
}