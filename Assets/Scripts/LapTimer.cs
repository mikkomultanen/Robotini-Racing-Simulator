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
                    row.transform.Find("TeamName").GetComponent<TextMeshProUGUI>().text = carName;
                    timers[carName].timeListElement = row;
                    row.transform.SetParent(lapTimeList.transform, false);
                    RectTransform rect = row.GetComponent<RectTransform>();
                    rect.localScale = Vector3.one;
                    rect.anchoredPosition = new Vector2(0, -25);
                }
                timers[carName].NewLapTime();
                this.SortTimeList();
            }
            else
            {
                Debug.Log("New car entering race: " + carName);
                timers[carName] = new TimeWrapper();
            }
        }
    }

    private void SortTimeList()
    {
        const float rowHeight = 20;
        const float startHeight = -10;

        List<TimeWrapper> times = new List<TimeWrapper>(timers.Values);
        times.Sort(delegate (TimeWrapper t1, TimeWrapper t2) {
            return t1.bestTime.CompareTo(t2.bestTime);
        });

        int i = 0;
        foreach (TimeWrapper time in times) {
            if (!time.timeListElement)
            {
                continue;
            }
            if (i > 7)
            {
                time.timeListElement.SetActive(false);
                continue;
            }
            time.timeListElement.SetActive(true);

            time.timeListElement.GetComponent<RectTransform>()
            .anchoredPosition = new Vector2(10, startHeight - rowHeight * i++);
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

    public void ResetTimers()
    {
        foreach (var timer in timers.Values)
        { 
            if (timer.timeListElement)
            {
                Destroy(timer.timeListElement);
            }
        }
        timers.Clear();
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