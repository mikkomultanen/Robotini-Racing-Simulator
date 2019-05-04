using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour
{
    public WheelCollider frontLeftWC, frontRightWC, rearLeftWC, rearRightWC;
    public Transform frontLeftT, frontRightT, rearLeftT, rearRightT;
    public float maxSteerAngle = 30;
    public float motorForce = 5000;

    private float angle;
    private float forward;

    public void GetInput()
    {
        angle = Input.GetAxis("Horizontal");
        forward = Input.GetAxis("Vertical");
    }

    private void Steer()
    {
        float steerAngle = maxSteerAngle * angle;
        frontLeftWC.steerAngle = steerAngle;
        frontRightWC.steerAngle = steerAngle;
    }

    private void Accelerate()
    {
        float motorTorgue = forward * motorForce;
        frontLeftWC.motorTorque = motorTorgue;
        frontRightWC.motorTorque = motorTorgue;
        rearLeftWC.motorTorque = motorTorgue;
        rearRightWC.motorTorque = motorTorgue;
    }

    private void UpdateWheelPoses()
    {
        UpdateWheelPose(frontLeftWC, frontLeftT);
        UpdateWheelPose(frontRightWC, frontRightT);
        UpdateWheelPose(rearLeftWC, rearLeftT);
        UpdateWheelPose(rearRightWC, rearRightT);
    }

    private void UpdateWheelPose(WheelCollider wheelCollider, Transform wheelTransform)
    {
        Vector3 pos = wheelTransform.position;
        Quaternion rot = wheelTransform.rotation;

        wheelCollider.GetWorldPose(out pos, out rot);

        wheelTransform.position = pos;
        wheelTransform.rotation = rot;
    }

    private void FixedUpdate()
    {
        GetInput();
        Steer();
        Accelerate();
    }

    private void Update()
    {
        UpdateWheelPoses();
        var commands = BotSocket.ReceiveCommands();
        foreach (var command in commands)
        {
            Debug.Log("Processing " + JsonUtility.ToJson(command));
        }
    }
}