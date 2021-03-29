using System.Collections.Generic;
using System.Collections.Concurrent;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class CarSocket : IDisposable {
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

        var receiverThread = new Thread(() =>
        {
            using var stream = new NetworkStream(socket);
            using var reader = new StreamReader(stream);

            try
            {
                Debug.Log("Reading car info...");
                var line = reader.ReadLine();
                this.carInfo = JsonUtility.FromJson<CarInfo>(line);
                Debug.Log("Received info " + line);
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
                    carInfo.texture = found.texture;
                }
                else if (raceParameters.mode == "race")
                {
                    throw new Exception("Team not found: " + carInfo.teamId);
                }
                Debug.Log("Using car name " + carInfo.name);
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
        });
        receiverThread.Name = "CarSocket Receiver";
        receiverThread.IsBackground = true;
        receiverThread.Start();

        var senderThread = new Thread(() =>
        {
            while (this.socket != null)
            {
                if (sendQueue.TryDequeue(out var data))
                {
                    try
                    {
                        WriteFrame(this.socket, data);
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
        });
        senderThread.Name = "CarSocket Sender";
        senderThread.IsBackground = true;
        senderThread.Start();
    }

    private void WriteFrame(Socket socket, uint[] rawData)
    {
        var data = ImageConversion.EncodeArrayToPNG(rawData, GraphicsFormat.R8G8B8_UNorm, IMAGE_WIDTH, IMAGE_HEIGHT);
        if (data.Length > ushort.MaxValue) throw new Exception("Max image size exceeded");

        // We could write this directly with a single method call... except this is in big endian.
        byte lowerByte = (byte)(data.Length & 0xff);
        byte higherByte = (byte)((data.Length & 0xff00) >> 8);

        socket.Send(new[] { higherByte, lowerByte });
        socket.Send(data);
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
        while (recvQueue.TryDequeue(out var command))
        {
            if (command != null)
            {
                // Seems we get null commands sometimes, when socket closing or something
                yield return command;
            }           
        }
    }

    public void Dispose()
    {
        socket?.Close();
        socket = null;
    }
}