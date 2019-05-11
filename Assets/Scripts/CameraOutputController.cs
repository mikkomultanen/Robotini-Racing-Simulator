using System.Collections.Concurrent;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraOutputController : MonoBehaviour
{
    private Camera mCamera;
    private Texture2D virtualPhoto;
    private float lastSaved = 0;
    private const int width = 128;
    private const int height = 80;
    private readonly ConcurrentQueue<byte[]> sendQueue = new ConcurrentQueue<byte[]>();
    private volatile Socket socket;

    private void Start()
    {
        mCamera = GetComponent<Camera>();
        virtualPhoto = new Texture2D(width, height, TextureFormat.RGB24, false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time < lastSaved + 0.03 || sendQueue.Count > 5 || socket == null)
        {
            return;
        }

        lastSaved = Time.time;

        mCamera.rect = new Rect(0, 0, 1, 1);
        mCamera.aspect = 1.0f * width / height;
        // recall that the height is now the "actual" size from now on

        RenderTexture tempRT = new RenderTexture(width, height, 24);
        // the 24 can be 0,16,24, formats like
        // RenderTextureFormat.Default, ARGB32 etc.

        mCamera.targetTexture = tempRT;
        mCamera.Render();

        RenderTexture.active = tempRT;
        virtualPhoto.ReadPixels(new Rect(0, 0, width, height), 0, 0);

        RenderTexture.active = null; //can help avoid errors 
        mCamera.targetTexture = null;
        // consider ... Destroy(tempRT);

        sendQueue.Enqueue(virtualPhoto.EncodeToPNG());
    }

    private void OnDestroy()
    {
        if (this.socket != null)
        {
            this.socket = null;
        }
    }

    public void SetSocket(Socket socket)
    {
        if (this.socket != null) return;

        this.socket = socket;

        Boolean stopped = false;
        new Thread(() =>
        {
            while (this.socket != null && !stopped)
            {
                byte[] data;

                if (sendQueue.TryDequeue(out data))
                {
                    if (data.Length > 65535) throw new Exception("Max image size exceeded");
                    byte lowerByte = (byte)(data.Length & 0xff);
                    byte higherByte = (byte)((data.Length & 0xff00) >> 8);
                    //Debug.Log("Length " + data.Length + " " + higherByte + " " + lowerByte);
                    byte[] lengthAsBytes = new byte[] { higherByte, lowerByte };
                    try
                    {
                        socket.Send(lengthAsBytes.Concat(data).ToArray());
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
}