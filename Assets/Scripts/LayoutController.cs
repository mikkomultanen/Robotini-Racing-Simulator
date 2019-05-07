using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LayoutController : MonoBehaviour
{
    public List<Camera> Cameras;

    private int mainCameraIndex = 0;

    // Start is called before the first frame update
    void Start()
    {  
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CycleCamera();
        }

        if (MainCamera != null)
            MainCamera.rect = new Rect(0f, 0.3f, 1.0f, 0.7f); 

        var index = 0;
        var otherCameraCount = Cameras.Count - 1;
        foreach (var camera in OtherCameras) {
            camera.rect = new Rect(index * 1.0f/otherCameraCount, 0.0f, 1.0f/otherCameraCount, 0.3f);
            index++;
        }
    }

    private void CycleCamera() => mainCameraIndex = (mainCameraIndex + 1) % Cameras.Count;

    private Camera MainCamera => mainCameraIndex < Cameras.Count ? Cameras[mainCameraIndex] : null;

    private IEnumerable<Camera> OtherCameras 
    {
        get
        {
            for (var i = 1; i < Cameras.Count; i++)
            {
                yield return Cameras[(mainCameraIndex + i) % Cameras.Count];
            }
        }
    }
}
