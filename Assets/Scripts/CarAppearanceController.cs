using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(Rigidbody))]
public class CarAppearanceController : MonoBehaviour
{
    public MeshRenderer bodyRenderer;
    public MeshRenderer[] tireRenderers;
    [NonSerialized]
    public CarInfo CarInfo;

    private void Start()
    {
        var carInfo = CarInfo;
        if (carInfo != null)
        {
            bodyRenderer.material.color = carInfo.GetColor();
            if (carInfo.texture != null && carInfo.texture.Length > 0) {
                StartCoroutine(GetTexture(carInfo.texture));
            }
        }
    }

    IEnumerator GetTexture(string url)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();

        Texture texture = DownloadHandlerTexture.GetContent(www);
        if (texture != null)
        {
            Debug.Log("Loaded car texture for " + CarInfo.teamId);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            bodyRenderer.material.color = Color.white;
            bodyRenderer.material.mainTexture = texture;
            foreach (var tireRenderer in tireRenderers) {
                tireRenderer.material.color = Color.white;
                tireRenderer.material.mainTexture = texture;
            }
        }
        else {
            Debug.Log("Failed to load car texture for " + CarInfo.teamId + ": " + www.error);
        }
    }
}