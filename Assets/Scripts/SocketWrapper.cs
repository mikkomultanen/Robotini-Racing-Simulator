using System.Collections.Generic;
using System.Collections.Concurrent;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class SocketWrapper {
    private volatile Socket socket;
    private readonly ConcurrentQueue<JsonControlCommand> commandQueue = new ConcurrentQueue<JsonControlCommand>();
    private readonly ConcurrentQueue<byte[]> sendQueue = new ConcurrentQueue<byte[]>();

    public SocketWrapper(Socket socket)
    {
        this.socket = socket;

        Boolean stopped = false;

        // Receiver thread
        new Thread(() => {
            var stream = new NetworkStream(socket);
            var reader = new StreamReader(stream);
            while (this.socket != null && !stopped)
            {
                try
                {
                    var line = reader.ReadLine();
                    var command = JsonUtility.FromJson<JsonControlCommand>(line);
                    if (command != null) {
                        // Seems we get null commands sometimes, when socket closing or something
                        commandQueue.Enqueue(command);
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("Socket read failed:" + e.ToString());
                    stopped = true;
                }
            }
            commandQueue.Enqueue(new JsonControlCommand {
                action = "disconnected"
            });
        }).Start();


        // Sender thread        
        new Thread(() =>
        {
            while (this.socket != null && !stopped)
            {
                byte[] data;

                if (sendQueue.TryDequeue(out data))
                {
                    try
                    {
                        socket.Send(data);
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

    public void Send(byte[] data)
    {
        sendQueue.Enqueue(data);
    }

    public int SendQueueSize()
    {
        return sendQueue.Count;
    }

    public IEnumerable<JsonControlCommand> ReceiveCommands()
    {
        var commands = new List<JsonControlCommand>();
        JsonControlCommand command = null;
        while (commandQueue.TryDequeue(out command))
        {
            commands.Add(command);
        }
        return commands;
    }

    public void Dispose()
    {
        this.socket.Dispose();
    }
}