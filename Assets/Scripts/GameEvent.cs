using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using UnityEngine;


public class GameEvents
{
    
}

public enum GameEventType { lap, gameStatus }

[Serializable]
public abstract class GameEvent
{
    public GameEvent(GameEventType type, float timestamp)
    {
        this.timestamp = timestamp;
        this.type = type;
    }

    public GameEventType type;
    public float timestamp;
}

[Serializable]
public class CarStatus
{
    public CarStatus(Vector3 position, Vector3 velocity, Quaternion rotation)
    {
        this.position = position;
        this.velocity = velocity;
        this.rotation = rotation;
    }
    public Vector3 position;
    public Vector3 velocity;
    public Quaternion rotation;
}

[Serializable]
public class GameStatus: GameEvent
{
    public GameStatus(CarStatus[] cars, float timestamp): base(GameEventType.gameStatus, timestamp)
    {
        this.cars = cars;
    }
    public CarStatus[] cars;
}