using UnityEngine;
using UniRx;

public class KeyboardController : MonoBehaviour
{
    private int nextCameraIndex = 0;
    private bool useRemoteCameraPosition;

    // Called from js
    void UseCameraPositionsFromStream() {
        useRemoteCameraPosition = true;
    }

    private void Update()
    {
        if (!useRemoteCameraPosition) {
            if (Input.GetKeyDown(KeyCode.C)) {
                var cars = GameObject.FindGameObjectsWithTag("Car");
                EventBus.Publish(
                    new CameraFollow(nextCameraIndex >= cars.Length ? null : cars[nextCameraIndex].name)
                );                
                nextCameraIndex = (nextCameraIndex + 1) % (cars.Length + 1);
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