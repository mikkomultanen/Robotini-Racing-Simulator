using UnityEngine;
using UniRx;
using System;
using System.Collections.Concurrent;

public class RaceController : MonoBehaviour
{
    public GameObject carPrefab;
    [HideInInspector]
    public bool motorsEnabled = true;
    private readonly ConcurrentQueue<CarSocket> incomingCars = new ConcurrentQueue<CarSocket>();
    private SplineMesh.Spline track;

    private void OnEnable()
    {
        track = FindObjectOfType<SplineMesh.Spline>();

        MessageBroker.Default.Receive<MotorsToggle>().Subscribe(x => {
            Debug.Log("Motors enabled: " + motorsEnabled);
            motorsEnabled = !motorsEnabled;            
        }).AddTo(this);

        MessageBroker.Default.Receive<ResetTimers>().Subscribe(x => {
            Debug.Log("Reset timers");
            FindObjectOfType<LapTimer>().ResetTimers();
            var cars = FindObjectsOfType<CarController>();
            int i = 0;
            foreach (var car in cars)
            {
                var curveSample = track.GetSampleAtDistance(track.Length - (++i * 0.4f));
                car.transform.position = curveSample.location + 0.1f * Vector3.up + curveSample.Rotation * Vector3.right * (i % 2 == 0 ? 1 : -1) * 0.1f;
                car.transform.rotation = curveSample.Rotation;
                car.GetComponent<Rigidbody>().velocity = Vector3.zero;
            }
        }).AddTo(this);
    }

    public void AddCarSocket(CarSocket socket)
    {
        incomingCars.Enqueue(socket);       
    }

    private void Update()
    {
        CarSocket socket = null;
        while (incomingCars.TryDequeue(out socket))
        {
            var curveSample = track.GetSampleAtDistance(0.95f * track.Length);
            var car = Instantiate(carPrefab, curveSample.location + 0.1f * Vector3.up, curveSample.Rotation);
            var carController = car.GetComponent<CarController>();
            carController.SetSocket(socket);
            carController.raceController = this;
            car.name = socket.CarInfo().name;
        }
    }
}

[Serializable]
public class JsonControlCommand
{
    public string action;
    public string move;
    public float value;
}

[Serializable]
public class CarInfo
{
    public string teamId;
    public string name;
    public CarInfo(string teamId, string name)
    {
        this.teamId = teamId;
        this.name = name;
    }
}

public class MotorsToggle
{
}

public class ResetTimers
{
}