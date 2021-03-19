using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarAppearanceController : MonoBehaviour
{
    public MeshRenderer bodyRenderer;   
    public CarInfo CarInfo;

    private void Start()
    {
        var carInfo = CarInfo;
        if (carInfo != null)
        {
            // This is duplicated in RemoteEventPlayer
            bodyRenderer.material.color = carInfo.GetColor();
        }
    }
}