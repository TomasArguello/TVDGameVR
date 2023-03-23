using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using Tamu.Tvd.VR;
using Photon.Pun;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CostPanelController : MonoBehaviour {

    public GameObject costPanel;

    [HideInInspector]
    public bool toggle;

    public GameObject materialPanelController;

    const int drinkingStrawPrice = 2;
    const int bambooSkewerPrice = 3;
    const int spaghettiStrawPrice = 1;
    const int coffeeStirrerPrice = 5;

    [Header("Sprites")]
    public Sprite activatedSprite;
    public Sprite deactivatedSprite;

    [Header("Texts")]
    [SerializeField] TMP_Text drinkingStrawNumberTxt;
    [SerializeField] TMP_Text drinkingStrawTotalTxt;
    [SerializeField] TMP_Text bambooSkewerNumberTxt;
    [SerializeField] TMP_Text bambooSkewerTotalTxt;
    [SerializeField] TMP_Text spaghettiStrawNumberTxt;
    [SerializeField] TMP_Text spaghettiStrawTotalTxt;
    [SerializeField] TMP_Text coffeeStirrerNumberTxt;
    [SerializeField] TMP_Text coffeeStirrerTotalTxt;
    [SerializeField] TMP_Text totalPriceTxt;

    // Start is called before the first frame update
    void Awake() {
        TurnOff(this.gameObject);
    }

    // Update is called once per frame
    void Update() {
        costPanel.SetActive(toggle);
    }

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.CompareTag("PlayerFinger")) {
            if (!toggle) {
                materialPanelController.GetComponent<MaterialPanelController>().TurnOff(materialPanelController);
                TurnOn(this.gameObject);
            } else {
                TurnOff(this.gameObject);
            }
        }
    }

    public void TurnOn(GameObject gameObject) {
        gameObject.GetComponent<SpriteRenderer>().sprite = activatedSprite;
        toggle = true;
        CalculateCost();
    }

    public void TurnOff(GameObject gameObject) {
        gameObject.GetComponent<SpriteRenderer>().sprite = deactivatedSprite;
        toggle = false;
    }

    public void CalculateCost() {
        GameObject[] gameObjects = GameObject.FindGameObjectsWithTag("Structural");
        int drinkingStrawNumber = 0;
        int drinkingStrawTotal = 0;
        int bambooSkewerNumber = 0;
        int bambooSkewerTotal = 0;
        int spaghettiStrawNumber = 0;
        int spaghettiStrawTotal = 0;
        int coffeeStirrerNumber = 0;
        int coffeeStirrerTotal = 0;
        int totalPrice = 0;
        foreach (GameObject gameObject in gameObjects) {
            //Debug.Log("The current gameObject in gameObjects is " + gameObject.name);
            //Debug.Log("Does " + gameObject.name + " have a TVDGrabbale? And has it ever been grabbed?" + gameObject.GetComponent<TVDGrabbable>().hasEverBeenGrabbed);
            //Debug.Log("The PV ID is : " + gameObject.GetComponent<PhotonView>().ViewID);
            if (gameObject.GetComponent<TVDGrabbable>() == null) {
                continue;
            }

            if (!gameObject.GetComponent<TVDGrabbable>().hasEverBeenGrabbed) {
                continue;
            }

            switch (gameObject.name) {
                case "drinkingStraw(Clone)":
                    drinkingStrawNumber++;
                    break;
                case "bambooSkewer(Clone)":
                    bambooSkewerNumber++;
                    break;
                case "spaghettistraw(Clone)":
                    spaghettiStrawNumber++;
                    break;
                case "coffeeStirrer(Clone)":
                    coffeeStirrerNumber++;
                    break;
                case "Tape(Clone)":
                    break;
                default:
                    Debug.LogWarning("Found the object with the erroneous name!");
                    break;
            }
        }
        drinkingStrawTotal = drinkingStrawNumber * drinkingStrawPrice;
        bambooSkewerTotal = bambooSkewerNumber * bambooSkewerPrice;
        spaghettiStrawTotal = spaghettiStrawNumber * spaghettiStrawPrice;
        coffeeStirrerTotal = coffeeStirrerNumber * coffeeStirrerPrice;
        totalPrice = drinkingStrawTotal + bambooSkewerTotal + spaghettiStrawTotal + coffeeStirrerTotal;

        drinkingStrawNumberTxt.text = drinkingStrawNumber.ToString();
        drinkingStrawTotalTxt.text = drinkingStrawTotal.ToString("F2");
        bambooSkewerNumberTxt.text = bambooSkewerNumber.ToString();
        bambooSkewerTotalTxt.text = bambooSkewerTotal.ToString("F2");
        spaghettiStrawNumberTxt.text = spaghettiStrawNumber.ToString();
        spaghettiStrawTotalTxt.text = spaghettiStrawTotal.ToString("F2");
        coffeeStirrerNumberTxt.text = coffeeStirrerNumber.ToString();
        coffeeStirrerTotalTxt.text = coffeeStirrerTotal.ToString("F2");
        totalPriceTxt.text = totalPrice.ToString("F2");
        Debug.Log("The total price of stuff is " + totalPriceTxt.text);
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(CostPanelController))]
    public class CostPanelControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            CostPanelController cosPanCon = (CostPanelController)target;
            if(GUILayout.Button("Turn On"))
            {
                cosPanCon.TurnOn(cosPanCon.gameObject);
            }
        }
    }
#endif
}
