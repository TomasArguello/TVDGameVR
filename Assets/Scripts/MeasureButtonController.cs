using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeasureButtonController : MonoBehaviour
{
    public GameObject measurePlane;

    [Header("Sprites")]
    public Sprite activatedSprite;
    public Sprite deactivatedSprite;

    private bool toggle;

    private void Awake() {
        measurePlane.SetActive(false);
        TurnOff(this.gameObject);
    }

    void Update() {
        measurePlane.SetActive(toggle);
    }

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.CompareTag("PlayerFinger")) {
            if (!toggle) {
                TurnOn(this.gameObject);
            } else {
                TurnOff(this.gameObject);
            }
        }
    }

    public void TurnOn(GameObject gameObject) {
        gameObject.GetComponent<SpriteRenderer>().sprite = activatedSprite;
        toggle = true;
    }

    public void TurnOff(GameObject gameObject) {
        gameObject.GetComponent<SpriteRenderer>().sprite = deactivatedSprite;
        toggle = false;
    }


}
