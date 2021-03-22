using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class CarSocketListener : MonoBehaviour {
    private readonly ManualResetEvent allDone = new ManualResetEvent(false);
    private readonly ConcurrentQueue<Socket> clientSocketQueue = new ConcurrentQueue<Socket>();
    private Socket listener = null;
    private List<IDisposable> disposables = new List<IDisposable>();

    private void OnEnable()
    {
        if (ModeController.Mode != SimulatorMode.Playback && ModeController.Mode != SimulatorMode.RemoteControl)
        {
            Debug.Log("Initializing car socket");
            StartListening();
        }        
    }

    private void OnDisable()
    {
        EndListening();
    }

    private void OnDestroy()
    {
        foreach (var d in disposables)
        {
            d.Dispose();
        }
        disposables.Clear();
    }

    private void Update()
    {
        Socket socket;
        while (clientSocketQueue.TryDequeue(out socket))
        {
           disposables.Add(new CarSocket(socket, FindObjectOfType<RaceController>()));
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

        var listenerThread = new Thread(() =>
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
        });
        listenerThread.Name = "CarSocketListener";
        listenerThread.IsBackground = true;
        listenerThread.Start();
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