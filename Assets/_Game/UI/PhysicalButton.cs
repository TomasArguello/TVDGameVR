using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UniRx;

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
    [RequireComponent(typeof(Collider))]
    public class PhysicalButton : MonoBehaviour {
        // ==================================================================================
        // Fields & Properties
        // ==================================================================================
        protected Collider _volume;

        protected Dictionary<Collider, OVRInput.Controller> _handColliders = new Dictionary<Collider, OVRInput.Controller>();

        [SerializeField] protected MeshRenderer _renderer;
        public Color BaseColor = Color.white;
        public Color UninteractibleColor = Color.gray;
        private MaterialPropertyBlock _block;

        public bool DiscretePress = true;
        public OVRInput.Button PressButtons =
            OVRInput.Button.PrimaryHandTrigger | OVRInput.Button.PrimaryIndexTrigger;

        [field: SerializeField] public UnityEvent OnPress { get; private set; } = new UnityEvent();
        [field: SerializeField] public UnityEvent OnHold { get; private set; } = new UnityEvent();

        private BoolReactiveProperty _isInteractible = new BoolReactiveProperty(true);
        public BoolReactiveProperty Interactible => _isInteractible;
        // ==================================================================================
        // MonoBehaviour
        // ==================================================================================
        // ------------------------------------------------------------------------
        protected virtual void Awake() {
            _block = new MaterialPropertyBlock();

            _volume = this.GetComponent<Collider>();
            _volume.isTrigger = true;
        }
        // ------------------------------------------------------------------------
        protected virtual void Start() {
            Interactible.Subscribe(b => UpdateColor()).AddTo(this);

            TVDGrabber[] hands = GameObject.FindObjectsOfType<TVDGrabber>();
            for (int i = 0; i < hands.Length; i++)
                RegisterColliders(hands[i].Controller, hands[i].GrabVolumes.FirstOrDefault());
        }
        // ------------------------------------------------------------------------
        //private void OnTriggerEnter(Collider other)
        //{
        //    if (_handColliders.Contains(other))
        //    {

        //        Debug.Log($"Colliding with {other.transform.name}", other);
        //    }
        //}
        //private void OnTriggerExit(Collider other)
        //{
        //	if (_handColliders.Contains(other))
        //	{
        //		_collisionCount--;
        //		//Debug.Log($"Stopped colliding with {other.transform.name}", other);
        //	}
        //}

        protected virtual void OnTriggerStay(Collider other) {
            TVDGrabber[] hands = GameObject.FindObjectsOfType<TVDGrabber>();
            for (int i = 0; i < hands.Length; i++)
                RegisterColliders(hands[i].Controller, hands[i].GrabVolumes.FirstOrDefault());

            if (Interactible.Value && _handColliders.TryGetValue(other, out OVRInput.Controller controller)) {
                if (!DiscretePress) {
                    OnHold.Invoke();
                    return;
                }

                if (OVRInput.GetDown(PressButtons, controller)) {
                    Debug.Log($"{this.gameObject.name} Pressed", this);
                    OnPress.Invoke();
                }

                //if (OVRInput.Get(PressButtons, OVRInput.Controller.LTouch) || OVRInput.Get(PressButtons, OVRInput.Controller.RTouch))
                //{
                //    _down = true;
                //}
                //if (OVRInput.GetUp(PressButtons, OVRInput.Controller.LTouch) || OVRInput.GetUp(PressButtons, OVRInput.Controller.RTouch))
                //{
                //    _down = false;
                //    Debug.Log("Release");
                //}
                //if (_down)
                //    OnHold.Invoke();
            }
        }

        //protected virtual void OnTriggerEnter(Collider other) {
        //    if (other.gameObject.CompareTag("PlayerFinger")) {
        //        OnPress.Invoke();
        //    }
        //}
        // ------------------------------------------------------------------------
        // ==================================================================================
        // Methods
        // ==================================================================================
        public void RegisterColliders(OVRInput.Controller controller, params Collider[] colliders) {
            for (int i = 0; i < colliders.Length; i++) {
                if (colliders[i] == null)
                    continue;

                if (_handColliders.ContainsKey(colliders[i]))
                    _handColliders[colliders[i]] = controller;
                else
                    _handColliders.Add(colliders[i], controller);
            }
        }
        public void UnregisterColliders(params Collider[] colliders) {
            for (int i = 0; i < colliders.Length; i++) {
                if (_handColliders.ContainsKey(colliders[i]))
                    _handColliders.Remove(colliders[i]);
            }
        }

        public async Cysharp.Threading.Tasks.UniTask DisableFor(int milliseconds) {
            milliseconds = Mathf.Max(0, milliseconds);

            Interactible.Value = false;
            await Cysharp.Threading.Tasks.UniTask.Delay(milliseconds);
            Interactible.Value = true;
        }

        public void UpdateColor() => this.SetColor(Interactible.Value ? BaseColor : UninteractibleColor);
        protected virtual void SetColor(Color color) {
            _block.SetColor("_Color", color);
            if (_renderer != null)
                _renderer.SetPropertyBlock(_block);
        }
        // ==================================================================================
    }
    // ================================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ================================================================================================ 
}
