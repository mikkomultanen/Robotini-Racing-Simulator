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
    private Vector3 oldPosition;
    private Quaternion oldRotation;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float t = 0;

    private void SetFollow(CameraOutputController value) {
        if (follow != null)
        {
            follow.Camera.enabled = CameraOutputController.ShouldEnableCamera;
        }
        else
        {
            oldPosition = transform.position;
            oldRotation = transform.rotation;
            t = 0;
        }
        follow = value;
        if (value != null)
        {
            value.Camera.enabled = true;
        }
        else
        {
            SetPosition(originalPosition, originalRotation);
        }        
    }

    private void SetPosition(Vector3 position, Quaternion rotation) {
        if (follow != null)
        {
            follow.Camera.enabled = CameraOutputController.ShouldEnableCamera;
        }
        follow = null;
        oldPosition = transform.position;
        oldRotation = transform.rotation;
        targetPosition = position;
        targetRotation = rotation;
        t = 0;
    }

    void Start()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        oldPosition = originalPosition;
        oldRotation = originalRotation;
        targetPosition = originalPosition;
        targetRotation = originalRotation;

        EventBus.Subscribe<CameraFollow>(this, f => {
            if (f.carName == null)
            {
                SetFollow(null);
            }
            else
            {
                var car = GameObject.Find(f.carName);
                var camera = car?.GetComponentInChildren<CameraOutputController>();
                SetFollow(camera);
            }           
        });
        EventBus.Subscribe<CameraPosition>(this, p => {
            SetPosition(p.position, p.rotation);
        });
    }

    void LateUpdate()
    {
        if (follow != null)
        {
            targetPosition = follow.transform.position + follow.transform.TransformVector(offset);
            targetRotation = follow.transform.rotation * Quaternion.Euler(offsetRotation);
        }
        transform.position = Vector3.Lerp(oldPosition, targetPosition, t);
        transform.rotation = Quaternion.Lerp(oldRotation, targetRotation, t);
        t = Mathf.Clamp01((t + Time.deltaTime) / 0.8f);
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
