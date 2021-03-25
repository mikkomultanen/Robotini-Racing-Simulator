using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

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
            bodyRenderer.material.color = carInfo.GetColor();
            if (carInfo.texture != null) {
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
            bodyRenderer.material.color = Color.white;
            bodyRenderer.material.mainTexture = texture;
        }
        else {
            Debug.Log("Failed to load car texture for " + CarInfo.teamId + ": " + www.error);
        }
    }
}