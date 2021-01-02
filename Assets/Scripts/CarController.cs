using System.Collections.Generic;
using System.Collections.Concurrent;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UniRx;

public class CarController : MonoBehaviour
{
    public WheelCollider frontLeftWC, frontRightWC, rearLeftWC, rearRightWC;
    public Transform frontLeftT, frontRightT, rearLeftT, rearRightT;
    public float maxSteerAngle = 30;
    public float motorForce = 50;
    public float brakeForce = 100;
    public float maxAngleChangePerSecond = 10;
    [HideInInspector]
    public float velocity;
    [HideInInspector]
    public float angle;
    [HideInInspector]
    public float forward;
    [HideInInspector]
    public float brake;
    [HideInInspector]
    public RaceController raceController;
    private float targetAngle = 0;
    private float lastBotCommandTime = 0;
    private bool finished = false;
    public Rigidbody rigidBody;
    
    private volatile CarSocket socket;
    private WheelCollider[] allWheels;
    private DateTime collidingSince;

    private void OnEnable()
    {
        rigidBody = GetComponent<Rigidbody>();
        allWheels = new WheelCollider[] { frontLeftWC, frontRightWC, rearLeftWC, rearRightWC };
        EventBus.Subscribe<CarFinished>(this, f => {
            if (f.car.name == CarInfo.name)
            {
                this.finished = true;
                Observables.Delay(TimeSpan.FromSeconds(1)).Subscribe(_ => { Destroy(gameObject); });
            }
        });
    }

    public void GetInput()
    {
        if (Time.time > lastBotCommandTime + 0.3)
        {
            // 300ms pause in bot commands -> return to manual control
            angle = Input.GetAxis("Horizontal");
            forward = Input.GetAxis("Vertical");
            brake = Input.GetKey(KeyCode.Space) ? 1 : 0;
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

    private bool motorEnabled()
    {
        return raceController.motorsEnabled && !finished;
    }

    private void Accelerate()
    {
        if (!motorEnabled()) {
            foreach (WheelCollider wheel in allWheels)
            {
                wheel.motorTorque = 0;
                wheel.brakeTorque = 0;
            }
        }
        else if (brake == 0)
        {
            float motorTorque = forward * motorForce;
            foreach (WheelCollider wheel in allWheels)
            {
                wheel.motorTorque = motorTorque;
                wheel.brakeTorque = 0;
            }
        }
        else
        {
            float brakeTorque = brake * brakeForce;
            foreach (WheelCollider wheel in allWheels)
            {
                wheel.motorTorque = 0;
                wheel.brakeTorque = brakeTorque;
            }
        }
        if (finished) {
            rigidBody.AddForce(new Vector3(0, 10, 0));
        }
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

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponent<CarController>() != null) return; // ignore if colliding with other car
        this.collidingSince = System.DateTime.Now;   
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.GetComponent<CarController>() != null) return; // ignore if colliding with other car
        float collidingFor = GameEvent.TimeDiff(System.DateTime.Now, this.collidingSince);
        if (collidingFor >= 2) {
            Debug.Log("Returning car to track: " + CarInfo.name);
            var track = FindObjectOfType<RaceController>().track;
            SplineMesh.CurveSample closest = null;
            float closestDistance = float.MaxValue;
            for (float i = 0; i < track.Length; i += 0.05f) {
                var curveSample = track.GetSampleAtDistance(i);
                var distance = (gameObject.transform.position - curveSample.location).magnitude;
                if (distance < closestDistance) {
                    closestDistance = distance;
                    closest = curveSample;
                }
            }

            gameObject.transform.position = closest.location + 0.1f * Vector3.up;
            gameObject.transform.rotation = closest.Rotation;
        }
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
            this.socket = null;
        }
    }

    public void SetSocket(CarSocket socket) {
        this.socket = socket;
        var cameraOutput = GetComponentInChildren<CameraOutputController>();
        cameraOutput.SetSocket(socket);        
    }

    public CarInfo CarInfo { get
        {
            return socket.CarInfo;
        }
    }

    private void ProcessBotCommands()
    {
        if (socket == null) return;
        if (!socket.IsConnected())
        {
            Destroy(gameObject);
        }
        foreach (var command in socket.ReceiveCommands())
        {
            lastBotCommandTime = Time.time;
            //Debug.Log("Processing " + JsonUtility.ToJson(command));
            if (command.action == "forward")
            {
                brake = 0;
                forward = command.value;
            }
            else if (command.action == "reverse")
            {
                brake = 0;
                forward = 0;
            }
            else if (command.action == "brake")
            {
                forward = 0;
                brake = command.value;
            }
            else if (command.action == "turn")
            {
                targetAngle = -command.value; // bot uses -1 right, +1 left
            }
        }
    }


}