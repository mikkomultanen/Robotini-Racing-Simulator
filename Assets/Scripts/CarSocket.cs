using System.Collections.Generic;
using System.Collections.Concurrent;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class VirtualCarSocket {

}

public abstract class CarSocketBase {
    protected readonly ConcurrentQueue<JsonControlCommand> recvQueue = new ConcurrentQueue<JsonControlCommand>();
    protected readonly ConcurrentQueue<uint[]> sendQueue = new ConcurrentQueue<uint[]>();
    private CarInfo carInfo;
    public volatile bool FrameRequested = false;

    protected void init(CarInfo c)
    {
        this.carInfo = c;
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
    }

    public CarInfo CarInfo { get {
        return carInfo;
    }}


    public abstract bool IsConnected();

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

public class CarSocket : CarSocketBase, IDisposable {
    public static uint IMAGE_WIDTH = 128;
    public static uint IMAGE_HEIGHT = 80;
    private volatile Socket socket;

    public CarSocket(Socket socket, RaceController raceController)
    {
        this.socket = socket;

        var receiverThread = new Thread(() =>
        {
            var stream = new NetworkStream(socket);
            var reader = new StreamReader(stream);
            try
            {
                Debug.Log("Reading car info...");
                var line = reader.ReadLine();
                Debug.Log("Received info " + line);

                init(JsonUtility.FromJson<CarInfo>(line));

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
        });
        senderThread.Name = "CarSocket Sender";
        senderThread.IsBackground = true;
        senderThread.Start();
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
        if (this.CarInfo != null) {
            Debug.Log("Car disconnected: " + CarInfo.name);
            EventBus.Publish(new CarDisconnected(CarInfo));
        } else {
            Debug.Log("Car disconnected: " + socket.RemoteEndPoint);
        }
        this.socket = null;
    }

    public override Boolean IsConnected()
    {
        return socket != null;
    }

    public void Dispose()
    {
        socket?.Close();
        socket = null;
    }
}