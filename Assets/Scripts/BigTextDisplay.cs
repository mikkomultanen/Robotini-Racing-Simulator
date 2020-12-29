using UnityEngine;
using TMPro;

public class BigTextDisplay : MonoBehaviour
{
    void Update()
    {
        EventBus.Subscribe<System.Object>(this, e => {
            if (e is RaceLobbyInit) {
                showBigText("Race lobby, waiting for players...");
            } else if (e is CarConnected) {
                showBigText("Connected: " + (e as CarConnected).car.name);
            } else if (e is CarDisconnected) {
                showBigText("Disconnected: " + (e as CarDisconnected).car.name);
            } else if (e is QualifyingStart) {            
                showBigText("Qualifying");
            } else if (e is StartingGridInit) {
                showBigText("Ready to race");
            } else if (e is RaceStart) {
                showBigText("GO!");
            } else if (e is FreePracticeStart) {
                showBigText("Free practice");
            }
        });
    }

    void showBigText(string text)
    {
        gameObject.GetComponent<TextMeshProUGUI>().text = text;
    }
}
