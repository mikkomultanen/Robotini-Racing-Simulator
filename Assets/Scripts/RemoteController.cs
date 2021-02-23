using UnityEngine;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Collections.Concurrent;
using System;

public class RemoteController : RemoteEventPlayer
{
    private readonly ConcurrentQueue<GameEvent> recvQueue = new ConcurrentQueue<GameEvent>();

    void Start()
    {
        if (ModeController.Mode == SimulatorMode.RemoteControl)
        {
            Debug.Log("Initializing remote controller");
            var client = new TcpClient("localhost", 11001);
            Debug.Log("Connected to Simulator");

            new Thread(() => {
                var stream = client.GetStream();
                var reader = new StreamReader(stream);
                try
                {                
                    var line = reader.ReadLine();
                    while (client != null)
                    {
                        line = reader.ReadLine();
                        var command = GameEvent.FromJson(line);
                        recvQueue.Enqueue(command);
                    }

                }
                catch (Exception e)
                {
                    Debug.Log("RemoteControl socket read failed:" + e.ToString());
                    client = null;
                }
            }).Start();

            EventBus.Subscribe<UICommand>(this, cmd => {
                Debug.Log("Sending " + cmd.type);
                string jsonString = JsonUtility.ToJson(cmd);
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(jsonString + "\n");
                client.GetStream().Write(bytes, 0, bytes.Length);
            });
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
