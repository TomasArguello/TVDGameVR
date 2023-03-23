using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Photon.Pun;
using Photon.Realtime;

namespace Tamu.Tvd.VR {
    // ================================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ================================================================================================
    /**
     * An infinite source of a particular TVDGrabbable object; when a user grabs the available object,
     * a new one is instantiated to replace it.
     * 
     * NOTE: The choice was made to optionally control the spawned object's position/rotation instead
     * of parenting it to this object to avoid issues with inheriting scale.
     */
    // ================================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ================================================================================================
    public class TVDGrabbableSpawner : MonoBehaviour {
        // ==================================================================================
        // Fields & Properties
        // ==================================================================================
        [SerializeField] private TVDGrabbable _prefab;
        private TVDGrabbable _spawnedObject;
        
        public bool SnapObjectToThis = true;

        [Tooltip("The layer on which to temporarily keep the spawned object before it gets picked up.")]
        public int DisplayLayer = 5;    // UI layer

        [Tooltip("The layer on which the other player's spawned objects reside.")]
        public int OtherDisplayLayer = 13; // UI Layer for others

        [Tooltip("What constraints to apply to the rigidbody of the spawned object when it is grabbed.")]
        public RigidbodyConstraints ConstraintsOnGrab = RigidbodyConstraints.FreezeAll;

        public BoolReactiveProperty Interactible { get; private set; } = new BoolReactiveProperty(true);

        // TODO: Hook this up some other way, this class doesn't need to inherently depend on PhysicsActivator
        private PhysicsActivator _physics;

        /// <summary>
        /// newestObject indicates the most newly created object - which are on the material panel
        /// </summary>
        [HideInInspector]
        public GameObject newestObject = null;

        public bool spawnOnCallFlg;

        // ==================================================================================
        // MonoBehaviour
        // ==================================================================================
        // ------------------------------------------------------------------------
        //private void OnValidate()
        //{
        // TODO: Verify DisplayLayer layer number exists
        //}
        // ------------------------------------------------------------------------
        void Awake() {
            //Spawn();
            Interactible.Subscribe(b => {
                if (_spawnedObject != null && _spawnedObject.TryGetComponent(out MeshRenderer rend))
                    rend.enabled = b;
            })
            .AddTo(this);
        }
        private void Start() {
            _physics = GameObject.FindObjectOfType<PhysicsActivator>();
            _physics.IsPhysicsOn.Subscribe(b => Interactible.Value = !b).AddTo(this);
        }
        // ------------------------------------------------------------------------
        void Update() {
            //Debug.Log(PhotonNetwork.NetworkClientState);
            if (SnapObjectToThis) {
                _spawnedObject.transform.position = this.transform.position;
                _spawnedObject.transform.rotation = this.transform.rotation;
            }
        }
        // ------------------------------------------------------------------------
        // ==================================================================================
        // Methods
        // ==================================================================================
        public void Spawn() {
            Debug.LogWarning("Spawn called on " + this.gameObject.name);
            GameObject[] mallows = GameObject.FindGameObjectsWithTag("Marshmallow");
            if (mallows.Length > 1 && this.gameObject.name == "Spawner Marshmallow") {
                GameObject.Destroy(mallows[0]);
            }

            //Debug.LogWarning("Is _spawnedObject null?? " + _spawnedObject);
            if (_spawnedObject != null) {
                _spawnedObject.gameObject.layer = _prefab.gameObject.layer;

                Rigidbody r = _spawnedObject.GetComponent<Rigidbody>();
                //r.useGravity = true;
                r.constraints = ConstraintsOnGrab;

                _spawnedObject.OnGrab.RemoveListener(this.Spawn);
                //This turns the renderer back on for the object that is getting pulled from the 
                //  Material panel.
                _spawnedObject.TurnOnRend();
            }

            _spawnedObject = PhotonNetwork.Instantiate(_prefab.name, new Vector3(0, 0, 0), Quaternion.identity).GetComponent<TVDGrabbable>();
            newestObject = _spawnedObject.gameObject;

            _spawnedObject.transform.position = this.transform.position;
            _spawnedObject.transform.rotation = this.transform.rotation;
            _spawnedObject.gameObject.layer = DisplayLayer;
            _spawnedObject.OnGrab.AddListener(this.Spawn);

            Rigidbody rb = _spawnedObject.GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;

            Debug.Log("At spawn, " + _spawnedObject.name + " has the constraints of " + rb.constraints + " and shouldn't be using gravity, right? " + rb.useGravity);

            //Debug.Log($"Spawned {_spawnedObject.transform.name}", _spawnedObject);
        }

        // ==================================================================================
    }
    // ================================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ================================================================================================ 
}
