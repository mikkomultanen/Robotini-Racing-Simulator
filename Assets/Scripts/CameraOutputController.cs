using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;

[RequireComponent(typeof(Camera))]
public class CameraOutputController : MonoBehaviour
{
    private Camera mCamera;
    public RenderTexture renderTexture;
    private NativeArray<uint> outputArray;
    private AsyncGPUReadbackRequest request;
    private bool hasRequest = false;
    private Texture2D virtualPhoto;
    private float lastSaved = 0;
    private const int width = 128;
    private const int height = 80;
    
    private volatile CarSocket socket;

    private void Start()
    {
        mCamera = GetComponent<Camera>();
        renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        renderTexture.antiAliasing = 2;
        outputArray = new NativeArray<uint>(width * height, Allocator.Persistent);
        virtualPhoto = new Texture2D(width, height, TextureFormat.ARGB32, false);
        Debug.Log("CameraOutputController started");
        mCamera.rect = new Rect(0, 0, 1, 1);
        mCamera.aspect = 1.0f * width / height;
        mCamera.targetTexture = renderTexture;
        mCamera.enabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (socket == null) return;
        if (Time.time < lastSaved + 0.03 || socket.SendQueueSize() > 5 || socket == null)
        {
            return;
        }

        if (hasRequest)
        {
            request.WaitForCompletion();
        }

        lastSaved = Time.time;

        request = AsyncGPUReadback.RequestIntoNativeArray(ref outputArray, renderTexture, 0, TextureFormat.ARGB32, OnCompleteReadback);
        hasRequest = true;
    }

    void OnCompleteReadback(AsyncGPUReadbackRequest request)
    {
        hasRequest = false;
        if (request.hasError)
        {
            Debug.Log("GPU readback error detected.");
            return;
        }
        if (virtualPhoto == null || socket == null) {
            return;
        }
        virtualPhoto.LoadRawTextureData(outputArray);
        virtualPhoto.Apply();
        socket.Send(encodeFrame(virtualPhoto));
    }

    private void OnDestroy()
    {
        if (this.socket != null)
        {
            this.socket = null;
        }
        if (hasRequest)
        {
            request.WaitForCompletion();
        }
        outputArray.Dispose();
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

    public void SetSocket(CarSocket socket)
    {
        if (this.socket != null) return;
        this.socket = socket;
    }
}