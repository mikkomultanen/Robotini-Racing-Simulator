using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public abstract class RemoteEventPlayer : MonoBehaviour {
    private readonly Dictionary<string, GameObject> cars = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, CarInfo> carInfos = new Dictionary<string, CarInfo>();

    public void ApplyEvent(GameEvent e)
    {
        if (e is RaceParameters) {
            FindObjectOfType<TrackController>().LoadTrack(((RaceParameters)e).track);
        }
        else if (e is CarAdded)
        {
            CarInfo carInfo = ((CarAdded)e).car;
            carInfos.Add(carInfo.name, carInfo);
            GameObject car;
            if (cars.TryGetValue(carInfo.name, out car)) {
                car.GetComponent<CarAppearanceController>().CarInfo = carInfo;
            }
        }
        else if (e is GameStatus)
        {
            UpdateCars((e as GameStatus).cars);
        }
        EventBus.Publish(e);
    }

    public void UpdateCars(CarStatus[] newStatuses)
    {
        // remove cars that no longer exist
        var carNames = newStatuses.Select(s => s.name);
        var oldNames = cars.Keys.ToList();
        foreach (var n in oldNames) {
            if (!carNames.Contains(n)) {
                Destroy(cars[n]);
                cars.Remove(n);
            }
        }
        foreach (var newStatus in newStatuses) {
            GameObject car;
            if (cars.TryGetValue(newStatus.name, out car)) {
                car.name = newStatus.name;
                car.transform.position = newStatus.position;
                car.transform.rotation = newStatus.rotation;
            }
            else
            {
                var raceController = FindObjectOfType<RaceController>();
                var carPrefab = raceController.carPrefab;
                car = Instantiate(carPrefab, newStatus.position, newStatus.rotation);
                car.name = newStatus.name;

                Debug.Log("Add car to track: " + newStatus.name);
                car.GetComponent<Rigidbody>().isKinematic = true;

                CarInfo carInfo;
                if (carInfos.TryGetValue(newStatus.name, out carInfo)) {
                    car.GetComponent<CarAppearanceController>().CarInfo = carInfo;
                }

                // remove the unnecessary components that actually fails without a RaceController
                foreach (var c in car.GetComponentsInChildren<HudController>()) { Destroy(c); }
                foreach (var c in car.GetComponentsInChildren<CarController>()) { Destroy(c); }

                cars[newStatus.name] = car;
            }
        }
    }
}