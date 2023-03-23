using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tamu.Tvd.VR;
using Photon.Pun;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MaterialPanelController : MonoBehaviour {

    public GameObject materialPanel;

    [HideInInspector]
    public bool toggle;

    public GameObject[] spawners;

    public TVDGrabbableSpawner[] tVDGrabbableSpawners;

    public GameObject costPanelController;

    [Header("Sprites")]
    public Sprite activatedSprite;
    public Sprite deactivatedSprite;

    private void Awake() {
        materialPanel.SetActive(false);
        TurnOff(this.gameObject);
    }

    void Update() {
        materialPanel.SetActive(toggle);
    }

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.CompareTag("PlayerFinger")) {
            if (!toggle) {
                TurnOn(this.gameObject);
                costPanelController.GetComponent<CostPanelController>().TurnOff(costPanelController);
            } else {
                TurnOff(this.gameObject);
            }
        }
    }

    public void TurnOn(GameObject gameObject) {
        gameObject.GetComponent<SpriteRenderer>().sprite = activatedSprite;
        toggle = true;
        foreach (GameObject spawner in spawners) {
            spawner.GetComponent<TVDGrabbableSpawner>().Spawn();
        }
    }

    public void TurnOff(GameObject gameObject) {
        gameObject.GetComponent<SpriteRenderer>().sprite = deactivatedSprite;
        toggle = false;
        foreach (GameObject spawner in spawners) {
            if (spawner.GetComponent<TVDGrabbableSpawner>().newestObject == null) {
                //do nothing
            } else {
                PhotonNetwork.Destroy(spawner.GetComponent<TVDGrabbableSpawner>().newestObject);
                spawner.GetComponent<TVDGrabbableSpawner>().newestObject = null;
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(MaterialPanelController))]
    public class MaterialPanelControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            MaterialPanelController matPanCon = (MaterialPanelController)target;
            if (GUILayout.Button("TurnOn"))
            {
                matPanCon.TurnOn(matPanCon.gameObject);
            }
        }
    }
#endif
}
