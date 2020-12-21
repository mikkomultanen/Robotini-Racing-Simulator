using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class RaceController : MonoBehaviour
{
    public GameObject carPrefab;
    [HideInInspector]
    public bool motorsEnabled = true;

    private SplineMesh.Spline track;
    private readonly ManualResetEvent allDone = new ManualResetEvent(false);
    private readonly ConcurrentQueue<Socket> clientSocketQueue = new ConcurrentQueue<Socket>();
    private Socket listener = null;
    private int index = 0;

    private void OnEnable()
    {
        track = FindObjectOfType<SplineMesh.Spline>();
        StartListening();
    }

    private void OnDisable()
    {
        EndListening();
    }

    private void Update()
    {
        Socket socket;
        while (clientSocketQueue.TryDequeue(out socket))
        {
            Debug.Log("Client connected " + socket.RemoteEndPoint);
            var curveSample = track.GetSampleAtDistance(0.95f * track.Length);
            var car = Instantiate(carPrefab, curveSample.location + 0.1f * Vector3.up, curveSample.Rotation);
            var carController = car.GetComponent<CarController>();
            carController.SetSocket(socket);
            carController.raceController = this;
            car.name = "Car" + (++index);
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            motorsEnabled = !motorsEnabled;
            Debug.Log("Motors enabled: " + motorsEnabled);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            FindObjectOfType<LapTimer>().ResetTimers();
            var cars = FindObjectsOfType<CarController>();
            int i = 0;
            foreach (var car in cars)
            {
                var curveSample = track.GetSampleAtDistance(track.Length - (++i * 0.4f));
                car.transform.position = curveSample.location + 0.1f * Vector3.up + curveSample.Rotation * Vector3.right * (i % 2 == 0 ? 1 : -1) * 0.1f;
                car.transform.rotation = curveSample.Rotation;
                car.GetComponent<Rigidbody>().velocity = Vector3.zero;
            }
        }
    }

    private void StartListening()
    {
        if (listener != null) return;
        IPAddress ipAddress = IPAddress.Any;
        int port = 11000;
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

        // Create a TCP/IP socket.  
        listener = new Socket(ipAddress.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);

        listener.Bind(localEndPoint);
        listener.Listen(100);

        Debug.Log("Starting car thread, listening on " + port);

        new Thread(() =>
        {
            while (listener != null)
            {
                // Set the event to nonsignaled state.  
                allDone.Reset();

                // Start an asynchronous socket to listen for connections.  
                Debug.Log("Waiting for car connection...");
                listener.BeginAccept(
                    new AsyncCallback(AcceptCallback),
                    listener);

                // Wait until a connection is made before continuing.  
                allDone.WaitOne();
            }
        }).Start();
    }

    private void EndListening()
    {
        if (listener != null)
        {
            listener.Dispose();
            listener = null;
        }
    }

    private void AcceptCallback(IAsyncResult ar)
    {
        allDone.Set();

        Socket socket = listener.EndAccept(ar);
        socket.SendBufferSize = 20000;
        socket.NoDelay = true;

        clientSocketQueue.Enqueue(socket);
    }
}
