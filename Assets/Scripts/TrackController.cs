using UnityEngine;

[RequireComponent(typeof(SplineMesh.Spline))]
public class TrackController : MonoBehaviour
{
    public Transform finishLineDecal;
    public Transform trackTriggerPrefab;

    private SplineMesh.Spline track;

    private Transform finishLine;
    private Transform finishLineTrigger;
    private Transform firstTrigger;
    private Transform secondTrigger;

#if UNITY_EDITOR
    [ContextMenu("TestGenerate")]
    void TestGenerate()
    {
        track = GetComponent<SplineMesh.Spline>();
        UpdateFinishLine();
        UpdateTriggers();
    }
#endif

    void Start()
    {
        track = GetComponent<SplineMesh.Spline>();
        UpdateFinishLine();
        UpdateTriggers();
        track.NodeListChanged += NodeListChanged;
    }

    void NodeListChanged(object sender, SplineMesh.ListChangedEventArgs<SplineMesh.SplineNode> args)
    {
        UpdateFinishLine();
        UpdateTriggers();
    }

    void UpdateFinishLine()
    {
        if (finishLine == null) {
            finishLine = Instantiate(finishLineDecal);
            finishLine.parent = transform;
        }
        var curveSample = track.GetSampleAtDistance(0);
        finishLine.position = curveSample.location;
        finishLine.rotation = curveSample.Rotation;
    }

    void UpdateTriggers()
    {
        var raceController = FindObjectOfType<RaceController>();
        UpdateTrigger(ref finishLineTrigger, 0, raceController.FinishLineTrigger);
        UpdateTrigger(ref firstTrigger, 0.33f, raceController.TrackSegmentTrigger1);
        UpdateTrigger(ref secondTrigger, 0.67f, raceController.TrackSegmentTrigger2);
    }

    private void UpdateTrigger(ref Transform trigger, float distance, TriggerEventSourceEventHandler eventHandler)
    {
        if (trigger == null)
        {
            trigger = Instantiate(trackTriggerPrefab);
            trigger.parent = transform;
            var eventSource = trigger.GetComponent<TriggerEventSource>();
            eventSource.OnTrigger += eventHandler;
        }
        var curveSample = track.GetSampleAtDistance(track.Length * distance);
        trigger.position = curveSample.location;
        trigger.rotation = curveSample.Rotation;
    }
}
