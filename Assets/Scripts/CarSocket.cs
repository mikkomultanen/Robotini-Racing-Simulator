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
    public static uint IMAGE_WIDTH = 128;
    public static uint IMAGE_HEIGHT = 80;
    public uint imageWidth = IMAGE_WIDTH;
    public uint imageHeight = IMAGE_HEIGHT;

    protected readonly ConcurrentQueue<JsonControlCommand> recvQueue = new ConcurrentQueue<JsonControlCommand>();
    protected readonly ConcurrentQueue<uint[]> sendQueue = new ConcurrentQueue<uint[]>();
    private CarInfo carInfo;

    public volatile bool FrameRequested = false;

    protected void init(CarLogin c)
    {
        // TODO: respond with error msgs
        if (c.name == null || c.name == "") throw new Exception("CarInfo.name missing");
        if (c.teamId == null || c.teamId == "") throw new Exception("CarInfo.teamId missing");

        var raceParameters = RaceParameters.readRaceParameters();
        var cars = raceParameters.cars;
        var found = cars?.FirstOrDefault(d => d.teamId == c.teamId);
        if (found != null)
        {
            carInfo = found;
        }
        else if (raceParameters.mode == "race")
        {
            throw new Exception("Team not found: " + c.teamId);
        }
        else {
            carInfo = new CarInfo(c.teamId, c.name, c.color);
        }

        this.imageWidth = (c.imageWidth < 8 || c.imageWidth > 128) ? 128 : (uint)c.imageWidth;
        this.imageHeight = imageWidth * IMAGE_HEIGHT / IMAGE_WIDTH;
        Debug.Log("Using car name " + carInfo.name + " and image size " + imageWidth + "x" + imageHeight);
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
        while (recvQueue.TryDequeue(out var command))
        {
            if (command != null)
            {
                // Seems we get null commands sometimes, when socket closing or something
                yield return command;
            }           
        }
    }
}

public class CarSocket : CarSocketBase, IDisposable {
    private volatile Socket socket;

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
                Debug.Log("Received info " + line);

                init(JsonUtility.FromJson<CarLogin>(line));

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
        var data = ImageConversion.EncodeArrayToPNG(rawData, GraphicsFormat.R8G8B8_UNorm, imageWidth, imageHeight);
        if (data.Length > ushort.MaxValue) throw new Exception("Max image size exceeded");

        // We could write this directly with a single method call... except this is in big endian.
        byte lowerByte = (byte)(data.Length & 0xff);
        byte higherByte = (byte)((data.Length & 0xff00) >> 8);

        socket.Send(new[] { higherByte, lowerByte });
        socket.Send(data);
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