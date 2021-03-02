using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;

[RequireComponent(typeof(Camera))]
public class CameraOutputController : MonoBehaviour
{
    private class GPUReader : IDisposable
    {
        private NativeArray<uint> outputArray;
        private AsyncGPUReadbackRequest request;
        private bool hasRequest = false;
        private bool outputReady = false;

        public GPUReader(int size)
        {
            outputArray = new NativeArray<uint>(size, Allocator.Persistent);
        }

        public void Read(RenderTexture renderTexture)
        {
            request = AsyncGPUReadback.RequestIntoNativeArray(ref outputArray, renderTexture, 0, TextureFormat.ARGB32, OnCompleteReadback);
            hasRequest = true;
        }


        public bool WriteTo(Texture2D texture)
        {
            if (hasRequest)
            {
                request.WaitForCompletion();
            }
            if (outputReady)
            {
                outputReady = false;
                texture.LoadRawTextureData(outputArray);
                texture.Apply();
                return true;
            }
            return false;
        }

        void OnCompleteReadback(AsyncGPUReadbackRequest request)
        {
            hasRequest = false;
            if (request.hasError)
            {
                Debug.Log("GPU readback error detected.");
                return;
            }
            outputReady = true;
        }

        public void Dispose()
        {
            if (hasRequest)
            {
                request.WaitForCompletion();
            }
            outputArray.Dispose();
        }
    }

    private const int READERS_LENGTH = 3;
    private const int CURRENT = 0;
    private const int NEXT = READERS_LENGTH - 1;

    private Camera mCamera;
    public RenderTexture renderTexture;
    private GPUReader[] readers = new GPUReader[READERS_LENGTH];
    private Texture2D virtualPhoto;
    private byte[] latestCameraData = null;
    private const int width = 128;
    private const int height = 80;
    private bool read = false;
    private bool async;
    private volatile CarSocket socket;
    FPSLogger logger;

    private void Start()
    {
        mCamera = GetComponent<Camera>();
        renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        renderTexture.antiAliasing = 2;
        for (int i = 0; i < readers.Length; ++i)
        {
            readers[i] = new GPUReader(width * height);
        }
        virtualPhoto = new Texture2D(width, height, TextureFormat.ARGB32, false);
        Debug.Log("CameraOutputController started");
        mCamera.rect = new Rect(0, 0, 1, 1);
        mCamera.aspect = 1.0f * width / height;
        mCamera.targetTexture = renderTexture;
        mCamera.enabled = true;
        RenderPipelineManager.endFrameRendering += OnEndFrameRendering;
        this.async = SystemInfo.supportsAsyncGPUReadback;
        Debug.Log("Async GPU Readback supported: " + this.async);
        logger = FindObjectOfType<FPSLogger>();
    }

    void Update()
    {
        if (socket != null && socket.FrameRequested && latestCameraData != null)
        {
            socket.Send(encodeFrame(latestCameraData));
            latestCameraData = null;
            socket.FrameRequested = false;
            logger.LogFrameSent(socket.CarInfo);
        }
        read = true;
    }

    void OnEndFrameRendering(ScriptableRenderContext context, Camera[] cameras)
    {
        if (socket == null) return;

        if (!read)
        {
            //Debug.Log("Duplicate read call");
            return;
        }
        read = false;

        if (async)
        {
            ReadAsync();
        }
        else
        {
            ReadSync();
        }
    }

    void ReadAsync() 
    {
        readers[NEXT].Read(renderTexture);

        if (readers[CURRENT].WriteTo(virtualPhoto))
        {
            latestCameraData = virtualPhoto.EncodeToPNG();
        }

        Roll(readers);
    }

    void ReadSync()
    {
        RenderTexture.active = renderTexture;
        virtualPhoto.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        virtualPhoto.Apply();
        RenderTexture.active = null; //can help avoid errors 

        latestCameraData = virtualPhoto.EncodeToPNG();
    }

    private void OnDestroy()
    {
        if (this.socket != null)
        {
            this.socket = null;
        }
        for (int i = 0; i < readers.Length; ++i)
        {
            if (readers[i] != null)
            {
                readers[i].Dispose();
            }
            readers[i] = null;
        }
    }

    private byte[] encodeFrame(byte[] data)
    {
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

    private static void Roll(GPUReader[] array)
    {
        GPUReader tmp = array[0];
        for (int i = 1; i < array.Length; ++i)
        {
            array[i - 1] = array[i];
        }
        array[array.Length - 1] = tmp;
    }
}