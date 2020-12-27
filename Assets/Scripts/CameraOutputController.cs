using System;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraOutputController : MonoBehaviour
{
    private Camera mCamera;
    private RenderTexture renderTexture;
    private Texture2D virtualPhoto;
    private float lastSaved = 0;
    private const int width = 128;
    private const int height = 80;
    
    private volatile SocketWrapper socket;

    private void Start()
    {
        mCamera = GetComponent<Camera>();
        renderTexture = new RenderTexture(width, height, 24);
        renderTexture.antiAliasing = 2;
        virtualPhoto = new Texture2D(width, height, TextureFormat.RGB24, false);
        Debug.Log("CameraOutputController started");
    }

    // Update is called once per frame
    void Update()
    {
        if (socket == null) return;
        if (Time.time < lastSaved + 0.03 || socket.SendQueueSize() > 5 || socket == null)
        {
            return;
        }

        lastSaved = Time.time;

        mCamera.rect = new Rect(0, 0, 1, 1);
        mCamera.aspect = 1.0f * width / height;
        // recall that the height is now the "actual" size from now on

        //RenderTexture tempRT = RenderTexture.GetTemporary(width, height, 24);
        // the 24 can be 0,16,24, formats like
        // RenderTextureFormat.Default, ARGB32 etc.
        //tempRT.antiAliasing = 2;

        mCamera.targetTexture = renderTexture;
        mCamera.Render();

        RenderTexture.active = renderTexture;
        virtualPhoto.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        virtualPhoto.Apply();

        RenderTexture.active = null; //can help avoid errors 
        mCamera.targetTexture = null;
        //RenderTexture.ReleaseTemporary(tempRT);

        socket.Send(encodeFrame(virtualPhoto));
    }

    private void OnDestroy()
    {
        if (this.socket != null)
        {
            this.socket = null;
        }
    }

    private byte[] encodeFrame(Texture2D virtualPhoto)
    {
        byte[] data = virtualPhoto.EncodeToPNG();

        if (data.Length > 65535) throw new Exception("Max image size exceeded");
        byte lowerByte = (byte)(data.Length & 0xff);
        byte higherByte = (byte)((data.Length & 0xff00) >> 8);
        //Debug.Log("Length " + data.Length + " " + higherByte + " " + lowerByte);
        byte[] lengthAsBytes = new byte[] { higherByte, lowerByte };
        byte[] encodedBytes = lengthAsBytes.Concat(data).ToArray();
        return encodedBytes;
    }

    public void SetSocket(SocketWrapper socket)
    {
        if (this.socket != null) return;
        this.socket = socket;
    }
}