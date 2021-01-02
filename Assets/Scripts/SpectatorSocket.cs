using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using UnityEngine;
using UniRx;

public class SpectatorSocket : MonoBehaviour
{
    private GameStatus latestGameStatus;
    private Socket listener = null;
    private readonly ManualResetEvent allDone = new ManualResetEvent(false);
    private DateTime startTime = System.DateTime.Now;    

    private void Update()
    {
        CarController[] cars = FindObjectsOfType<CarController>();
        CarStatus[] statuses = cars.Select(c => new CarStatus(c.name, c.rigidBody.position, c.rigidBody.velocity, c.rigidBody.rotation)).ToArray();
        latestGameStatus = new GameStatus(statuses);
    }

    private void OnEnable()
    {
        if (ModeController.Mode != SimulatorMode.Playback)
        {
            Debug.Log("Initializing spectator socket");
            StartListening();
            RaceParameters raceParams = RaceParameters.readRaceParameters();
            if (raceParams.raceLogFile != null) {
                Debug.Log("Writing race log to " + raceParams.raceLogFile);
                var stream = new BinaryWriter(File.Open(raceParams.raceLogFile, FileMode.Create));
                
                Spectate(b => stream.Write(b), () => stream.Close());
            }
        }
        else
        {
            Debug.Log("Skipping spectator socket");
        }
    }

    private void OnDisable()
    {
        EndListening();
    }

    private void StartListening()
    {
        if (listener != null) return;
        IPAddress ipAddress = IPAddress.Any;
        int port = 11001;
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

        // Create a TCP/IP socket.  
        listener = new Socket(ipAddress.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);

        listener.Bind(localEndPoint);
        listener.Listen(100);

        Debug.Log("Starting spectator thread, listening on " + port);

        new Thread(() =>
        {
            while (listener != null)
            {
                // Set the event to nonsignaled state.  
                allDone.Reset();

                // Start an asynchronous socket to listen for connections.  
                Debug.Log("Waiting for a spectator connection...");
                listener.BeginAccept(
                    new AsyncCallback(AcceptCallback),
                    listener);

                // Wait until a connection is made before continuing.  
                allDone.WaitOne();
            }
        }).Start();


    }

    private void AcceptCallback(IAsyncResult ar)
    {
        allDone.Set();

        Socket socket = listener.EndAccept(ar);
        socket.SendBufferSize = 20000;
        socket.NoDelay = true;        

        Debug.Log("Spectator connected.");

        Spectate(b => socket.Send(b), () => socket.Close());

    }

    private void EndListening()
    {
        if (listener != null)
        {
            listener.Dispose();
            listener = null;
        }
    }

    void Spectate(Action<byte[]> sendBytes, Action close)
    {
        Action<GameEvent> send = (GameEvent e) => {
            string jsonString = JsonUtility.ToJson(e);
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(jsonString + "\n");
            //Debug.Log("Writing " + jsonString + " as " + bytes.Length + " bytes");
            sendBytes(bytes);
        };

        new Thread(() =>
        {
            var eventQueue = new ConcurrentQueue<GameEvent>();
            var subscription = EventBus.Receive<GameEvent>().Subscribe(e => {
                eventQueue.Enqueue(e);
            });
            GameStatus myGameStatus = null;
            GameEvent gameEvent = null;

            try
            {
                while (listener != null)
                {                   
                    if (latestGameStatus != null)
                    {
                        if (myGameStatus == null || (myGameStatus.timestamp != latestGameStatus.timestamp && (myGameStatus.cars.Length > 0 || latestGameStatus.cars.Length > 0))) {
                            myGameStatus = latestGameStatus;
                            send(latestGameStatus);
                        }
                    }
                    if (eventQueue.TryDequeue(out gameEvent))
                    {
                        Debug.Log("Sending event " + gameEvent.type);
                        send(gameEvent);
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("Spectator send failed:" + e.ToString() + " Closing socket");
                close();
                subscription.Dispose();
                return;
            }

        }).Start();
    }
}