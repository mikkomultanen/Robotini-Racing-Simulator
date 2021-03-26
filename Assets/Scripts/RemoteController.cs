using UnityEngine;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Collections.Concurrent;
using System;

public class RemoteController : RemoteEventPlayer
{
    private readonly ConcurrentQueue<GameEvent> recvQueue = new ConcurrentQueue<GameEvent>();
    private TcpClient client;

    void Start()
    {
        if (ModeController.Mode == SimulatorMode.RemoteControl)
        {
            Debug.Log("Initializing remote controller");
            client = new TcpClient("localhost", 11001);
            Debug.Log("Connected to Simulator");

            var receiverThread = new Thread(() => {
                var stream = client.GetStream();
                var reader = new StreamReader(stream);
                try
                {
                    string line = null;
                    while (client != null)
                    {
                        line = reader.ReadLine();
                        var command = GameEvent.FromJson(line);
                        if (!(command is UICommand)) {
                            recvQueue.Enqueue(command);
                        }
                    }

                }
                catch (Exception e)
                {
                    Debug.Log("RemoteControl socket read failed:" + e.ToString());
                    client = null;
                }
            });
            receiverThread.Name = "RemoteController Receiver";
            receiverThread.IsBackground = true;
            receiverThread.Start();

            EventBus.Subscribe<UICommand>(this, cmd => {
                Debug.Log("Sending " + cmd.type);
                string jsonString = JsonUtility.ToJson(cmd);
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(jsonString + "\n");
                client.GetStream().Write(bytes, 0, bytes.Length);
            });
        }
    }

    private void OnDestroy()
    {
        if (client != null)
        {
            Debug.Log("Close TCP client");
            client.Close();
            client = null;
        }
    }

    // Update is called once per frame
    void Update()
    {
        GameEvent command = null;
        while (recvQueue.TryDequeue(out command))
        {
            if (command != null)
            {
                ApplyEvent(command);
            }            
        }
    }
}
