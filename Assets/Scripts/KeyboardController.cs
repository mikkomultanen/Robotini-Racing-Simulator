using System.Linq;
using UnityEngine;

public class KeyboardController : MonoBehaviour
{
    private bool useRemoteCameraPosition;
    private readonly KeyCode[] numberKeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9 };

    // Called from js
    void UseCameraPositionsFromStream() {
        useRemoteCameraPosition = true;
    }

    private void Start()
    {
       #if !UNITY_EDITOR && UNITY_WEBGL
        WebGLInput.captureAllKeyboardInput = false;
       #endif
    }

    private void Update()
    {
        if (!useRemoteCameraPosition) {
            for (int i = 0; i < numberKeys.Length; i++) {
                if (Input.GetKeyDown(numberKeys[i]))
                {
                    var cars = FindObjectOfType<LapTimeDisplay>()?.CarInfos;
                    if (cars != null && cars.Length > i)
                    {
                        EventBus.Publish(new CameraFollow(cars[i].name));
                    }
                }
            }
            if (Input.GetKeyDown(KeyCode.Backspace)) {
                EventBus.Publish(new CameraFollow(null));
            }
            if (Input.GetKeyDown(KeyCode.S)) {
                var track = FindObjectOfType<SplineMesh.Spline>();
                var curveSample = track.GetSampleAtDistance(0);
                var position = curveSample.location + 0.4f * Vector3.up;
                curveSample = track.GetSampleAtDistance(track.Length - 0.7f);
                var lookAt = curveSample.location;
                var rotation = Quaternion.LookRotation(lookAt - position, Vector3.up);
                EventBus.Publish(new CameraPosition(position, rotation));
            }
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            EventBus.Publish(new MotorsToggle());
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            EventBus.Publish(new ProceedToNextPhase());
        }
    }
}