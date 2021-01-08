using UnityEngine;
using TMPro;

public class CountdownUI : MonoBehaviour
{
    void Start()
    {
        EventBus.Subscribe<PhaseChange>(this, clearText);
        EventBus.Subscribe<SecondsRemaining>(this, s => {
            var txt = s.secondsRemaining > 0 ? "" + s.secondsRemaining : "";
            gameObject.GetComponent<TextMeshProUGUI>().text = txt;
        });
    }

    void clearText()
    {
        gameObject.GetComponent<TextMeshProUGUI>().text = "";
    }
}
