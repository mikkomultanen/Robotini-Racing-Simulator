using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

[RequireComponent(typeof(Camera))]
public class CameraOutputController : MonoBehaviour
{
    private Camera mCamera;
    private float lastSaved = 0;

    private void OnEnable()
    {
       AsynchronousSocketListener.StartListening();
    }

    private void OnDisable()
    {
        AsynchronousSocketListener.EndListening();
    }

    private void Start()
    {
        mCamera = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time < lastSaved + 0.03)
        {
            return;
        }

        lastSaved = Time.time;
        int width = 128;
        int height = 80;

        float originalAspect = GetComponent<Camera>().aspect;
        mCamera.aspect = 1.0f;
        // recall that the height is now the "actual" size from now on

        RenderTexture tempRT = new RenderTexture(width, height, 24);
        // the 24 can be 0,16,24, formats like
        // RenderTextureFormat.Default, ARGB32 etc.

        mCamera.targetTexture = tempRT;
        mCamera.Render();

        RenderTexture.active = tempRT;
        Texture2D virtualPhoto =
            new Texture2D(width, height, TextureFormat.RGB24, false);
        // false, meaning no need for mipmaps
        virtualPhoto.ReadPixels(new Rect(0, 0, width, height), 0, 0);

        RenderTexture.active = null; //can help avoid errors 
        mCamera.targetTexture = null;
        mCamera.aspect = originalAspect;
        // consider ... Destroy(tempRT);

        byte[] bytes;
        bytes = virtualPhoto.EncodeToPNG();

        AsynchronousSocketListener.SendFrame(bytes);
    }
}

// State object for reading client data asynchronously  
public class StateObject : IDisposable
{
    // Client  socket.  
    public Socket workSocket = null;
    // Size of receive buffer.  
    public const int BufferSize = 1024;
    // Receive buffer.  
    public byte[] buffer = new byte[BufferSize];
    // Received data string.  
    public StringBuilder sb = new StringBuilder();

    public void Dispose() {
        if (workSocket == null) return;
        workSocket.Dispose();
        workSocket = null;
    }
}

public class AsynchronousSocketListener
{
    // Thread signal.  
    public static ManualResetEvent allDone = new ManualResetEvent(false);
    static Socket listener = null;
    static StateObject globalState = null;
    static bool sending = false;
    static ConcurrentQueue<byte[]> sendQueue = new ConcurrentQueue<byte[]>();

    public async static void SendFrame(byte[] data)
    {
        if (sendQueue.Count > 5)
        {
            Debug.Log("Queue full");
        }
        else
        {
            sendQueue.Enqueue(data);
        }
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

        new Thread(() =>
        {
            while (listener != null)
            {
                byte[] data;
                if (sendQueue.TryDequeue(out data) && globalState != null)
                {
                    if (data.Length > 65535) throw new Exception("Max image size exceeded");
                    byte lowerByte = (byte)(data.Length & 0xff);
                    byte higherByte = (byte)((data.Length & 0xff00) >> 8);
                    // Debug.Log("Length " + data.Length + " " + higherByte + " " + lowerByte);
                    byte[] lengthAsBytes = new byte[] { higherByte, lowerByte };
                    try
                    {
                        globalState.workSocket.Send(lengthAsBytes.Concat(data).ToArray());
                    }
                    catch (Exception e)
                    {
                        Debug.Log("Socket send failed:" + e.ToString());
                        globalState = null;
                    }
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }).Start();

    }

    public static void EndListening()
    {
        if (globalState != null) {
            globalState.Dispose();
            globalState = null;
        }
        if (listener != null) {
            listener.Dispose();
            listener = null;
        }
    }

    public static void AcceptCallback(IAsyncResult ar)
    {
        // Signal the main thread to continue.  
        allDone.Set();

        // Get the socket that handles the client request.  
        Socket listener = (Socket)ar.AsyncState;
        Socket socket = listener.EndAccept(ar);

        // Create the state object.  
        globalState = new StateObject();
        globalState.workSocket = socket;
        socket.SendBufferSize = 20000;
        socket.NoDelay = true;

        Debug.Log("Client connected");

    }

    public static void ReadCallback(IAsyncResult ar)
    {

    }
}