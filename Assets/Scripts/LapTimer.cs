using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LapTimer : MonoBehaviour
{
    public static Dictionary<string, TimeWrapper> timers = new Dictionary<string, TimeWrapper>();

    [SerializeField] private GameObject lapTimeList;
    [SerializeField] private GameObject lapTimeRowPrefab;

    public void ToggleLapTimeList() => lapTimeList?.SetActive(!lapTimeList.activeSelf);

    public void CarHitTrigger(GameObject carObject)
    {
        if (carObject.GetComponent<CarController>())
        {
            string carName = carObject.name;

            if (timers.ContainsKey(carName))
            {
                if (!timers[carName].timeListElement)
                {
                    GameObject row = Instantiate(lapTimeRowPrefab);
                    timers[carName].timeListElement = row;
                    row.transform.Find("TeamName").GetComponent<TextMeshProUGUI>().text = carName;
                    row.transform.parent = lapTimeList.transform;
                    RectTransform rect = row.GetComponent<RectTransform>();
                    rect.localScale = Vector3.one;
                    rect.anchoredPosition = new Vector2(0, -25);
                }
                timers[carName].NewLapTime();
            }
            else
            {
                Debug.Log("New car entering race: " + carName);
                timers[carName] = new TimeWrapper();
            }
        }
    }

    void FixedUpdate()
    {
        float stepSize = Time.fixedDeltaTime;
        foreach (string key in timers.Keys)
        {
            timers[key].time += stepSize;
        }
    }
}

public class TimeWrapper
{
    internal float time;
    internal float bestTime = float.MaxValue;
    internal float lastTime = 0;
    internal float lastLapRecordedAt = 0;
    internal int lapCount;

    internal GameObject timeListElement;

    internal void NewLapTime()
    {
        lapCount++;
        lastTime = time;
        lastLapRecordedAt = Time.time;
        if (time < bestTime)
        {
            bestTime = time;
        }
        time = 0;

        if (timeListElement)
        {
            timeListElement.transform.Find("LastLap").GetComponent<TextMeshProUGUI>().text = FormattedTime(lastTime);
            timeListElement.transform.Find("BestLap").GetComponent<TextMeshProUGUI>().text = FormattedTime(bestTime);
            timeListElement.transform.Find("LapCount").GetComponent<TextMeshProUGUI>().text = lapCount.ToString();
        }
    }

    public static string FormattedTime(float inputTime)
    {
        int minutes = (int)(inputTime / 60);
        int seconds = (int)inputTime % 60;
        return string.Format("{0:00}:{1:00}{2:.000}", minutes, seconds, inputTime - Mathf.Floor(inputTime));
    }

    public static string FormattedDiff(float inputTime)
    {
        int seconds = (int)inputTime % 60;
        return string.Format("{0:0}{1:.000}", seconds, inputTime - Mathf.Floor(inputTime));
    }
}