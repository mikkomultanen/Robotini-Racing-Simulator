using UnityEngine;
using UniRx;
using System;

public class AudioController : MonoBehaviour
{
    public AudioClip hornClip1;
    public AudioClip hornClip2;
    public AudioClip fixClip1;
    public AudioClip fixClip2;
    public AudioClip bumpClip;
    public AudioClip screamClip;
    public AudioClip crashClip1;
    public AudioClip crashClip2;
    public AudioClip turboClip;
    public AudioClip raceAmbientClip;    
    public AudioClip engineClip1;
    public AudioClip engineClip2;
    public AudioClip engineClip3;
    public AudioClip engineClip4;
   
    private int counter = 0;

    private AudioSource ambientSource;
    private AudioSource effectSource;

    // Start is called before the first frame update
    void Start()
    {
        AudioClip[] engines = { engineClip1, engineClip2, engineClip3, engineClip4 };
        AudioClip[] bumps = { bumpClip, bumpClip, bumpClip, bumpClip, bumpClip, bumpClip, bumpClip, hornClip1, hornClip2 };
        AudioClip[] laps = { turboClip };
        AudioClip[] crashes = { screamClip, crashClip1, crashClip2 };
        AudioClip[] fixes = { fixClip1, fixClip2 };

        ambientSource = gameObject.AddComponent<AudioSource>();
        effectSource = gameObject.AddComponent<AudioSource>();
        ambientSource.loop = true;
        EventBus.Subscribe<RaceLobbyInit>(this, play(ambientSource, raceAmbientClip));
        EventBus.Subscribe<StartingGridInit>(this, play(ambientSource, raceAmbientClip));
        EventBus.Subscribe<QualifyingResults>(this, silence);
        EventBus.Subscribe<RaceFinished>(this, silence);
        EventBus.Receive<CarCrashed>().ThrottleFirst(TimeSpan.FromSeconds(0.1)).Subscribe(carPlay<CarCrashed>(crashes)).AddTo(this);
        EventBus.Subscribe<LapCompleted>(this, carPlay<LapCompleted>(laps));
        EventBus.Receive<CarBumped>().ThrottleFirst(TimeSpan.FromSeconds(0.1)).Subscribe(carPlay<CarBumped>(bumps)).AddTo(this);
        EventBus.Subscribe<CarReturnedToTrack>(this, carPlay<CarReturnedToTrack>(fixes));
        EventBus.Subscribe<GameStatus>(this, s => {
            foreach (var car in s.cars)
            {
                var src = getCarAudioSource(car.name);
                if (src != null) {
                    if (src.clip == null)
                    {
                        src.clip = randomClip(engines);
                        src.loop = true;
                        src.Play();
                    }
                    src.pitch = 0.5f + (float)Math.Pow(car.velocity.magnitude, 0.7f);
                    src.volume = 0.2f + (car.velocity.magnitude - 2) / 3;
                }
            }            
        });
    }

    private Action<E> carPlay<E>(AudioClip[] clips) where E : CarEvent {
        return e => {
            var src = effectSource;
            play(src, randomClip(clips))();
        };
    }

    private AudioClip randomClip(AudioClip[] clips) {
        return clips[counter++ % clips.Length];
    }

    private AudioSource getCarAudioSource(string carName) {
        return GameObject.Find(carName).GetComponent<AudioSource>();
    }

    private Action play(AudioSource src, AudioClip clip) {
        return () => {
            src.clip = clip;
            src.Play();
        };
    }

    private void silence() {
        ambientSource.Stop();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
