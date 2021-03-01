using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using System.IO;

public class FPSLogger : MonoBehaviour
{   
    class Stat {
        public string name;
        private int count = 0;
        public Stat(string name) {
            this.name = name;
        }
        public void inc() {
            count++;
        }

        public int pop() {
            int countWas = count;
            count = 0;
            return countWas;
        }
    }

    Stat frameRender = new Stat("FPS");
    private Dictionary<string, Stat> carFrameSent = new Dictionary<string, Stat>();
    public float valuesInterval = 1;
    public float previousValues;
    public float headersInterval = 10;
    public float previousHeaders;

    void Start() {

    }

    private void Update()
    {
        LogFrameRendered();
        var now = Time.time;
        if (now > previousValues + valuesInterval) {
            var allStats = (new[] { frameRender }).Concat(carFrameSent.Values);
            var colWidth = 12;

            if (now > previousHeaders + headersInterval) {
                var headers = String.Join(" ", allStats.Select(stat => { return stat.name.Substring(0, Math.Min(colWidth, stat.name.Length)).PadRight(colWidth); }));
                Debug.Log(headers);
                previousHeaders = now;
            }
            var values = String.Join(" ", allStats.Select(stat => { return stat.pop().ToString().PadRight(colWidth); }));
            Debug.Log(values);
            previousValues = now;
        }
    }

    public void LogFrameSent(CarInfo car) {
        if (!carFrameSent.ContainsKey(car.name)) {
            carFrameSent[car.name] = new Stat(car.name);
        }
        Stat carStat = carFrameSent[car.name];
        carStat.inc();
    }

    public void LogFrameRendered() {
        frameRender.inc();
    }
}