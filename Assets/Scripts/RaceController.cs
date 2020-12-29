using UnityEngine;
using System;
using System.Collections.Generic;

public class RaceController : MonoBehaviour
{
    public GameObject carPrefab;
    [HideInInspector]
    public bool motorsEnabled = true;
    private SplineMesh.Spline track;
    private Dictionary<string, CarStatus> cars = new Dictionary<string, CarStatus>();

    private void OnEnable()
    {
        track = FindObjectOfType<SplineMesh.Spline>();

        EventBus.Subscribe<MotorsToggle>(this, x => {
            Debug.Log("Motors enabled: " + motorsEnabled);
            motorsEnabled = !motorsEnabled;            
        });

        EventBus.Subscribe<ResetTimers>(this, x => {
            Debug.Log("Reset timers");
            FindObjectOfType<LapTimeDisplay>().ResetTimers();
            var cars = FindObjectsOfType<CarController>();
            int i = 0;
            foreach (var car in cars)
            {
                var curveSample = track.GetSampleAtDistance(track.Length - (++i * 0.4f));
                car.transform.position = curveSample.location + 0.1f * Vector3.up + curveSample.Rotation * Vector3.right * (i % 2 == 0 ? 1 : -1) * 0.1f;
                car.transform.rotation = curveSample.Rotation;
                car.GetComponent<Rigidbody>().velocity = Vector3.zero;
            }
        });

        EventBus.Subscribe<CarConnected>(this, e =>
        {
            if (cars.ContainsKey(e.car.name))
            {
                throw new Exception("TODO: duplicate car");
            }

            var curveSample = track.GetSampleAtDistance(0.95f * track.Length);
            var car = Instantiate(carPrefab, curveSample.location + 0.1f * Vector3.up, curveSample.Rotation);
            var carController = car.GetComponent<CarController>();
            carController.SetSocket(e.socket);
            carController.raceController = this;
            car.name = e.car.name;
            Debug.Log("Add Car '" + e.car.name + "'");
            cars[e.car.name] = new CarStatus(e.car);
        });

        EventBus.Subscribe<CarDisconnected>(this, e =>
        {
            EventBus.Publish(new CarRemoved(e.car));
            // TODO keep it as a DNF car in race mode
            
        });

        EventBus.Subscribe<CarRemoved>(this, e =>
        {
            Debug.Log("Remove Car " + e.car.name);
            cars.Remove(e.car.name);
        });
    }

    public void CarHitTrigger(GameObject car)
    {
        var name = car.name;
        Debug.Log("Lap Time for '" + name + "'");
        cars[name].NewLapTime();
    }


    class CarStatus
    {
        internal float bestTime = float.MaxValue;
        internal float lastTime = 0;
        internal float lastLapRecordedAt = Time.time;
        internal int lapCount = -1; // Not even started first lap yet, 0 would be running first lap.
        readonly CarInfo CarInfo;

        public CarStatus(CarInfo carInfo)
        {
            this.CarInfo = carInfo;
        }

        internal void NewLapTime()
        {
            var now = Time.time;
            lapCount++;
            if (lapCount == 0)
            {
                // crossed finish line, starting first lap.
                lastLapRecordedAt = now;
                return;
            }
            var lastTime = now - lastLapRecordedAt;
            lastLapRecordedAt = now;
            if (lastTime < bestTime)
            {
                bestTime = lastTime;
            }
            EventBus.Publish(new LapCompleted(CarInfo, lapCount, lastTime, bestTime));
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