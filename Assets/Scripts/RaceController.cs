using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;

public class RaceController : MonoBehaviour
{
    public GameObject carPrefab;
    
    [HideInInspector]
    public bool motorsEnabled = true;
    public SplineMesh.Spline track;
    private Dictionary<string, CarStatus> cars = new Dictionary<string, CarStatus>();
    private RaceParameters raceParameters;
    private State state;

    public IEnumerable<CarInfo> GetCars() {
        return cars.Values.Select(car => car.CarInfo);
    }

    private void OnEnable()
    {
        track = FindObjectOfType<SplineMesh.Spline>();
    }

    private void Start()
    {
        switch (ModeController.Mode) {
            case SimulatorMode.Development:
            case SimulatorMode.Race:
                QualitySettings.vSyncCount = 0;  // VSync must be disabled
                Application.targetFrameRate = 60;
                Time.captureFramerate = 60;
                Time.fixedDeltaTime = 0.002f;
                break;
            default:
                QualitySettings.vSyncCount = 1;
                Application.targetFrameRate = 60;
                Time.captureFramerate = 0;
                Time.fixedDeltaTime = 0.02f;
                break;
        }
        raceParameters = RaceParameters.readRaceParameters();
        Observables.Delay(TimeSpan.FromMilliseconds(0)).Subscribe(_ => {
            if (ModeController.Mode == SimulatorMode.Playback || ModeController.Mode == SimulatorMode.RemoteControl)
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
            EventBus.Publish(new CameraFollow(null));
        }).AddTo(this);        
    }

    public void StartFreePractice() {
        setState(new FreePractice(this));
    }

    public void CarConnected(CarConnected c)
    {
        this.state.CarConnected(c);
    }

    public Boolean IsSimulation { get {
        return this.state is Simulation;
    } }

    bool isCurrentState(State state) {
        return this.state == state;
    }

    private void Update()
    {
        if (this.state != null) {
            this.state.Update();
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
        public override void OnSessionFinish() {
            // Never gonna stop
        }
        override public CurrentStandings CurrentStandings() {
            throw new NotImplementedException();
        }
    }

    public class RaceLobby: Simulation
    {
        private Dictionary<string, CarConnected> cars = new Dictionary<string, CarConnected>();

        public RaceLobby(RaceController c): base(c)
        {
            
        }
        
        public override void OnEnable() {
            
            Debug.Log("RaceParameters:" + JsonUtility.ToJson(c.raceParameters));

            EventBus.Publish(new RaceLobbyInit(c.raceParameters));            
            Subscribe<CarDisconnected>(e =>
            {                
                cars.Remove(e.car.name);
                EventBus.Publish(new CarRemoved(e.car));
            });
            
            Countdown(c.raceParameters.autoStartQualifyingSeconds, OnSessionFinish);

        }

        public override void CarConnected(CarConnected c) {
            if (cars.ContainsKey(c.car.name)) {
                throw new JoinException("Car " + c.car.name + " already connected");
            }
            cars[c.car.name] = c;
            EventBus.Publish(c);
        }

        override public CurrentStandings CurrentStandings() {
            return new CurrentStandings(cars.Values.Select(c => c.car.ToLap()).ToArray(), false);
        }

        public override void OnSessionFinish() {
            c.setState(new Qualifying(c, cars.Values.ToArray()));
        }
    }


    public abstract class Simulation : State {
        public Simulation(RaceController c): base(c)
        {
            
        }
    }

    public class Qualifying : Racing
    {
        private CarConnected[] cars;
        public Qualifying(RaceController c, CarConnected[] cars): base(c)
        {
            this.cars = cars;
        }

        public override void OnEnable()
        {
            Debug.Log("Qualifying starting");
            base.OnEnable();
            foreach (CarConnected car in cars)
            {
                c.addCarOnTrack(car);
            }

            var controllers = FindObjectsOfType<CarController>();
            int i = 0;
            float totalLength = c.track.Length;
            float spacing = totalLength / (float)controllers.Count();
            foreach (var car in controllers)
            {
                var curveSample = c.track.GetSampleAtDistance(Mathf.Max(0, c.track.Length - (++i * spacing)));
                car.transform.position = curveSample.location + 0.1f * Vector3.up;
                car.transform.rotation = curveSample.Rotation;
                car.GetComponent<Rigidbody>().velocity = Vector3.zero;
            }


            EventBus.Publish(new QualifyingStart(cars.Select(c => c.car).ToArray()));
            EventBus.Publish(CurrentStandings());

            Countdown(c.raceParameters.qualifyingDurationSeconds, OnSessionFinish);            
        }

        public override void OnSessionFinish()
        {

            Debug.Log("End of qualifying");
            c.motorsEnabled = false;

            var results = c.cars.Values.Select(v => v.LastLap).OrderBy(l => l.bestLap == 0 ? float.MaxValue : l.bestLap).ToArray();
            EventBus.Publish(new QualifyingResults(results));
            var startingOrder = results
                .Where(r => !r.dnf)
                .Select(l => l.car).ToArray();

            c.setState(new StartingGrid(c, startingOrder));                
        }

        public override int compareLaps(LapCompleted a, LapCompleted b)
        {
            return Racing.compareByBestLap(a, b);
        }
    }

    public class StartingGrid : Simulation
    {
        CarInfo[] startingGrid;

        public StartingGrid(RaceController c, CarInfo[] startingGrid): base(c)
        {
            this.startingGrid = startingGrid;
        }

        public override void OnEnable()
        {
            Debug.Log("Starting grid for race");
            EventBus.Publish(new StartingGridInit(startingGrid));
            EventBus.Publish(CurrentStandings());
            
            
            int i = 0;
            foreach (var carInfo in startingGrid)
            {
                var car = GameObject.Find(carInfo.name);
                if (car == null) {
                    Debug.Log("Car not found: " + carInfo.name);
                    continue;
                }
                var curveSample = c.track.GetSampleAtDistance(c.track.Length - (++i * 0.4f));
                car.transform.position = curveSample.location + 0.1f * Vector3.up + curveSample.Rotation * Vector3.right * (i % 2 == 0 ? 1 : -1) * 0.1f;
                car.transform.rotation = curveSample.Rotation;
                car.GetComponent<Rigidbody>().velocity = Vector3.zero;
            }

            foreach (var carState in c.cars.Values) {
                carState.ResetLap();
            }

            Countdown(c.raceParameters.autoStartRaceSeconds, OnSessionFinish);
        }

        public override void OnSessionFinish() {
            c.setState(new Race(c));
        }

        override public CurrentStandings CurrentStandings() {
            return new CurrentStandings(startingGrid.Select(c => c.ToLap()).ToArray(), false);
        }
    }

    public class Race : Racing
    {
        int finishers = 0;
        internal float raceStarted = Time.time;
        internal float firstCarFinished = float.MaxValue;
        int secondsRemaining = 0;

        public Race(RaceController c): base(c)
        {            
            c.motorsEnabled = true;
            qualifying = false;
        }

        public override void OnEnable()
        {
            Debug.Log("Race started");
            base.OnEnable();
            EventBus.Publish(new RaceStart());
            EventBus.Publish(CurrentStandings());

            // TODO: race timeout
            Subscribe<LapCompleted>(l => {
                if (l.lapCount >= c.raceParameters.lapCount || finishers > 0)
                {
                    c.cars[l.car.name].Finished();                    
                    if (finishers++ == 0)
                    {
                        firstCarFinished = Time.time;
                        EventBus.Publish(new RaceWon(l.car));
                    }                    
                    checkForFinish();
                }
            });

            checkForFinish();
        }

        public override void Update()
        {
            float raceEndsAt = Math.Min(
                raceStarted + c.raceParameters.raceTimeoutSeconds,
                firstCarFinished + c.raceParameters.raceTimeoutAfterWinnerSeconds);
            int remaining = (int)(raceEndsAt - Time.time);
            
            if (remaining != secondsRemaining) {
                secondsRemaining = remaining;
                EventBus.Publish(new SecondsRemaining(secondsRemaining));
                if (secondsRemaining == 0) {
                    Debug.Log("Race timed out");
                    OnSessionFinish();
                } else {
                    Debug.Log("Race time remaining: " + remaining);
                }
            }
        }

        public override void OnSessionFinish()
        {
            if (c.isCurrentState(this)) {
                Debug.Log("Race finished");
                EventBus.Publish(new RaceFinished(CurrentStandings().standings.Select(s => new CarRaceResult(
                    s.car, s.lapCount, s.bestLap, s.totalTime, s.dnf
                )).ToArray()));
            }
        }

        public override int compareLaps(LapCompleted a, LapCompleted b)
        {
            return Racing.compareByCompletedLaps(a, b);
        }
    }

    public class FreePractice: Racing
    {
        int i = 0;

        public FreePractice(RaceController c): base(c) {
        }

        override public void OnEnable()
        {
            Debug.Log("Starting free practice");
            base.OnEnable();
            EventBus.Publish(new FreePracticeStart());

            Subscribe<CarConnected>(e => {
                try {
                    Debug.Log("Car connected, adding on track");
                    var car = c.addCarOnTrack(e);
                    var controllers = FindObjectsOfType<CarController>();
                    float totalLength = c.track.Length;
                    float spacing = totalLength / (float)10; // always 10 segments                                        
                    var curveSample = c.track.GetSampleAtDistance(c.track.Length - (i * spacing));
                    car.transform.position = curveSample.location + 0.1f * Vector3.up;
                    car.transform.rotation = curveSample.Rotation;
                    car.GetComponent<Rigidbody>().velocity = Vector3.zero;
                    i++;
                
                } catch (JoinException ex) {
                    Debug.Log("Error adding car in RaceController:" + ex.Message);
                    e.socket.Close();
                }     
            });


            Subscribe<CarDisconnected>(e =>
            {
                EventBus.Publish(new CarRemoved(e.car));
            });
        }

        public override void CarConnected(CarConnected e) {
            if (c.cars.ContainsKey(e.car.name))
            {
                throw new JoinException("Duplicate car");
            }
            EventBus.Publish(e);                   
        }


        public override void OnSessionFinish() {
            // Never gonna stop
        }

        public override int compareLaps(LapCompleted a, LapCompleted b)
        {
            return Racing.compareByBestLap(a, b);
        }
    }

    public abstract class Racing : Simulation
    {
        public bool qualifying = true;
        public Racing(RaceController c): base(c)
        {
        
        }

        public abstract int compareLaps(LapCompleted a, LapCompleted b);

        override public void CarHitTrigger(GameObject car, int segment)
        {
            var totalTime = GameEvent.TimeDiff(System.DateTime.Now, sessionStartTime);
            bool updateStandings = c.cars[car.name].TrackSegmentStarted(segment, totalTime);
            if (updateStandings)
            {
                EventBus.Publish(CurrentStandings());
            }
        }

        override public CurrentStandings CurrentStandings() {
            var standings = c.cars.Values
                .Select(c => c.lastLap)
                .ToArray();
            Array.Sort(standings, compareLaps);            

            return new CurrentStandings(standings, qualifying);
        }

        static float getComparableTime(LapCompleted t)
        {
            if (t != null && t.bestLap != 0) return t.bestLap;
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
        internal DateTime sessionStartTime;
        private List<IDisposable> disposables = new List<IDisposable>();
        protected RaceController c;

        public State(RaceController c)
        {
            this.c = c;
        }

        public virtual void CarConnected(CarConnected c)
        {
            throw new JoinException("Car connections not supported in this state");
        }

        public virtual void OnEnable()
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
                EventBus.Publish(CurrentStandings());
                checkForFinish();
            });
            Subscribe<InvalidateLap>(e => {
                c.cars[e.carName].ResetLap();
                EventBus.Publish(CurrentStandings());
            });
        }

        public virtual void Update() { }

        public abstract CurrentStandings CurrentStandings();

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
            Debug.Log("Start countdown, " + seconds + " seconds");
            var secondsRemaining = Observables.Countdown(seconds);

            Subscribe(secondsRemaining, EventBus.Publish);

            var timedOut = secondsRemaining.Where(s => s.secondsRemaining == 0).Select(s => new Unit());
            var spaceBar = EventBus.Receive<ProceedToNextPhase>().Select(s => new Unit());

            Subscribe(
                timedOut.Merge(spaceBar),
                action
            );
        }

        public void checkForFinish()
        {
            foreach (CarStatus car in c.cars.Values) {
                if (!car.disconnected && !car.finished) {
                    return;
                }
            }
            OnSessionFinish();    
        }

        public abstract void OnSessionFinish();
    }

    CarController addCarOnTrack(CarConnected e)
    {
        if (cars.ContainsKey(e.car.name))
        {
            Debug.Log("Error adding car: Duplicate");
            throw new JoinException("Duplicate car");
        }

        var curveSample = track.GetSampleAtDistance(0.95f * track.Length);
        var car = Instantiate(carPrefab, curveSample.location + 0.1f * Vector3.up, curveSample.Rotation);
        var carController = car.GetComponent<CarController>();
        carController.SetSocket(e.socket);
        car.GetComponent<CarAppearanceController>().CarInfo = e.car;
        carController.raceController = this;
        car.name = e.car.name;
        Debug.Log("Add Car '" + e.car.name + "'");
        cars[e.car.name] = new CarStatus(e.car);
        EventBus.Publish(new CarAdded(e.car));
        return carController;
    }

    class CarStatus
    {        
        internal float lastLapRecordedAt = Time.time;
        public readonly CarInfo CarInfo;
        internal LapCompleted lastLap;
        private int trackSegment;
        internal bool disconnected = false;
        internal bool finished = false;

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
                0, 0, 0, disconnected);
        }

        internal void Disconnected()
        {
            if (!finished) {
                Debug.Log("Disconnected: " + CarInfo.name);
                this.disconnected = true;
                lastLap = new LapCompleted(CarInfo, lastLap.lapCount, lastLap.lastLap, lastLap.bestLap, lastLap.totalTime, true);
            }
        }

        internal void Finished() {
            finished = true;
            EventBus.Publish(new CarFinished(CarInfo));
        }

        internal bool TrackSegmentStarted(int segment, float totalTime)
        {
            if (segment == (this.trackSegment + 1) % 3) {
                this.trackSegment = segment;
                if (segment == 0)
                {                    
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
                lastLap = new LapCompleted(CarInfo, 0, 0, 0, totalTime, false);
                return;
            }
            var lastTime = now - lastLapRecordedAt;
            Debug.Log("Lap Time for '" + CarInfo.name + "': " + lastTime);
            lastLapRecordedAt = now;
            var bestTime = (lastTime < lastLap.bestLap || lastLap.bestLap == 0) ? lastTime : lastLap.bestLap;
            
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