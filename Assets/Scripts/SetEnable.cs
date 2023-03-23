using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetEnable : MonoBehaviour {
    [SerializeField]
    public Collider myCollider;

    void Start() {
        
    }

    void Update() {
        if (!myCollider.enabled) {
            myCollider.enabled = true;
        }
    }
}
