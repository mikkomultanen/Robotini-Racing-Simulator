using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraOutputController : MonoBehaviour
{
    private static string SCREENSHOT_FILE = "./screenshot.png";
    public Camera camera;
    private float lastSaved = 0;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time < lastSaved + 0.03 || System.IO.File.Exists(SCREENSHOT_FILE))
        {
            return;
        }

        lastSaved = Time.time;
        int width = 128;
        int height = 80;

        camera.aspect = 1.0f;
        // recall that the height is now the "actual" size from now on

        RenderTexture tempRT = new RenderTexture(width, height, 24);
        // the 24 can be 0,16,24, formats like
        // RenderTextureFormat.Default, ARGB32 etc.

        camera.targetTexture = tempRT;
        camera.Render();

        RenderTexture.active = tempRT;
        Texture2D virtualPhoto =
            new Texture2D(width, height, TextureFormat.RGB24, false);
        // false, meaning no need for mipmaps
        virtualPhoto.ReadPixels(new Rect(0, 0, width, height), 0, 0);

        RenderTexture.active = null; //can help avoid errors 
        camera.targetTexture = null;
        // consider ... Destroy(tempRT);

        byte[] bytes;
        bytes = virtualPhoto.EncodeToPNG();

        System.IO.File.WriteAllBytes(SCREENSHOT_FILE, bytes);
        // virtualCam.SetActive(false); ... no great need for this.
    }
}
