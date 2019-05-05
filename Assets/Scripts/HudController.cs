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
        var angle = -controller.angle;
        var forward = controller.forward;

        speedText.text = "S " + f(rigidBody.velocity.magnitude);
        turnText.text = "T " + f(angle);
        throttleText.text = "F " + f(forward) ;
    }

    static string f(float f)
    {
        return "" + Mathf.Round(f * 10) / 10;
    }
}
