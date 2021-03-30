using System.Collections.Concurrent;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using UnityEngine;
using UniRx;
using System.Collections.Generic;

public class SpectatorSocket : MonoBehaviour
{
    private GameStatus latestGameStatus;
    private bool raceEnded = false;
    private Socket listener = null;
    private readonly ManualResetEvent allDone = new ManualResetEvent(false);
    private DateTime startTime = System.DateTime.Now;
    // Collect here because the cars in RaceController can only be accessed in main thread
    private Dictionary<string, CarInfo> carInfos = new Dictionary<string, CarInfo>();


    private void Update()
    {
        if (raceEnded) return;
        CarController[] cars = FindObjectsOfType<CarController>();
        CarStatus[] statuses = cars.Select(c => new CarStatus(c.name, c.rigidBody.position, c.rigidBody.velocity, c.rigidBody.rotation)).ToArray();

        var newGameStatus = new GameStatus(statuses);


        if (latestGameStatus == null || (latestGameStatus.timestamp != newGameStatus.timestamp && (latestGameStatus.cars.Length > 0 || newGameStatus.cars.Length > 0))) {
            latestGameStatus = newGameStatus;
            EventBus.Publish(latestGameStatus);
        }
    }

    private void OnEnable()
    {
        switch (ModeController.Mode) {
            case SimulatorMode.Playback:
            case SimulatorMode.RemoteControl:
                break;
            default:
                Debug.Log("Initializing spectator socket");
                StartListening();
                RaceParameters raceParams = RaceParameters.readRaceParameters();
                if (raceParams.raceLogFile != null) {
                    Debug.Log("Writing race log to " + raceParams.raceLogFile);
                    var stream = new BinaryWriter(File.Open(raceParams.raceLogFile, FileMode.Create));
                
                    Spectate(b => {stream.Write(b); stream.Flush(); }, () => stream.Close(), new CarInfo[] { });
                }
                EventBus.Subscribe<CarAdded>(this, e => {
                    CarInfo car = ((CarAdded)e).car;
                    carInfos.Add(car.name, car);
                });

                EventBus.Subscribe<RaceFinished>(this, e => {
                    Observables.Delay(TimeSpan.FromSeconds(1)).Subscribe(_ => {
                        Debug.Log("Race finished, stopping updates");
                        raceEnded = true;
                        EventBus.Publish(new GameStatus(new CarStatus[0]));
                        var stream = new BinaryWriter(File.Open(raceParams.raceResultFile, FileMode.Create));

                        stream.Write(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(e, true)));
                        stream.Close();
                    });

                    Observables.Delay(TimeSpan.FromSeconds(3)).Subscribe(_ => {
                        Debug.Log("Quitting application");
                        Application.Quit();
                    });
                });
                break;
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

        var listenerThread = new Thread(() =>
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
        });
        listenerThread.Name = "SpectatorSocket Listener";
        listenerThread.IsBackground = true;
        listenerThread.Start();
    }

    private void AcceptCallback(IAsyncResult ar)
    {
        allDone.Set();

        Socket socket = listener.EndAccept(ar);
        socket.SendBufferSize = 20000;
        socket.NoDelay = true;        

        Debug.Log("Spectator connected.");

        Action close = () => socket.Close();

        CarInfo[] initialCars = carInfos.Values.ToArray();

        //Debug.Log("Initial cars " + string.Join(",", initialCars.Select(c => c.name).ToArray()));

        var receiverThread = new Thread(() => {
            var stream = new NetworkStream(socket);
            var reader = new StreamReader(stream);
            try
            {
                while (true)
                {
                    var line = reader.ReadLine();
                    var command = GameEvent.FromJson(line);
                    if (!(command is UICommand)) {
                        throw new Exception("Received unexpected command from Spectator: " + command);
                    }
                    EventBus.Publish(command);
                }

            }
            catch (Exception e)
            {
                Debug.Log("Spectator socket read failed:" + e.ToString());
                close();
            }
        });
        receiverThread.Name = "SpectatorSocket Receiver";
        receiverThread.IsBackground = true;
        receiverThread.Start();

        Spectate(b => socket.Send(b), close, initialCars);

    }

    private void EndListening()
    {
        if (listener != null)
        {
            listener.Dispose();
            listener = null;
        }
    }

    void Spectate(Action<byte[]> sendBytes, Action close, CarInfo[] initialCars)
    {
        Action<GameEvent> send = (GameEvent e) => {
            string jsonString = JsonUtility.ToJson(e);
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(jsonString + "\n");
            //Debug.Log("Writing event " + e.type + " JSON " + jsonString + " as " + bytes.Length + " bytes");
            sendBytes(bytes);
        };

        var senderThread = new Thread(() =>
        {
            var eventQueue = new ConcurrentQueue<GameEvent>();
            var subscription = EventBus.Receive<GameEvent>().Subscribe(e => {
                eventQueue.Enqueue(e);
            });
            GameEvent gameEvent = null;
            eventQueue.Enqueue(RaceParameters.readRaceParameters());
            foreach (var car in initialCars) {
                eventQueue.Enqueue(new CarAdded(car));
            }
            try
            {
                while (listener != null)
                {
                    if (eventQueue.TryDequeue(out gameEvent))
                    {
                        //Debug.Log("Sending event " + gameEvent.type);                        
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

        });
        senderThread.Name = "SpectatorSocket Sender";
        senderThread.IsBackground = true;
        senderThread.Start();
    }
}