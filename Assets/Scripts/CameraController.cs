using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    public Vector3 offset = Vector3.up * 0.1f;
    public Vector3 offsetRotation = Vector3.zero;
    [Range(0.1f,1f)]
    public float size = 0.2f;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private CameraOutputController follow = null;
    private int nextCameraIndex = 0;

    void Start()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
    }

    void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.C)) {
            var cameras = FindObjectsOfType<CameraOutputController>();
            if (nextCameraIndex >= cameras.Length)
            {
                follow = null;
                transform.position = originalPosition;
                transform.rotation = originalRotation;
            }
            else {
                follow = cameras[nextCameraIndex];
            }
            nextCameraIndex = (nextCameraIndex + 1) % (cameras.Length + 1);
        }
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
