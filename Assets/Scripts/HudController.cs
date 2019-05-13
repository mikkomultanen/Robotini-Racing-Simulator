using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HudController : MonoBehaviour
{
    public GameObject car;
    public UnityEngine.UI.Text speedText;
    public UnityEngine.UI.Text throttleText;
    public UnityEngine.UI.Text turnText;
    public UnityEngine.UI.Text lapText;

    private Rigidbody rigidBody;

    // Start is called before the first frame update
    void Start()
    {
        rigidBody = car.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        var controller = car.GetComponent<CarController>();
        var angle = controller.angle;
        var forward = controller.forward;

        speedText.text = "S " + f(controller.velocity);
        turnText.text = "T " + f(angle);
        if (Mathf.Abs(angle) < Mathf.Epsilon)
        {
            turnText.text = "";
        }
        else if (angle > 0)
        {
            turnText.text = "  " + f(angle) + " >";
        }
        else
        {
            turnText.text = "< " + f(-angle);
        }
        if (controller.brake > Mathf.Epsilon)
        {
            throttleText.text = "BRK";
        }
        else if (Mathf.Abs(forward) < Mathf.Epsilon)
        {
            throttleText.text = "IDLE";
        }
        else if (forward > 0)
        {
            throttleText.text =  "F " + f(forward);
        }
        else
        {
            throttleText.text = "REV";
        }

        if (LapTimer.timers.ContainsKey(car.name))
        {
            var timer = LapTimer.timers[car.name];
            if (timer.lastLapRecordedAt > Time.time - 5)
            {
                if (timer.bestTime < timer.lastTime)
                {
                    lapText.text = "LAP " + TimeWrapper.FormattedTime(timer.lastTime) + " (+" + TimeWrapper.FormattedDiff(timer.lastTime - timer.bestTime) + ")";
                }
                else
                {
                    lapText.text = "LAP " + TimeWrapper.FormattedTime(timer.lastTime);
                }
            }
            else
            {
                lapText.text = "";
            }
        }
    }

    static string f(float f)
    {
        return "" + Mathf.Round(f * 10) / 10;
    }
}
