using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LapTimeDisplay : MonoBehaviour
{
    Dictionary<string, TimeWrapper> timers = new Dictionary<string, TimeWrapper>();
    [SerializeField] private GameObject lapTimeList;
    [SerializeField] private GameObject lapTimeRowPrefab;

    public static string FormattedTime(float inputTime)
    {
        if (inputTime == 0) return "";
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

        EventBus.Subscribe<CurrentStandings>(this, standings => {
            var index = 0;
            foreach (var s in standings.standings)
            {
                addCar(s.car);                
                timers[s.car.name].SetLap(standings, index);
                index++;
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
        EventBus.Subscribe<ResetSimulator>(this, e => {
            ResetTimers();
        });
    }

    private void addCar(CarInfo car)
    {
        string carName = car.name;
        if (!timers.ContainsKey(carName))
        {
            GameObject row = Instantiate(lapTimeRowPrefab);
            row.transform.SetParent(lapTimeList.transform, false);
            timers[carName] = new TimeWrapper(row, car);            
        }
    }

    public void ToggleLapTimeList() => lapTimeList?.SetActive(!lapTimeList.activeSelf);

    public CarInfo[] CarInfos {
        get {
            List<TimeWrapper> times = new List<TimeWrapper>(timers.Values);
            times.Sort(compareTimes);
            return times.Select(e => e.car).ToArray();
        } 
    }

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
        internal GameObject timeListElement;
        internal CarInfo car;

        public TimeWrapper(GameObject row, CarInfo car)
        {
            this.car = car;
            this.timeListElement = row;
            setTexts("", "", "", "");            
            RectTransform rect = row.GetComponent<RectTransform>();
            rect.localScale = Vector3.one;
            rect.anchoredPosition = new Vector2(0, -25);

        }

        internal void SetLap(CurrentStandings standings, int index)
        {
            this.Standing = index;
            var lap = standings.standings[index];
            if (lap.dnf)
            {
                setTexts("", "", "", "DNF");
            }
            else if (lap.lapCount <= 0)
            {
                setTexts("", "", "", "");
                return;
            }
            else
            {
                var totalTime = "";
                if (!standings.qualifying)
                {
                    if (index == 0)
                    {
                        totalTime = FormattedTime(lap.totalTime);
                    }
                    else
                    {
                        var leader = standings.standings[0];
                        if (leader.totalTime > lap.totalTime)
                        {
                            // This lap's diff not determined yet
                            totalTime = "";
                        }
                        else if (leader.lapCount == lap.lapCount)
                        {
                            totalTime = "+" + FormattedDiff(lap.totalTime - leader.totalTime);
                        }
                        else
                        {
                            var lapDiff = leader.lapCount - lap.lapCount;
                            totalTime = lapDiff + " lap" + (lapDiff > 1 ? "s" : "");
                        }
                    }
                }
                setTexts(FormattedTime(lap.lastLap), FormattedTime(lap.bestLap), lap.lapCount.ToString(), totalTime);
            }            
        }
        private void setTexts(string lastLap, string bestLap, string lapCount, string totalTime)
        {
            string[] texts = { car.name, lastLap, bestLap, lapCount, totalTime };

            var t = timeListElement.transform;
            var count = System.Math.Min(texts.Length, t.childCount);
            for (var i = 0; i < count; i++)
            {
                var c = t.GetChild(i);                
                c.GetComponent<TextMeshProUGUI>().text = texts[i];
            }
            t.GetComponentInChildren<Image>().color = car.GetColor();
        }

        public void OnRemove()
        {
            Destroy(timeListElement);
        }
    }
}