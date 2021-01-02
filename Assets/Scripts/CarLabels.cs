using UnityEngine;
using System.Collections;
using System.Linq;
using TMPro;

public class CarLabels : MonoBehaviour
{
    public GameObject labelPrefab;
    public Vector3 offset = Vector3.up;    // Units in world space to offset; 1 unit above object by default
    public bool clampToScreen = false;  // If true, label will be visible even if object is off screen
    public float clampBorderSize = 0.05f;  // How much viewport space to leave at the borders when a label is being clamped
    public bool useMainCamera = true;   // Use the camera tagged MainCamera
    public Camera cameraToUse;   // Only use this if useMainCamera is false
    Camera cam;    
    Transform camTransform;

    void Start()
    {        
        if (useMainCamera)
            cam = Camera.main;
        else
            cam = cameraToUse;
        camTransform = cam.transform;
    }

  
    // Update is called once per frame
    void Update()
    {
        var cars = GameObject.FindGameObjectsWithTag("Car");

        for (int i = 0; i < transform.childCount; i++)
        {
            var label = transform.GetChild(0);
            if (cars.Where(c => c.name == label.name).ToArray().Length == 0)
            {
                Destroy(label.gameObject);
            }
        }

        foreach (var car in cars)
        {
            var label = transform.Find(car.name);
            if (label == null)
            {
                var labelObject = Instantiate(labelPrefab);
                labelObject.transform.SetParent(transform, false);
                labelObject.name = car.name;
                labelObject.GetComponent<TextMeshProUGUI>().text = car.name;
                label = labelObject.transform;
            }
            UpdateLabel(car, label);
        }

    }


    void UpdateLabel(GameObject car, Transform labelTransform)
    {
        if (clampToScreen)
        {
            Vector3 relativePosition = camTransform.InverseTransformPoint(car.transform.position + offset);
            relativePosition.z = Mathf.Max(relativePosition.z, 1.0f);
            labelTransform.position = cam.WorldToViewportPoint(camTransform.TransformPoint(relativePosition));
            labelTransform.position = new Vector3(Mathf.Clamp(labelTransform.position.x, clampBorderSize, 1.0f - clampBorderSize),
                                             Mathf.Clamp(labelTransform.position.y, clampBorderSize, 1.0f - clampBorderSize),
                                             labelTransform.position.z);

        }
        else
        {
            
            labelTransform.position = cam.WorldToScreenPoint(car.transform.position + offset);
        }
    }

}
