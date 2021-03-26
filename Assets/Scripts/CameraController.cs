using UnityEngine;
using System.Linq;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    public Vector3 offset = Vector3.up * 0.1f;
    public Vector3 offsetRotation = Vector3.zero;
    [Range(0.1f, 1f)]
    public float size = 0.2f;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private CameraOutputController follow = null;    

    private void SetFollow(CameraOutputController value) {
        if (follow != null)
        {
            follow.Camera.enabled = CameraOutputController.ShouldEnableCamera;
        }
        follow = value;
        if (value != null)
        {
            value.Camera.enabled = true;
        }
        else
        {
            transform.position = originalPosition;
            transform.rotation = originalRotation;
        }        
    }

    void Start()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        EventBus.Subscribe<CameraFollow>(this, f => {
            if (f.carName == null)
            {
                SetFollow(null);
            } else {
                var car = GameObject.Find(f.carName);
                var camera = car?.GetComponentInChildren<CameraOutputController>();
                SetFollow(camera);
            }           
        });
    }

    void LateUpdate()
    {
        if (follow != null) {

            transform.position = follow.transform.position + follow.transform.TransformVector(offset);
            transform.rotation = follow.transform.rotation * Quaternion.Euler(offsetRotation);
        }
    }

    private void OnGUI()
    {
        if (follow != null) {
            var width = Screen.width * size;
            var height = width * CarSocket.IMAGE_HEIGHT / CarSocket.IMAGE_WIDTH;
            Graphics.DrawTexture(new Rect(0, Screen.height - height, width, height), follow.RenderTexture);
        }
    }
}
