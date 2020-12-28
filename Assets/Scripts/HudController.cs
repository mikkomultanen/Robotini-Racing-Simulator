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
    private LapCompleted lap;

    // Start is called before the first frame update
    void Start()
    {
        rigidBody = car.GetComponent<Rigidbody>();
        EventBus.Subscribe<LapCompleted>(this, newLap => {
            if (newLap.car.name == car.name)
            {
                lap = newLap;
            }
        });
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
            throttleText.color = Color.red;
            throttleText.text = "BRK";
        }
        else if (Mathf.Abs(forward) < Mathf.Epsilon)
        {
            throttleText.color = Color.blue;
            throttleText.text = "IDLE";
        }
        else if (forward > 0)
        {
            throttleText.color = Color.green;
            throttleText.text =  "F " + f(forward);
        }
        else
        {
            throttleText.color = Color.red;
            throttleText.text = "REV";
        }

        if (lap != null)
        {
            if (lap.bestLap < lap.lastLap)
            {
                lapText.text = "LAP " + LapTimeDisplay.FormattedTime(lap.lastLap) + " (+" + LapTimeDisplay.FormattedDiff(lap.lastLap - lap.bestLap) + ")";
            }
            else
            {
                lapText.text = "LAP " + LapTimeDisplay.FormattedTime(lap.lastLap);
            }
        }
        else
        {
            lapText.text = "";
        }
    }

    static string f(float f)
    {
        return "" + Mathf.Round(f * 10) / 10;
    }
}
