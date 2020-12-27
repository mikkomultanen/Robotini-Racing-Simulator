using System.Collections.Generic;
using System.Collections.Concurrent;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class CarSocket {
    private volatile Socket socket;
    private readonly ConcurrentQueue<JsonControlCommand> recvQueue = new ConcurrentQueue<JsonControlCommand>();
    private readonly ConcurrentQueue<byte[]> sendQueue = new ConcurrentQueue<byte[]>();
    private CarInfo carInfo;

    public CarSocket(Socket socket, RaceController raceController)
    {
        this.socket = socket;

        // Receiver thread
        new Thread(() => {
            var stream = new NetworkStream(socket);
            var reader = new StreamReader(stream);
            try
            {
                Debug.Log("Reading car info...");
                var line = reader.ReadLine();
                this.carInfo = JsonUtility.FromJson<CarInfo>(line);
                Debug.Log("car info " + line);
                if (carInfo.name == null || carInfo.name == "") throw new Exception("CarInfo.name missing");
                if (carInfo.teamId == null || carInfo.teamId == "") throw new Exception("CarInfo.teamId missing");
                raceController.AddCarSocket(this);

                while (this.socket != null)
                {
                    line = reader.ReadLine();
                    var command = JsonUtility.FromJson<JsonControlCommand>(line);
                    recvQueue.Enqueue(command);
                }

            }
            catch (Exception e)
            {
                Debug.Log("Socket read failed:" + e.ToString());
                this.socket = null;
            }
        }).Start();


        // Sender thread        
        new Thread(() =>
        {
            while (this.socket != null)
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
                        this.socket = null;
                    }
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }).Start();
    }

    public Boolean IsConnected()
    {
        return socket != null;
    }

    public CarInfo CarInfo()
    {
        return carInfo;
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
        while (recvQueue.TryDequeue(out command))
        {
            if (command != null)
            {
                // Seems we get null commands sometimes, when socket closing or something
                commands.Add(command);
            }            
        }
        return commands;
    }

    public void Dispose()
    {
        if (this.socket != null)
        {
            this.socket.Dispose();
        }
    }
}