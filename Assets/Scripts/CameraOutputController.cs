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

    private Camera mCamera;
    private RenderTexture renderTexture;
    private GPUReader[] readers = new GPUReader[READERS_LENGTH];
    private Texture2D virtualPhoto;
    private uint[] latestCameraData = null;
    private bool read = false;
    private bool async;
    private volatile CarSocketBase socket;
    FPSLogger logger;

    public Camera Camera {
        get { return mCamera; }
    }

    public static bool ShouldEnableCamera
    {
        get { return ModeController.Mode == SimulatorMode.Development || ModeController.Mode == SimulatorMode.Race; }
    }
    private void Start()
    {
        mCamera = GetComponent<Camera>();
        renderTexture = new RenderTexture((int)CarSocket.IMAGE_WIDTH, (int)CarSocket.IMAGE_HEIGHT, 24, RenderTextureFormat.ARGB32);
        renderTexture.antiAliasing = 2;
        for (int i = 0; i < readers.Length; ++i)
        {
            readers[i] = new GPUReader((int)CarSocket.IMAGE_WIDTH * (int)CarSocket.IMAGE_HEIGHT);
        }
        virtualPhoto = new Texture2D((int)CarSocket.IMAGE_WIDTH, (int)CarSocket.IMAGE_HEIGHT, TextureFormat.RGB24, false);
        Debug.Log("CameraOutputController started");
        mCamera.rect = new Rect(0, 0, 1, 1);
        mCamera.aspect = 1.0f * CarSocket.IMAGE_WIDTH / CarSocket.IMAGE_HEIGHT;
        mCamera.targetTexture = renderTexture;
        mCamera.enabled = ShouldEnableCamera;
        RenderPipelineManager.endFrameRendering += OnEndFrameRendering;
        this.async = SystemInfo.supportsAsyncGPUReadback;
        Debug.Log("Async GPU Readback supported: " + this.async);
        logger = FindObjectOfType<FPSLogger>();
    }

    void Update()
    {
        if (socket != null && socket.IsConnected() && socket.FrameRequested && latestCameraData != null)
        {
            socket.Send(latestCameraData);
            latestCameraData = null;
            socket.FrameRequested = false;
            logger.LogFrameSent(socket.CarInfo);
        }
        read = true;
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

        uint[] data;
        if (readers[CURRENT].WriteTo(out data))
        {
            latestCameraData = data;
        }

        Roll(readers);
    }

    void ReadSync()
    {
        RenderTexture.active = renderTexture;
        virtualPhoto.ReadPixels(new Rect(0, 0, CarSocket.IMAGE_WIDTH, CarSocket.IMAGE_HEIGHT), 0, 0);
        virtualPhoto.Apply();
        RenderTexture.active = null; //can help avoid errors 

        latestCameraData = virtualPhoto.GetPixelData<uint>(0).ToArray();
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

    public RenderTexture RenderTexture {
        get { return renderTexture; }
    }

    public void SetSocket(CarSocketBase socket)
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