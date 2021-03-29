using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public abstract class RemoteEventPlayer : MonoBehaviour {
    private readonly Dictionary<string, GameObject> cars = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, CarInfo> carInfos = new Dictionary<string, CarInfo>();

    public void ApplyEvent(GameEvent e)
    {
        if (e is RaceParameters raceParameters)
        {
            FindObjectOfType<TrackController>().LoadTrack(raceParameters.track);
        }
        else if (e is CarAdded carAdded)
        {
            CarInfo carInfo = carAdded.car;
            carInfos.Add(carInfo.name, carInfo);
            if (cars.TryGetValue(carInfo.name, out var car)) {
                car.GetComponent<CarAppearanceController>().CarInfo = carInfo;
            }
        }
        else if (e is GameStatus gameStatus)
        {
            UpdateCars(gameStatus.cars);
        }
        EventBus.Publish(e);
    }

    void UpdateCars(CarStatus[] newStatuses)
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
            if (cars.TryGetValue(newStatus.name, out var car)) {
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

                if (carInfos.TryGetValue(newStatus.name, out var carInfo)) {
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