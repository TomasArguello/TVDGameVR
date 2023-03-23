using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonStateChanger : MonoBehaviour {
    [SerializeField]
    public int id;

    public int stateVal = 0;

    [SerializeField]
    ButtonManager buttonManager;

    [SerializeField]
    GameObject childContent;

    private void Update() {
        if (stateVal == 1) {
            childContent.SetActive(true);
        } else {
            childContent.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.CompareTag("Player")) {
            Debug.Log("Collision!");
            this.stateVal = 1 - this.stateVal;
            buttonManager.SetButton(id);
        }
    }
}
