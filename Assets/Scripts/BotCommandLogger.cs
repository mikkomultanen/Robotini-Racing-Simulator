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

public class BotCommandLogger : MonoBehaviour
{
    BinaryWriter stream;

    // Use this for initialization
    void Start() {
        if (Application.platform != RuntimePlatform.WebGLPlayer)
        {
            var logFile = RaceParameters.readRaceParameters().botCommandLogFile;
            if (logFile != "") {
                Debug.Log("Logging bot commands to " + logFile);
                stream = new BinaryWriter(File.Open(logFile, FileMode.Create));
            }
        }
    }        

    private void Update()
    {
        
    }

    public void LogCommand(CarInfo carInfo, JsonControlCommand command) {
        var nameWidth = 15;
        if (stream != null) {
            var line = "" + Time.time +
                " " + carInfo.name.PadRight(nameWidth).Substring(0, nameWidth) +
                " " + JsonUtility.ToJson(command);
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(line + "\n");
            stream.Write(bytes);
            stream.Flush();            
        }
    }
}
