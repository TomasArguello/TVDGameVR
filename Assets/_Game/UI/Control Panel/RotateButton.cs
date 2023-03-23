using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Tamu.Tvd.VR {
    // ================================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ================================================================================================
    /**
     * This class does things...
     */
    // ================================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ================================================================================================
    public class RotateButton : PhysicalButton {
        // ==================================================================================
        // Fields & Properties
        // ==================================================================================
        public Transform RotationCenterPoint;
        public Transform[] RotationObjects = new Transform[0];
        public Vector3 RotationAxis = Vector3.up;
        bool RotateClockwise;
        public float RevolutionsPerTick = 0.1f;
        private int _dir = 1;

        public bool RotateSkybox = false;
        private Material _skybox;
        // ==================================================================================
        // MonoBehaviour
        // ==================================================================================
        // ------------------------------------------------------------------------
        // ------------------------------------------------------------------------
        protected override void Start() {
            base.Start();

            if (this.gameObject.name.EndsWith("Right")) {
                RotateClockwise = true;
            } else if (this.gameObject.name.EndsWith("Left")) {
                RotateClockwise = false;
            }
            _dir = RotateClockwise ? 1 : -1;

            this.OnHold.AddListener(this.Rotate);

            if (RotateSkybox) {
                _skybox = Instantiate(RenderSettings.skybox);
                RenderSettings.skybox = _skybox;
            }
        }
        // ------------------------------------------------------------------------
        // ==================================================================================
        // Methods
        // ==================================================================================
        public void Rotate() {
            if (RotationObjects[RotationObjects.Length - 1] == null) {
                RotationObjects[RotationObjects.Length - 1] = GameObject.FindObjectOfType<OVRManager>().transform;
            }

            for (int i = 0; i < RotationObjects.Length; i++) {
                RotationObjects[i].RotateAround(RotationCenterPoint.position, RotationAxis, RevolutionsPerTick * _dir);
            }
            if (RotateSkybox) {
                _skybox.SetFloat("_Rotation", _skybox.GetFloat("_Rotation") - (RevolutionsPerTick * _dir));
                Debug.Log($"{_skybox.GetFloat("_Rotation") - (RevolutionsPerTick * _dir):000.000} = {_skybox.GetFloat("_Rotation"):000.000} - {RevolutionsPerTick * _dir:000.000}", this);
            }
        }
        // ==================================================================================
    }
    // ================================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ================================================================================================ 
}
