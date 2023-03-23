using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class VRUIOperation : MonoBehaviour {
    public UnityEvent OnEnter;
    public UnityEvent OnExit;

    private void Start() {
        
    }

    private void OnTriggerEnter(Collider other) {
        TrigExit.instance.currentCollider = GetComponent<VRUIOperation>();
        OnEnter.Invoke();
    }
}
