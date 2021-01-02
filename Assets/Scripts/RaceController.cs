using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using System.IO;

public class RaceController : MonoBehaviour
{
    public GameObject carPrefab;
    
    [HideInInspector]
    public bool motorsEnabled = true;
    private SplineMesh.Spline track;
    private Dictionary<string, CarStatus> cars = new Dictionary<string, CarStatus>();
    private RaceParameters raceParameters;
    private State state;

    private void OnEnable()
    {
        track = FindObjectOfType<SplineMesh.Spline>();
        raceParameters = readRaceParameters();

        if (ModeController.Mode == SimulatorMode.Playback)
        {
            setState(new Playback(this));
        }
        else if (ModeController.Mode == SimulatorMode.Race)
        {
            setState(new RaceLobby(this));
        }
        else
        {
            setState(new FreePractice(this));
        }
    }

    private RaceParameters readRaceParameters()
    {
        try
        {
            var reader = new StreamReader("RaceParameters.json");
            var raceParameters = JsonUtility.FromJson<RaceParameters>(reader.ReadToEnd());
            return raceParameters;
        }
        catch (FileNotFoundException e)
        {
            return new RaceParameters();
        }
    }

    void setState(State state)
    {
        if (this.state != null)
        {
            this.state.OnDisable();
        }
        this.state = state;
        state.OnEnable();
    }

    public void FinishLineTrigger(GameObject car)
    {
        state.CarHitTrigger(car, 0);
    }

    public void TrackSegmentTrigger1(GameObject car)
    {
        state.CarHitTrigger(car, 1);
    }

    public void TrackSegmentTrigger2(GameObject car)
    {
        state.CarHitTrigger(car, 2);
    }


    public class Playback: State
    {
        public Playback(RaceController c) : base(c)
        {

        }
        public override void OnEnable() { }
        public override void CarHitTrigger(GameObject car, int segment) { }
    }

    public class RaceLobby: State
    {
        private Dictionary<string, CarConnected> cars = new Dictionary<string, CarConnected>();

        public RaceLobby(RaceController c): base(c)
        {
            
        }
        
        public override void OnEnable() {
            
            Debug.Log("RaceParameters:" + JsonUtility.ToJson(c.raceParameters));

            EventBus.Publish(new RaceLobbyInit(c.raceParameters));
            Subscribe<CarConnected>(e =>
            {
                cars[e.car.name] = e;
            });
            Subscribe<CarDisconnected>(e =>
            {                
                cars.Remove(e.car.name);
                EventBus.Publish(new CarRemoved(e.car));
            });
            
            Countdown(c.raceParameters.autoStartQualifyingSeconds, () => {
                c.setState(new Qualifying(c, cars.Values.ToArray()));
            });

        }        
    }

    // TODO: penalize crashes, reposition for re-entry after N seconds (remove support for reversing)

    public class Qualifying : Racing
    {
        private CarConnected[] cars;
        public Qualifying(RaceController c, CarConnected[] cars): base(c)
        {
            this.cars = cars;
        }

        public override void OnEnable()
        {
            base.OnEnable();
            foreach (CarConnected car in cars)
            {
                c.addCarOnTrack(car);
            }

            var controllers = FindObjectsOfType<CarController>();
            int i = 0;
            float totalLength = 14;
            float spacing = totalLength / (float)controllers.Count();
            foreach (var car in controllers)
            {
                var curveSample = c.track.GetSampleAtDistance(c.track.Length - (++i * spacing));
                car.transform.position = curveSample.location + 0.1f * Vector3.up;
                car.transform.rotation = curveSample.Rotation;
                car.GetComponent<Rigidbody>().velocity = Vector3.zero;
            }


            EventBus.Publish(new QualifyingStart(cars.Select(c => c.car).ToArray()));

            Countdown(c.raceParameters.qualifyingDurationSeconds, () => {
                Debug.Log("End of qualifying");
                c.motorsEnabled = false;

                var results = c.cars.Values.Select(v => v.LastLap).OrderBy(l => float.IsNaN(l.bestLap) ? float.MaxValue : l.bestLap).ToArray();
                EventBus.Publish(new QualifyingResults(results));
                var startingOrder = results.Select(l => l.car).ToArray();

                c.setState(new StartingGrid(c, startingOrder));                
            });
        }

        public override int compareLaps(LapCompleted a, LapCompleted b)
        {
            return Racing.compareByBestLap(a, b);
        }
    }

    public class StartingGrid : State
    {
        CarInfo[] startingGrid;

        public StartingGrid(RaceController c, CarInfo[] startingGrid): base(c)
        {
            this.startingGrid = startingGrid;
        }

        public override void OnEnable()
        {
            EventBus.Publish(new StartingGridInit(startingGrid));
            
            var cars = FindObjectsOfType<CarController>();
            int i = 0;
            foreach (var car in cars)
            {
                var curveSample = c.track.GetSampleAtDistance(c.track.Length - (++i * 0.4f));
                car.transform.position = curveSample.location + 0.1f * Vector3.up + curveSample.Rotation * Vector3.right * (i % 2 == 0 ? 1 : -1) * 0.1f;
                car.transform.rotation = curveSample.Rotation;
                car.GetComponent<Rigidbody>().velocity = Vector3.zero;
            }

            foreach (var carState in c.cars.Values) {
                carState.ResetLap();
            }

            Countdown(c.raceParameters.autoStartRaceSeconds, () => {
                c.setState(new Race(c));
            });
        }
    }

    public class Race : Racing
    {
        int finishers = 0;

        public Race(RaceController c): base(c)
        {            
            c.motorsEnabled = true;
            qualifying = false;
        }

        public override void OnEnable()
        {
            base.OnEnable();
            EventBus.Publish(new RaceStart());

            // TODO: race timeout
            Subscribe<LapCompleted>(l => {
                if (l.lapCount >= c.raceParameters.lapCount || finishers > 0)
                {
                    EventBus.Publish(new CarFinished(l.car));
                    if (finishers++ == 0)
                    {
                        EventBus.Publish(new RaceWon(l.car));
                    }                    
                    if (finishers == c.cars.Count) {
                        EventBus.Publish(new RaceFinished(CurrentStandings.standings));
                    }
                }
            });                        
        }

        public override int compareLaps(LapCompleted a, LapCompleted b)
        {
            return Racing.compareByCompletedLaps(a, b);
        }
    }

    public class FreePractice: Racing
    {
        public FreePractice(RaceController c): base(c) {
        }

        override public void OnEnable()
        {
            base.OnEnable();
            EventBus.Publish(new FreePracticeStart());
            

            Subscribe<CarConnected>(e =>
            {
                c.addCarOnTrack(e);
            });

            Subscribe<CarDisconnected>(e =>
            {
                EventBus.Publish(new CarRemoved(e.car));
            });
        }

        public override int compareLaps(LapCompleted a, LapCompleted b)
        {
            return Racing.compareByBestLap(a, b);
        }
    }

    public abstract class Racing : State
    {
        private DateTime sessionStartTime;
        public bool qualifying = true;
        public Racing(RaceController c): base(c)
        {
        
        }

        public abstract int compareLaps(LapCompleted a, LapCompleted b);

        override public void OnEnable()
        {            
            sessionStartTime = System.DateTime.Now;
            Subscribe<MotorsToggle>(x => {
                Debug.Log("Motors enabled: " + c.motorsEnabled);
                c.motorsEnabled = !c.motorsEnabled;
            });

            Subscribe<CarRemoved>(e =>
            {
                Debug.Log("Remove Car " + e.car.name);
                c.cars.Remove(e.car.name);
            });

            Subscribe<CarDisconnected>(e =>
            {
                c.cars[e.car.name].Disconnected();
                EventBus.Publish(CurrentStandings);
            });
        }

        override public void CarHitTrigger(GameObject car, int segment)
        {
            var totalTime = GameEvent.TimeDiff(System.DateTime.Now, sessionStartTime);
            bool updateStandings = c.cars[car.name].TrackSegmentStarted(segment, totalTime);
            if (updateStandings)
            {
                EventBus.Publish(CurrentStandings);
            }
        }

        public CurrentStandings CurrentStandings { get {
            var standings = c.cars.Values
                .Select(c => c.lastLap)
                .ToArray();
            Array.Sort(standings, compareLaps);            

            return new CurrentStandings(standings, qualifying);
        }}

        static float getComparableTime(LapCompleted t)
        {
            if (t != null && !float.IsNaN(t.bestLap)) return t.bestLap;
            return float.MaxValue;
        }

        static int getComparableLapCount(LapCompleted t)
        {
            if (t != null) return t.lapCount;
            return 0;
        }

        public static int compareByBestLap(LapCompleted a, LapCompleted b)
        {
            return getComparableTime(a).CompareTo(getComparableTime(b));
        }

        public static int compareByCompletedLaps(LapCompleted a, LapCompleted b)
        {
            int la = getComparableLapCount(a);
            int lb = getComparableLapCount(b);
            if (la == lb && la > 0)
            {
                return a.totalTime.CompareTo(b.totalTime);
            }
            return lb - la;
        }
    }    

    public abstract class State
    {
        private List<IDisposable> disposables = new List<IDisposable>();
        protected RaceController c;

        public State(RaceController c)
        {
            this.c = c;
        }


        public abstract void OnEnable();
        public virtual void CarHitTrigger(GameObject car, int segment) {
            
        }
        public void Subscribe<T>(Action<T> action) where T : class
        {
            Subscribe(EventBus.Receive<T>(), action);
        }
        public void Subscribe<T>(IObservable<T> observable, Action<T> action)
        {
            disposables.Add(observable.Subscribe(action));
        }
        public void Subscribe<T>(IObservable<T> observable, Action action)
        {
            disposables.Add(observable.Subscribe(_ => action()));
        }
        public void OnDisable()
        {
            foreach (var d in disposables)
            {
                d.Dispose();
            }
        }

        public void Countdown(int seconds, Action action) {
            var secondsRemaining = Observables.Countdown(seconds);

            Subscribe(secondsRemaining, EventBus.Publish);

            var timedOut = secondsRemaining.Where(s => s.secondsRemaining == 0).Select(s => new Unit());
            var spaceBar = EventBus.Receive<ProceedToNextPhase>().Select(s => new Unit());

            Subscribe(
                timedOut.Merge(spaceBar),
                action
            );
        }
    }

    void addCarOnTrack(CarConnected e)
    {
        if (cars.ContainsKey(e.car.name))
        {
            throw new Exception("TODO: duplicate car");
        }

        var curveSample = track.GetSampleAtDistance(0.95f * track.Length);
        var car = Instantiate(carPrefab, curveSample.location + 0.1f * Vector3.up, curveSample.Rotation);
        var carController = car.GetComponent<CarController>();
        carController.SetSocket(e.socket);
        carController.raceController = this;
        car.name = e.car.name;
        Debug.Log("Add Car '" + e.car.name + "'");
        cars[e.car.name] = new CarStatus(e.car);
    }

    class CarStatus
    {        
        internal float lastLapRecordedAt = Time.time;
        readonly CarInfo CarInfo;
        internal LapCompleted lastLap;
        private int trackSegment;
        internal bool disconnected = false;
        public LapCompleted LastLap
        {
            get { return lastLap; }
        }

        public CarStatus(CarInfo carInfo)
        {
            this.CarInfo = carInfo;
            ResetLap();
        }

        internal void ResetLap() {
            trackSegment = -1;
            lastLap = new LapCompleted(
                CarInfo,
                -1, // Not even started first lap yet, 0 would be running first lap.
                float.NaN, float.NaN, 0, disconnected);
        }

        internal void Disconnected()
        {
            Debug.Log("Disconnected: " + CarInfo.name);
            this.disconnected = true;
            lastLap = new LapCompleted(CarInfo, lastLap.lapCount, lastLap.lastLap, lastLap.bestLap, lastLap.totalTime, true);
        }

        internal bool TrackSegmentStarted(int segment, float totalTime)
        {
            Debug.Log("segment " + segment + " for " + CarInfo.name + " (currently " + this.trackSegment + ")");
            if (segment == (this.trackSegment + 1) % 3) {
                Debug.Log("Is go!");
                this.trackSegment = segment;
                if (segment == 0)
                {
                    Debug.Log("Lap Time for '" + CarInfo.name + "'");
                    NewLapTime(totalTime);
                    return true;
                }
            }
            return false;
        }

        internal void NewLapTime(float totalTime)
        {
            var now = Time.time;
            var lapCount = lastLap.lapCount + 1;
            if (lapCount == 0)
            {
                // crossed finish line, starting first lap.
                lastLapRecordedAt = now;
                lastLap = new LapCompleted(CarInfo, 0, float.NaN, float.NaN, totalTime, false);
                return;
            }
            var lastTime = now - lastLapRecordedAt;
            lastLapRecordedAt = now;
            var bestTime = (lastTime < lastLap.bestLap || float.IsNaN(lastLap.bestLap)) ? lastTime : lastLap.bestLap;
            
            this.lastLap = new LapCompleted(CarInfo, lapCount, lastTime, bestTime, totalTime, false);
            EventBus.Publish(lastLap);
        }
    }
}

[Serializable]
public class JsonControlCommand
{
    public string action;
    public string move;
    public float value;
}