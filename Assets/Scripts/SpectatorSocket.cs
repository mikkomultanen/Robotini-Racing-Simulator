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


public class SpectatorSocket : MonoBehaviour
{
    private GameStatus latestGameStatus;
    private Socket listener = null;
    private readonly ManualResetEvent allDone = new ManualResetEvent(false);
    private DateTime startTime = System.DateTime.Now;    

    private void Update()
    {
        CarController[] cars = FindObjectsOfType<CarController>();
        CarStatus[] statuses = cars.Select(c => new CarStatus(c.rigidBody.position, c.rigidBody.velocity, c.rigidBody.rotation)).ToArray();
        latestGameStatus = new GameStatus(statuses, (float)((System.DateTime.Now - startTime).TotalSeconds));
    }

    private void OnEnable()
    {
        if (ModeController.Mode != SimulatorMode.Playback)
        {
            Debug.Log("Initializing spectator socket");
            StartListening();
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

        new Thread(() =>
        {
            GameStatus myGameStatus = null;
            while (listener != null)
            {
                if (myGameStatus == null || (myGameStatus.timestamp != latestGameStatus.timestamp && ( myGameStatus.cars.Length > 0 || latestGameStatus.cars.Length > 0 ) ))
                {
                    myGameStatus = latestGameStatus;
                    Debug.Log("Send update " + myGameStatus.timestamp);
                    try
                    {
                        string jsonString = JsonUtility.ToJson(latestGameStatus);
                        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(jsonString + "\n");
                        socket.Send(bytes);
                    }
                    catch (Exception e)
                    {
                        Debug.Log("Spectator send failed:" + e.ToString() + " Closing socket");
                        socket.Close();
                        return;
                    }
                }
                else
                {
                    Thread.Sleep(10);
                }
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
}