using UnityEngine;
using System.Linq;
using System;
using System.IO;

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
    [Header("EDITOR ONLY")]
    [ContextMenuItem("Save", "SaveTrackJson")]
    [ContextMenuItem("Load", "LoadTrackJson")]
    public string trackFileName = "track.json";

    void SaveTrackJson()
    {
        track = GetComponent<SplineMesh.Spline>();
        if (File.Exists(trackFileName))
        {
            Debug.Log(trackFileName + " already exists.");
            return;
        }
        var sr = File.CreateText(trackFileName);
        sr.WriteLine(ToJson(track.nodes.ToArray(), true));
        sr.Close();
        Debug.Log("Saved track to " + trackFileName);
    }

    void LoadTrackJson() {
        track = GetComponent<SplineMesh.Spline>();
        LoadTrack(trackFileName);
    }
#endif

    void Start()
    {
        track = GetComponent<SplineMesh.Spline>();
        LoadTrack(RaceParameters.readRaceParameters().track);
        UpdateFinishLine();
        UpdateTriggers();
        track.NodeListChanged += NodeListChanged;
    }

    public void LoadTrack(string fileName)
    {
        SplineMesh.SplineNode[] trackNodes = null;
        if (fileName != null) {
            string json = null;
            if (File.Exists(fileName))
            {
                Debug.Log("Loading track from filesystem: " + fileName);
                json = new StreamReader(fileName).ReadToEnd();
            }
            else
            {
                Debug.Log("Loading track as Unity resource: " + fileName);
                json = Resources.Load<TextAsset>("Tracks/" + fileName.Replace(".json", ""))?.text;
            }
            if (json != null)
            {
                trackNodes = FromJson<SplineMesh.SplineNode>(json);
            }
            else
            {
                Debug.Log("Unknown track " + fileName + ": not found as a resource or in the file system");
            }
        }
        if (trackNodes != null && trackNodes.Length > 0)
        {
            track.enabled = false;
            track.IsLoop = false;
            while (track.nodes.Count > trackNodes.Length)
            {
                track.RemoveNode(track.nodes.Last());
            }
            for (int i = 0; i < trackNodes.Length; i++)
            {
                var node = trackNodes[i];
                if (i < track.nodes.Count)
                {
                    track.nodes[i].Position = node.Position;
                    track.nodes[i].Direction = node.Direction;
                    track.nodes[i].Up = node.Up;
                    track.nodes[i].Scale = node.Scale;
                    track.nodes[i].Roll = node.Roll;
                }
                else
                {
                    track.AddNode(node);
                }
            }
            track.IsLoop = true;
            track.enabled = true;
        }
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

    public static T[] FromJson<T>(string json)
    {
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
        return wrapper.trackNodes;
    }

    public static string ToJson<T>(T[] array)
    {
        Wrapper<T> wrapper = new Wrapper<T>();
        wrapper.trackNodes = array;
        return JsonUtility.ToJson(wrapper);
    }

    public static string ToJson<T>(T[] array, bool prettyPrint)
    {
        Wrapper<T> wrapper = new Wrapper<T>();
        wrapper.trackNodes = array;
        return JsonUtility.ToJson(wrapper, prettyPrint);
    }

    [Serializable]
    private class Wrapper<T>
    {
        public T[] trackNodes;
    }
}
