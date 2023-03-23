using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonManager : MonoBehaviour {
    [SerializeField]
    private GameObject[] buttons;
    public void SetButton(int changedId) {
        foreach (GameObject button in buttons) {
            if (button.GetComponent<ButtonStateChanger>().id != changedId) {
                button.GetComponent<ButtonStateChanger>().stateVal = 0;
            }
        }
    }
}
