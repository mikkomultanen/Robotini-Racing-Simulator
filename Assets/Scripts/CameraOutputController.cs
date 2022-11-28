using System;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using System.Linq;

interface C {
    void DoRead();
    void Update();
    void OnDestroy();
}

class WebC : C {
    private WebCarSocket socket;
    private RenderTexture renderTexture;
    FPSLogger logger;
    private Texture2D virtualPhoto;
    private byte[] latestCameraData = null;

    public WebC(WebCarSocket socket, RenderTexture renderTexture, FPSLogger logger) {
        this.socket = socket;
        this.renderTexture = renderTexture;
        virtualPhoto = new Texture2D((int)socket.imageWidth, (int)socket.imageHeight, TextureFormat.RGB24, false);
        this.logger = logger;
    }

    public void DoRead()
    {
        RenderTexture.active = renderTexture;
        virtualPhoto.ReadPixels(new Rect(0, 0, socket.imageWidth, socket.imageHeight), 0, 0);
        virtualPhoto.Apply();
        RenderTexture.active = null; //can help avoid errors 
        latestCameraData = virtualPhoto.GetPixelData<byte>(0).ToArray();
    }

    public void Update()
    {
        if (socket != null && socket.IsConnected() && socket.FrameRequested && latestCameraData != null)
        {
            socket.Send(latestCameraData);
            latestCameraData = null;
            socket.FrameRequested = false;
            logger.LogFrameSent(socket.CarInfo);
        }
    }

    public void OnDestroy()
    {
    }

    string ColorToHex(Color32 color)
    {
	    string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
	    return hex;
    }
}

class AsyncC : C {
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
            request = AsyncGPUReadback.RequestIntoNativeArray(ref outputArray, renderTexture, 0, TextureFormat.RGB24, OnCompleteReadback);
            hasRequest = true;
        }


        public bool WriteTo(out uint[] data)
        {
            if (hasRequest)
            {
                request.WaitForCompletion();
            }
            if (outputReady)
            {
                outputReady = false;
                data = outputArray.ToArray();
                return true;
            }
            data = null;
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
    private GPUReader[] readers = new GPUReader[READERS_LENGTH];
    private CarSocket socket;
    private RenderTexture renderTexture;
    FPSLogger logger;
    private uint[] latestCameraData = null;

    public AsyncC(CarSocket socket, RenderTexture renderTexture, FPSLogger logger) {
        this.socket = socket;
        this.logger = logger;
        this.renderTexture = renderTexture;
        for (int i = 0; i < readers.Length; ++i)
        {
            readers[i] = new GPUReader((int)socket.imageWidth * (int)socket.imageHeight);
        }
    }

    public void DoRead()
    {
        readers[NEXT].Read(renderTexture);

        uint[] data;
        if (readers[CURRENT].WriteTo(out data))
        {
            latestCameraData = data;
        }

        Roll(readers);
    }

    public void Update()
    {
        if (socket != null && socket.IsConnected() && socket.FrameRequested && latestCameraData != null)
        {
            socket.Send(latestCameraData);
            latestCameraData = null;
            socket.FrameRequested = false;
            logger.LogFrameSent(socket.CarInfo);
        }
    }

    public void OnDestroy()
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

class SyncC : C {
    private CarSocket socket;
    private RenderTexture renderTexture;
    private Texture2D virtualPhoto;
    FPSLogger logger;
    private uint[] latestCameraData = null;

    public SyncC(CarSocket socket, RenderTexture renderTexture, FPSLogger logger) {
        this.socket = socket;
        this.renderTexture = renderTexture;
        virtualPhoto = new Texture2D((int)socket.imageWidth, (int)socket.imageHeight, TextureFormat.RGB24, false);
        this.logger = logger;
    }

    public void DoRead()
    {
        RenderTexture.active = renderTexture;
        virtualPhoto.ReadPixels(new Rect(0, 0, socket.imageWidth, socket.imageHeight), 0, 0);
        virtualPhoto.Apply();
        RenderTexture.active = null; //can help avoid errors 
        latestCameraData = virtualPhoto.GetPixelData<uint>(0).ToArray();
    }

    public void Update()
    {
        if (socket != null && socket.IsConnected() && socket.FrameRequested && latestCameraData != null)
        {
            socket.Send(latestCameraData);
            latestCameraData = null;
            socket.FrameRequested = false;
            logger.LogFrameSent(socket.CarInfo);
        }
    }

    public void OnDestroy()
    {
    }
}

[RequireComponent(typeof(Camera))]
public class CameraOutputController : MonoBehaviour
{    
    private C c;

    private Camera mCamera;
    private RenderTexture renderTexture;
    private Texture2D virtualPhoto;
    private bool read = false;
    
    private volatile CarSocketBase socket;
    FPSLogger logger;

    public Camera Camera {
        get { return mCamera; }
    }

    public static bool ShouldEnableCamera
    {
        get {
            var enable = FindObjectOfType<RaceController>().IsSimulation;
            Debug.Log("Enable camera:" + enable);
            return enable;
        }
    }
    private void Start()
    {
        mCamera = GetComponent<Camera>();
        mCamera.enabled = ShouldEnableCamera;
        if (!mCamera.enabled) return;
        if (socket == null) {
            Debug.LogWarning("Camera enabled, but socket missing");
            return;
        }
        renderTexture = new RenderTexture((int)socket.imageWidth, (int)socket.imageHeight, 24, RenderTextureFormat.ARGB32);
        renderTexture.antiAliasing = 2;        
        
        Debug.Log("CameraOutputController started");
        mCamera.rect = new Rect(0, 0, 1, 1);
        mCamera.aspect = 1.0f * socket.imageWidth / socket.imageHeight;
        mCamera.targetTexture = renderTexture;

        RenderPipelineManager.endFrameRendering += OnEndFrameRendering;

        bool async = SystemInfo.supportsAsyncGPUReadback;
        Debug.Log("Async GPU Readback supported: " + async);
        logger = FindObjectOfType<FPSLogger>();
        if (socket is CarSocket s) {
            if (async) {
                this.c = new AsyncC(s, renderTexture, logger);
            } else {
                // TODO: Async GPU doesn't currently work, needs a re-impl.
                this.c = new SyncC(s, renderTexture, logger);
            }
        } else if (socket is WebCarSocket ws) {
            this.c = new WebC(ws, renderTexture, logger);
        }
    }

    void Update()
    {
        if (c != null) {
            c.Update();
            read = true;
        }
    }

    void OnEndFrameRendering(ScriptableRenderContext context, Camera[] cameras)
    {
        if (socket == null || !socket.IsConnected()) return;

        if (!read)
        {
            //Debug.Log("Duplicate read call");
            return;
        }
        read = false;

        c.DoRead();

    }

    private void OnDestroy()
    {
        if (this.socket != null)
        {
            this.socket = null;
        }
        c.OnDestroy();
    }

    public RenderTexture RenderTexture {
        get { return renderTexture; }
    }

    public void SetSocket(CarSocketBase socket)
    {
        if (this.socket != null) return;
        this.socket = socket;
    }
}