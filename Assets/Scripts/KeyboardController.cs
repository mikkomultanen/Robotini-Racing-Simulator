using UnityEngine;
using UniRx;

public class KeyboardController : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            MessageBroker.Default.Publish(new MotorsToggle());
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            MessageBroker.Default.Publish(new ResetTimers());
        }
    }
}