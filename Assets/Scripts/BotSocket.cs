using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using UnityEngine;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

[Serializable]
public class JsonControlCommand
{
    public string action;
    public string move;
    public float value;
}

public class BotSocket
{
    // Thread signal.  
    public static ManualResetEvent allDone = new ManualResetEvent(false);
    static Socket listener = null;
    static ConcurrentQueue<byte[]> sendQueue = new ConcurrentQueue<byte[]>();
    static ConcurrentQueue<JsonControlCommand> commandQueue = new ConcurrentQueue<JsonControlCommand>();

    public static void SendFrame(byte[] data)
    {
        if (sendQueue.Count > 5)
        {
            //Debug.Log("Queue full");
        }
        else
        {
            sendQueue.Enqueue(data);
        }
    }

    public static IEnumerable<JsonControlCommand> ReceiveCommands()
    {
        var commands = new List<JsonControlCommand>();
        JsonControlCommand command = null;
        while (commandQueue.TryDequeue(out command))
        {
            commands.Add(command);
        }
        return commands;
    }

    public static void StartListening()
    {
        if (listener != null) return;
        // Establish the local endpoint for the socket.  
        // The DNS name of the computer  
        // running the listener is "host.contoso.com".  
        IPAddress ipAddress = IPAddress.Any;
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

        // Create a TCP/IP socket.  
        listener = new Socket(ipAddress.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);

        listener.Bind(localEndPoint);
        listener.Listen(100);

        Debug.Log("Starting thread");

        new Thread(() =>
        {
            while (listener != null)
            {
                // Set the event to nonsignaled state.  
                allDone.Reset();

                // Start an asynchronous socket to listen for connections.  
                Debug.Log("Waiting for a connection...");
                listener.BeginAccept(
                    new AsyncCallback(AcceptCallback),
                    listener);

                // Wait until a connection is made before continuing.  
                allDone.WaitOne();
            }
        }).Start();
    }

    public static void EndListening()
    {
        if (listener != null)
        {
            listener.Dispose();
            listener = null;
        }
    }

    public static void AcceptCallback(IAsyncResult ar)
    {
        allDone.Set();

        Socket socket = listener.EndAccept(ar);
        socket.SendBufferSize = 20000;
        socket.NoDelay = true;

        Debug.Log("Client connected");
        Boolean stopped = false;

        new Thread(() => {
            var stream = new NetworkStream(socket);
            var reader = new StreamReader(stream);
            while (listener != null && !stopped)
            {
                var line = reader.ReadLine();
                var command = JsonUtility.FromJson<JsonControlCommand>(line);
                commandQueue.Enqueue(command);
            }

        }).Start();

        new Thread(() =>
        {
            while (listener != null && !stopped)
            {
                byte[] data;

                if (sendQueue.TryDequeue(out data))
                {
                    if (data.Length > 65535) throw new Exception("Max image size exceeded");
                    byte lowerByte = (byte)(data.Length & 0xff);
                    byte higherByte = (byte)((data.Length & 0xff00) >> 8);
                    // Debug.Log("Length " + data.Length + " " + higherByte + " " + lowerByte);
                    byte[] lengthAsBytes = new byte[] { higherByte, lowerByte };
                    try
                    {
                        socket.Send(lengthAsBytes.Concat(data).ToArray());
                    }
                    catch (Exception e)
                    {
                        Debug.Log("Socket send failed:" + e.ToString());
                        stopped = true;
                    }
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }).Start();
    }
}