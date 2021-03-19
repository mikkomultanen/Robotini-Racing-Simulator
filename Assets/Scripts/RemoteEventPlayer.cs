using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;

public abstract class RemoteEventPlayer : MonoBehaviour {
    private GameObject[] cars = { };
    private Dictionary<string, CarInfo> carInfos = new Dictionary<string, CarInfo>();

    public void ApplyEvent(GameEvent e)
    {
        if (e is RaceParameters) {
            FindObjectOfType<TrackController>().LoadTrack(((RaceParameters)e).track);
        }
        else if (e is CarAdded)
        {
            CarInfo car = ((CarAdded)e).car;
            carInfos.Add(car.name, car);
        }
        else if (e is GameStatus)
        {
            cars = UpdateCars(cars, (e as GameStatus).cars);
        }
        EventBus.Publish(e);
    }

    GameObject[] UpdateCars(GameObject[] cars, CarStatus[] newStatuses)
    {
        // remove cars that no longer exist
        foreach (var car in cars.Skip(newStatuses.Length)) {
            Destroy(car); 
        }

        cars = cars
            .Zip(newStatuses, (car, newStatus) => {
                car.name = newStatus.name;
                car.transform.position = newStatus.position;
                car.transform.rotation = newStatus.rotation;
                return car;
            })
            .Concat(newStatuses
                .Skip(cars.Length)
                .Select((newCarStatus, index) => {
                    var raceController = FindObjectOfType<RaceController>();
                    var carPrefab = raceController.carPrefab;
                    var car = Instantiate(carPrefab, newCarStatus.position, newCarStatus.rotation);
                    car.name = newCarStatus.name;

                    Debug.Log("Add car to track: " + newCarStatus.name);
                    var carInfo = carInfos[newCarStatus.name];

                    // TODO: terrible hack
                    car.GetComponent<Rigidbody>().mass = 10000;

                    car.GetComponent<CarAppearanceController>().CarInfo = carInfo;
                    // remove the unnecessary components that actually fails without a RaceController
                    foreach (var c in car.GetComponentsInChildren<BoxCollider>()) { Destroy(c); }                    
                    foreach (var c in car.GetComponentsInChildren<HudController>()) { Destroy(c); }
                    foreach (var c in car.GetComponentsInChildren<CarController>()) { Destroy(c); }
                    
                    return car;
                })
            ).ToArray();

        return cars;
    }
}