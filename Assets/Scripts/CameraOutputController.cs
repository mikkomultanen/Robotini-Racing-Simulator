using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraOutputController : MonoBehaviour
{
    private Camera mCamera;
    private float lastSaved = 0;

    private void OnEnable()
    {
       BotSocket.StartListening();
    }

    private void OnDisable()
    {
        BotSocket.EndListening();
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

        BotSocket.SendFrame(bytes);
    }
}