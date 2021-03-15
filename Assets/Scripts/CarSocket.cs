using System.Collections.Generic;
using System.Collections.Concurrent;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class CarSocket {
    public static uint IMAGE_WIDTH = 128;
    public static uint IMAGE_HEIGHT = 80;
    private volatile Socket socket;
    private readonly ConcurrentQueue<JsonControlCommand> recvQueue = new ConcurrentQueue<JsonControlCommand>();
    private readonly ConcurrentQueue<uint[]> sendQueue = new ConcurrentQueue<uint[]>();
    private CarInfo carInfo;
    public volatile bool FrameRequested = false;

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
                // TODO: respond with error msgs
                if (carInfo.name == null || carInfo.name == "") throw new Exception("CarInfo.name missing");
                if (carInfo.teamId == null || carInfo.teamId == "") throw new Exception("CarInfo.teamId missing");

                var raceParameters = RaceParameters.readRaceParameters();
                var cars = raceParameters.cars;
                var found = cars?.FirstOrDefault(c => c.teamId == carInfo.teamId);
                if (found != null)
                {
                    carInfo.name = found.name;
                    carInfo.color = found.color;
                }
                else if (raceParameters.mode == "race")
                {
                    throw new Exception("Team not found: " + carInfo.teamId);
                }
                EventBus.Publish(new CarConnected(carInfo, this));

                FrameRequested = true;

                while (this.socket != null)
                {
                    line = reader.ReadLine();
                    var command = JsonUtility.FromJson<JsonControlCommand>(line);
                    recvQueue.Enqueue(command);
                    FrameRequested = true;
                }

            }
            catch (Exception e)
            {
                Debug.Log("Car socket read failed:" + e.ToString());
                disconnected();
            }
        }).Start();


        // Sender thread        
        new Thread(() =>
        {
            while (this.socket != null)
            {
                uint[] data;

                if (sendQueue.TryDequeue(out data))
                {
                    try
                    {
                        socket.Send(encodeFrame(data));
                    }
                    catch (Exception e)
                    {
                        Debug.Log("Socket send failed:" + e.ToString());
                        disconnected();
                    }
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }).Start();
    }

    private byte[] encodeFrame(uint[] rawData)
    {
        var data = ImageConversion.EncodeArrayToPNG(rawData, GraphicsFormat.R8G8B8_UNorm, IMAGE_WIDTH, IMAGE_HEIGHT);
        if (data.Length > 65535) throw new Exception("Max image size exceeded");
        byte lowerByte = (byte)(data.Length & 0xff);
        byte higherByte = (byte)((data.Length & 0xff00) >> 8);
        //Debug.Log("Length " + data.Length + " " + higherByte + " " + lowerByte);
        byte[] lengthAsBytes = new byte[] { higherByte, lowerByte };
        byte[] encodedBytes = lengthAsBytes.Concat(data).ToArray();
        return encodedBytes;
    }

    private void disconnected()
    {
        if (this.carInfo != null) {
            Debug.Log("Car disconnected: " + carInfo.name);
            EventBus.Publish(new CarDisconnected(carInfo));
        } else {
            Debug.Log("Car disconnected: " + socket.RemoteEndPoint);
        }
        this.socket = null;
    }

    public Boolean IsConnected()
    {
        return socket != null;
    }

    public CarInfo CarInfo { get {
        return carInfo;
    } }

    public void Send(uint[] data)
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
}