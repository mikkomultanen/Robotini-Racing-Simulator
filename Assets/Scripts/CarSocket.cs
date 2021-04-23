using System.Collections.Generic;
using System.Collections.Concurrent;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

class JoinException : Exception {
    public JoinException(string message): base(message) {
    }
}

public class WebCarSocket: CarSocketBase {
    WebRaceController controller;
    public WebCarSocket(CarLogin login, WebRaceController controller) {
        init(login);
        this.controller = controller;
    }

    public override bool IsConnected()
    {
        return true;
    }

    public void Send(Color32[] data)
    {
        var pixels = data.SelectMany(color => new int[] { color.r, color.g, color.b }).ToArray();
        controller.SendToWeb(new WebCarFrame(CarInfo.name, pixels));
    }
}

public abstract class CarSocketBase {
    public static uint IMAGE_WIDTH = 128;
    public static uint IMAGE_HEIGHT = 80;
    public uint imageWidth = IMAGE_WIDTH;
    public uint imageHeight = IMAGE_HEIGHT;

    protected readonly ConcurrentQueue<JsonControlCommand> recvQueue = new ConcurrentQueue<JsonControlCommand>();
    private CarInfo carInfo;

    public volatile bool FrameRequested = false;

    protected void init(CarLogin c)
    {
        if (c.name == null || c.name == "") throw new JoinException("CarInfo.name missing");
        if (c.teamId == null || c.teamId == "") throw new JoinException("CarInfo.teamId missing");

        var raceParameters = RaceParameters.readRaceParameters();
        var cars = raceParameters.cars;
        var found = cars?.FirstOrDefault(d => d.teamId == c.teamId);
        if (found != null)
        {
            carInfo = found;
        }
        else if (raceParameters.mode == "race")
        {
            throw new JoinException("Team not found by teamId: " + c.teamId);
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

    public void EnqueueCommand(JsonControlCommand command) {
        recvQueue.Enqueue(command);
        FrameRequested = true;
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
    private volatile Socket socket;
    protected readonly ConcurrentQueue<uint[]> sendQueue = new ConcurrentQueue<uint[]>();

    public CarSocket(Socket socket)
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

                try {
                    init(JsonUtility.FromJson<CarLogin>(line));
                } catch (JoinException e) {
                    Debug.Log("Car login denied: " + e.Message);
                    socket.Close();
                    return;
                }

                while (this.socket != null)
                {
                    line = reader.ReadLine();
                    var command = JsonUtility.FromJson<JsonControlCommand>(line);
                    EnqueueCommand(command);
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
        var data = ImageConversion.EncodeArrayToPNG(rawData, GraphicsFormat.R8G8B8_UNorm, imageWidth, imageHeight);
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

    public void Send(uint[] data)
    {
        sendQueue.Enqueue(data);
    }

    public void Dispose()
    {
        socket?.Close();
        socket = null;
    }
}