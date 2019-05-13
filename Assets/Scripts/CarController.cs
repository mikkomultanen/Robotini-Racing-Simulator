using System.Collections.Generic;
using System.Collections.Concurrent;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

[Serializable]
public class JsonControlCommand
{
    public string action;
    public string move;
    public float value;
}

public class CarController : MonoBehaviour
{
    public WheelCollider frontLeftWC, frontRightWC, rearLeftWC, rearRightWC;
    public Transform frontLeftT, frontRightT, rearLeftT, rearRightT;
    public float maxSteerAngle = 30;
    public float motorForce = 50;
    public float maxAngleChangePerSecond = 10;
    [HideInInspector]
    public float velocity;
    [HideInInspector]
    public float angle;
    [HideInInspector]
    public float forward;
    private float targetAngle = 0;
    private float lastBotCommandTime = 0;
    private Rigidbody rigidBody;
    private readonly ConcurrentQueue<JsonControlCommand> commandQueue = new ConcurrentQueue<JsonControlCommand>();
    private volatile Socket socket;

    private void OnEnable()
    {
        rigidBody = GetComponent<Rigidbody>();
    }

    public void GetInput()
    {
        if (Time.time > lastBotCommandTime + 0.3)
        {
            // 300ms pause in bot commands -> return to manual control
            angle = Input.GetAxis("Horizontal");
            forward = Input.GetAxis("Vertical");
        }
        else
        {
            var angleDelta = targetAngle - angle;
            angle += Mathf.Sign(angleDelta) * Mathf.Min(maxAngleChangePerSecond * Time.fixedDeltaTime, Mathf.Abs(angleDelta));
        }
        //Debug.Log("angle " + angle + " forward " + forward);
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
        ProcessBotCommands();
        velocity = Vector3.Dot(rigidBody.transform.forward, rigidBody.velocity);
    }

    private void OnDestroy()
    {
        if (this.socket != null) 
        {
            this.socket.Dispose();
            this.socket = null;
        }
    }

    public void SetSocket(Socket socket)
    {
        if (this.socket != null) return;

        this.socket = socket;

        var cameraOutput = GetComponentInChildren<CameraOutputController>();
        cameraOutput.SetSocket(socket);

        Boolean stopped = false;

        new Thread(() => {
            var stream = new NetworkStream(socket);
            var reader = new StreamReader(stream);
            while (this.socket != null && !stopped)
            {
                try
                {
                    var line = reader.ReadLine();
                    var command = JsonUtility.FromJson<JsonControlCommand>(line);
                    commandQueue.Enqueue(command);
                }
                catch (Exception e)
                {
                    Debug.Log("Socket read failed:" + e.ToString());
                    stopped = true;
                }
            }
            commandQueue.Enqueue(new JsonControlCommand {
                action = "disconnected"
            });
        }).Start();
    }

    private void ProcessBotCommands()
    {
        foreach (var command in ReceiveCommands())
        {
            lastBotCommandTime = Time.time;
            //Debug.Log("Processing " + JsonUtility.ToJson(command));
            if (command.action == "forward")
            {
                forward = command.value;
            }
            else if (command.action == "reverse")
            {
                if (velocity <= 0.01)
                {
                    forward = -command.value;
                } else
                {
                    forward = 0;
                }
            }
            else if (command.action == "turn")
            {
                targetAngle = -command.value; // bot uses -1 right, +1 left
            }
            else if (command.action == "disconnected")
            {
                Destroy(gameObject);
            }
        }
    }

    private IEnumerable<JsonControlCommand> ReceiveCommands()
    {
        var commands = new List<JsonControlCommand>();
        JsonControlCommand command = null;
        while (commandQueue.TryDequeue(out command))
        {
            commands.Add(command);
        }
        return commands;
    }
}