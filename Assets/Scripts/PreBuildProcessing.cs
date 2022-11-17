#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class PreBuildProcessing : IPreprocessBuildWithReport
{
    public int callbackOrder => 1;
    public void OnPreprocessBuild(BuildReport report)
    {
        Debug.Log("Setting Python path...");
        System.Environment.SetEnvironmentVariable("EMSDK_PYTHON", "/usr/local/bin/python");
    }
}
#endif