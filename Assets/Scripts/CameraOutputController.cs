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
        Debug.Log("supportsAsyncGPUReadback " + SystemInfo.supportsAsyncGPUReadback);
    }

    // Update is called once per frame
    void Update()
    {
        if (socket == null) return;
        if (Time.time < lastSaved + 0.03 || socket.SendQueueSize() > 5 || socket == null)
        {
            return;
        }

        if (hasRequest && !request.done) {
            Debug.Log("SKIPPED FRAME");
            return;
        }

        if (hasRequest) {
            hasRequest = false;
            //request.WaitForCompletion();
            if (request.hasError)
            {
                Debug.LogError("ERROR reading pixels");
            }
            else
            {
                Debug.Log("DONE reading pixels");
                virtualPhoto.LoadRawTextureData(outputArray);
                virtualPhoto.Apply();
                socket.Send(encodeFrame(virtualPhoto));
            }
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
        //mCamera.Render();
        mCamera.enabled = true;
        request = AsyncGPUReadback.RequestIntoNativeArray(ref outputArray, renderTexture, 0, TextureFormat.ARGB32);
        hasRequest = true;
        
        //RenderTexture.active = renderTexture;
        //virtualPhoto.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        //virtualPhoto.Apply();

        //RenderTexture.active = null; //can help avoid errors 
        //mCamera.targetTexture = null;
        //RenderTexture.ReleaseTemporary(tempRT);

        //socket.Send(encodeFrame(virtualPhoto));
    }

    void OnCompleteReadback(AsyncGPUReadbackRequest request)
    {
        hasRequest = false;
        if (request.hasError)
        {
            Debug.Log("GPU readback error detected.");
            return;
        }
        if (virtualPhoto == null) {
            return;
        }
        virtualPhoto.LoadRawTextureData(request.GetData<uint>());
        virtualPhoto.Apply();
        socket.Send(encodeFrame(virtualPhoto));
    }

    private void OnDestroy()
    {
        //outputArray.Dispose();
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

    public void SetSocket(CarSocket socket)
    {
        if (this.socket != null) return;
        this.socket = socket;
    }
}