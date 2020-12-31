using UnityEngine;
using TMPro;

public class CountdownUI : MonoBehaviour
{
    // Use this for initialization
    void Start()
    {
        EventBus.Subscribe<SecondsRemaining>(this, s => {
            var txt = s.secondsRemaining > 0 ? "" + s.secondsRemaining : "";
            gameObject.GetComponent<TextMeshProUGUI>().text = txt;
        });
    }
}
