//place this script in the Editor folder within Assets.
using UnityEditor;
using UnityEngine;

//to be used on the command line:
//$ Unity -quit -batchmode -executeMethod WebGLBuilder.build

class WebGLBuilder {
    static void build() {
        Debug.Log("Starting WebGL build");
        string[] scenes = {"Assets/Track.unity"};
        BuildPipeline.BuildPlayer(scenes, "robotini-web-player", BuildTarget.WebGL, BuildOptions.None);
    }
}