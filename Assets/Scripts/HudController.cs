using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HudController : MonoBehaviour
{
    public GameObject car;
    public UnityEngine.UI.Text speedText;
    public UnityEngine.UI.Text throttleText;
    public UnityEngine.UI.Text turnText;
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
        if (Mathf.Abs(forward) < Mathf.Epsilon)
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
    }

    static string f(float f)
    {
        return "" + Mathf.Round(f * 10) / 10;
    }
}
