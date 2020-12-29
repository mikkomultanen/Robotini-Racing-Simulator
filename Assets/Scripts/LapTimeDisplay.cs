using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LapTimeDisplay : MonoBehaviour
{
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

    public void OnEnable()
    {
        EventBus.Subscribe<LapCompleted>(this, lap =>
        {
            string carName = lap.car.name;
            if (!timers.ContainsKey(carName))
            {
                GameObject row = Instantiate(lapTimeRowPrefab);
                row.transform.Find("TeamName").GetComponent<TextMeshProUGUI>().text = carName;
                timers[carName] = new TimeWrapper(row);
                row.transform.SetParent(lapTimeList.transform, false);
                RectTransform rect = row.GetComponent<RectTransform>();
                rect.localScale = Vector3.one;
                rect.anchoredPosition = new Vector2(0, -25);
            }
            timers[carName].LapCompleted(lap);
            this.SortTimeList();
        });

        EventBus.Subscribe<CarRemoved>(this, e =>
        {
            string key = e.car.name;
            if (timers.ContainsKey(key))
            {
                timers[key].OnRemove();
                timers.Remove(key);
            }
        });
    }

    Dictionary<string, TimeWrapper> timers = new Dictionary<string, TimeWrapper>();
    [SerializeField] private GameObject lapTimeList;
    [SerializeField] private GameObject lapTimeRowPrefab;

    public void ToggleLapTimeList() => lapTimeList?.SetActive(!lapTimeList.activeSelf);

    private void SortTimeList()
    {
        const float rowHeight = 20;
        const float startHeight = -10;

        List<TimeWrapper> times = new List<TimeWrapper>(timers.Values);
        times.Sort(delegate (TimeWrapper t1, TimeWrapper t2) {
            return t1.lap.bestLap.CompareTo(t2.lap.bestLap);
        });

        int i = 0;
        foreach (TimeWrapper time in times)
        {
            time.timeListElement.SetActive(true);
            time.timeListElement.GetComponent<RectTransform>()
                .anchoredPosition = new Vector2(10, startHeight - rowHeight * i++);
        }
    }

    public void ResetTimers()
    {
        foreach (var timer in timers.Values)
        {
            timer.OnRemove();
        }
        timers.Clear();
    }

    public class TimeWrapper
    {
        internal LapCompleted lap;
        internal GameObject timeListElement;

        public TimeWrapper(GameObject row)
        {
            this.timeListElement = row;
        }

        internal void LapCompleted(LapCompleted lap)
        {
            this.lap = lap;
            if (timeListElement)
            {
                timeListElement.transform.Find("LastLap").GetComponent<TextMeshProUGUI>().text = FormattedTime(lap.lastLap);
                timeListElement.transform.Find("BestLap").GetComponent<TextMeshProUGUI>().text = FormattedTime(lap.bestLap);
                timeListElement.transform.Find("LapCount").GetComponent<TextMeshProUGUI>().text = lap.lapCount.ToString();
            }
        }
        public void OnRemove()
        {
            Destroy(timeListElement);
        }
    }
}