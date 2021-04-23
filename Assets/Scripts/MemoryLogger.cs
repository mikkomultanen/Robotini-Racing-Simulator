using UnityEngine;
using System.Collections;
using System.Collections.Concurrent;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using UnityEngine;
using UniRx;
using System.Collections.Generic;
using UnityEngine.Profiling;

public class MemoryLogger : MonoBehaviour
{
    BinaryWriter stream;

    // Use this for initialization
    void Start() {
        if (Application.platform != RuntimePlatform.WebGLPlayer)
        {
            var logFile = RaceParameters.readRaceParameters().memoryLogFile;
            Debug.Log("Starting memory profiler output to " + logFile);
            stream = new BinaryWriter(File.Open(logFile, FileMode.Create));
        }
    }
    
    private string GetProfilerInfo() {
        return
            "GetAllocatedMemoryForGraphicsDriver: " + Profiler.GetAllocatedMemoryForGraphicsDriver() + "\n"
            + "    GetMonoHeapSize: " + Profiler.GetMonoHeapSizeLong() + "\n"
            + "    GetMonoUsedSize: " + Profiler.GetMonoUsedSizeLong() + "\n"
            + "    GetTotalAllocatedMemory: " + Profiler.GetTotalAllocatedMemoryLong() + "\n"
            + "    GetTotalReservedMemory: " + Profiler.GetTotalReservedMemoryLong() + "\n"
            + "    GetTotalUnusedReservedMemory: " + Profiler.GetTotalUnusedReservedMemoryLong() + "\n";
    }
    
    public float valuesInterval = 1;
    public float previousValues;


    private void Update()
    {
        if (stream == null) return;
        var now = Time.realtimeSinceStartup;
        var elapsed = now - previousValues;
        if (elapsed > valuesInterval) {
            
            previousValues = now;

            var row = "" + now + " " + GetProfilerInfo();
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(row + "\n");
            stream.Write(bytes);
            stream.Flush();
        }
    }
}
