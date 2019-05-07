using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LapTimer : MonoBehaviour
{
    private Dictionary<string, TimeWrapper> timers = new Dictionary<string, TimeWrapper>();

    public void CarHitTrigger(GameObject carObject)
    {
        if (carObject.GetComponent<CarController>())
        {
            string carName = carObject.name;
            Debug.Log("Car hit trigger: " + carName);

            if (timers.ContainsKey(carName))
            {
                NewLapTime(carName, timers[carName]);
                timers[carName].time = 0;
            }
            else
            {
                timers[carName] = new TimeWrapper();
            }
        }
    }

    private void NewLapTime(string carName, TimeWrapper timeWrapper)
    {
        Debug.Log("Lap time for " + carName + ": " + timeWrapper.time);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    void FixedUpdate()
    {
        float stepSize = Time.fixedDeltaTime;
        foreach (string key in timers.Keys)
        {
            timers[key].time += stepSize;
        }
    }

    private class TimeWrapper
    {
        internal float time;
    }
}
