using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tamu.Tvd.VR;
using Photon.Pun;

public class ObjectDeleter : MonoBehaviour {
    public ParticleSystem smoke;

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.CompareTag("Structural")) {
            if (other.gameObject.GetComponent<TVDGrabbable>().isGrabbed) {
                PhotonNetwork.Destroy(other.gameObject);
                smoke.Play();
            }
        }
    }
}
