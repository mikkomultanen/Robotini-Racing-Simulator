using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

public class PlaybackController : MonoBehaviour
{
    private void OnEnable()
    {
        if (ModeController.Mode == SimulatorMode.Playback)
        {
            Debug.Log("Initializing playback");
            StartCoroutine(GetRaceLog());
        }
    }

    IEnumerator GetRaceLog()
    {

        UnityWebRequest www = UnityWebRequest.Get("http://localhost:8000/race-capture.json");
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
        }
        else
        {
            // Show results as text
            string text = www.downloadHandler.text;

            string[] lines = text.Split('\n');
            GameStatus[] states = lines.Select(line => JsonUtility.FromJson<GameStatus>(line)).ToArray();
            Debug.Log(JsonUtility.ToJson(states[0]));

            GameStatus previous = null;

            GameObject[] cars = { };         

            foreach (GameStatus status in states)
            {
                if (previous != null && previous.cars.Length > 0)
                {
                    float delay = status.timestamp - previous.timestamp;
                    Debug.Log("Waiting " + delay);
                    yield return new WaitForSeconds(delay);
                }

                previous = status;


                cars = UpdateCars(cars, status.cars);

            }
        }
    }

    GameObject[] UpdateCars(GameObject[] cars, CarStatus[] newStatuses)
    {
        // TODO: use some identifiers for cars instead of just indices
        // remove cars that no longer exist
        foreach (var car in cars.Skip(newStatuses.Length)) {
            Destroy(car); 
        }

        cars = cars
            .Zip(newStatuses, (car, newStatus) => {
                car.transform.position = newStatus.position;
                car.transform.rotation = newStatus.rotation;
                return car;
            })
            .Concat(newStatuses
                .Skip(cars.Length)
                .Select((newCarStatus, index) => {
                    var carPrefab = FindObjectOfType<RaceController>().carPrefab;
                    var car = Instantiate(carPrefab, newCarStatus.position, newCarStatus.rotation);
                    // remove the unnecessary componens that actually fails without a RaceController
                    foreach (var c in car.GetComponentsInChildren<CarController>()) { Destroy(c); }
                    foreach (var c in car.GetComponentsInChildren<HudController>()) { Destroy(c); }

                    car.name = "Car" + (index + cars.Length);
                    return car;
                })
            ).ToArray();

        return cars;
    }
}
