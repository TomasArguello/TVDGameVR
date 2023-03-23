using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tamu.Tvd.VR;

public class ObjectDestroyer : MonoBehaviour {
    public GameObject[] spawners;
    public MaterialPanelController showPanel;

    void Start() {
        foreach (GameObject spawner in spawners) {
            if (spawner != null) {
                spawner.GetComponent<TVDGrabbableSpawner>().newestObject.gameObject.GetComponent<MeshRenderer>().enabled = false;
            }
        }
    }

    void Update() {
        
    }

    public void InvisiblizeNewestObjects() {
        if (!showPanel.toggle) {
            foreach (GameObject spawner in spawners) {
                if (spawner != null) {
                    spawner.GetComponent<TVDGrabbableSpawner>().newestObject.gameObject.GetComponent<MeshRenderer>().enabled = false;
                }
            }
        } else {
            foreach (GameObject spawner in spawners) {
                if (spawner != null) {
                    spawner.GetComponent<TVDGrabbableSpawner>().newestObject.gameObject.GetComponent<MeshRenderer>().enabled = true;
                }
            }
        }
    }
}
