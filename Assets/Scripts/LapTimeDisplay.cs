using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LapTimeDisplay : MonoBehaviour
{
    Dictionary<string, TimeWrapper> timers = new Dictionary<string, TimeWrapper>();
    [SerializeField] private GameObject lapTimeList;
    [SerializeField] private GameObject lapTimeRowPrefab;

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
        EventBus.Subscribe<CarConnected>(this, e => {
            addCar(e.car);
            this.SortTimeList();
        });

        EventBus.Subscribe<LapCompleted>(this, lap =>
        {
            string carName = lap.car.name;
            addCar(lap.car);
            timers[carName].LapCompleted(lap);
            SortTimeList();
        });

        EventBus.Subscribe<CurrentStandings>(this, standings => {
            var index = 0;
            foreach (var s in standings.standings)
            {
                addCar(s.car);
                timers[s.car.name].Standing = index;
                index++;
            }
            SortTimeList();
        });

        EventBus.Subscribe<StartingGridInit>(this, grid => {
            foreach (var timer in timers.Values) {
                timer.OnRemove();
            }
            timers.Clear();

            foreach (var car in grid.cars) {
                addCar(car);
            }

            SortTimeList();
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

    private void addCar(CarInfo car)
    {
        string carName = car.name;
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
    }

    public void ToggleLapTimeList() => lapTimeList?.SetActive(!lapTimeList.activeSelf);

    private int compareTimes(TimeWrapper t1, TimeWrapper t2)
    {
        return t1.Standing - t2.Standing;
    }

    private void SortTimeList()
    {
        const float rowHeight = 20;
        const float startHeight = -10;

        List<TimeWrapper> times = new List<TimeWrapper>(timers.Values);
        times.Sort(compareTimes);

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
        public int Standing = 0;
        internal LapCompleted lap;
        internal GameObject timeListElement;

        public TimeWrapper(GameObject row)
        {
            this.timeListElement = row;
            setTexts("", "", "");
        }

        internal void LapCompleted(LapCompleted lap)
        {
            this.lap = lap;
            if (timeListElement)
            {
                setTexts(FormattedTime(lap.lastLap), FormattedTime(lap.bestLap), lap.lapCount.ToString());
            }
        }
        private void setTexts(string lastLap, string bestLap, string lapCount)
        {
            setText("LastLap", lastLap);
            setText("BestLap", bestLap);
            setText("LapCount", lapCount);
        }
        private void setText(string name, string text)
        {
            timeListElement.transform.Find(name).GetComponent<TextMeshProUGUI>().text = text;
        }
        public void OnRemove()
        {
            Destroy(timeListElement);
        }
    }
}