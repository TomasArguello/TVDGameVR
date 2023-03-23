using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UniRx.Triggers;

namespace Tamu.Tvd.VR
{
    // ================================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ================================================================================================
    /**
     * Fake a physics simulation by duplicating structural elements from the scene into a simulation
     * physics layer and turning on all their physics properties. Simply destroy the duplicate objects
     * and re-enable visibility on the original objects to end the simulation.
     */
    // ================================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ================================================================================================
    public class PhysicsActivator : MonoBehaviour
    {
        // ==================================================================================
        // Fields & Properties
        // ==================================================================================
        //[SerializeField] private 

        public GameObject parentObj;

        private int _physicsLayer;
        private int _snappingLayer;
        private int _physicsMask;
        private int _simulationLayer;

        private BoolReactiveProperty _isActive = new BoolReactiveProperty(false);
        public BoolReactiveProperty IsPhysicsOn => _isActive;
        private bool _wasActive = false;

        private const string IS_SIMULATING = "IsSimulating";
        private const string BUILD_IDS = "BuildIDs";
        private const string SIM_IDS = "SimIDs";
        private const string STRUCTURAL_TAG = "Structural";
        // ==================================================================================
        // MonoBehaviour
        // ==================================================================================
        // ------------------------------------------------------------------------
        void Awake()
        {
            _physicsLayer = LayerMask.NameToLayer("Physics Objects");
            _snappingLayer = LayerMask.NameToLayer("Tape");
            _physicsMask = (1 << _physicsLayer) | (1 << _snappingLayer);
            _simulationLayer = LayerMask.NameToLayer("Simulation Objects");

            _isActive.Subscribe(b =>
            {
                if (b && !_wasActive)
                    this.TurnOnPhysics();
                else if (!b && _wasActive)
                    this.TurnOffPhysics();

                _wasActive = b;
            })
            .AddTo(this);
        }
        // ------------------------------------------------------------------------
        //void Start()
        //{

        //}
        // ------------------------------------------------------------------------
        // ==================================================================================
        // Methods
        // ==================================================================================
        public void TogglePhysics() => _isActive.Value = !_isActive.Value;
        // ==================================================================================
        #region Physics
        // ==================================================================================

        private void Enable(Rigidbody rb, bool enable = true)
        {
            rb.GetComponent<MeshRenderer>().enabled = enable;
            rb.detectCollisions = enable;
        }

        private void TurnOnPhysics()
        {
            //find all rigidbodies in scene (tape and building materials)
            Rigidbody[] rbs = GameObject.FindObjectsOfType<Rigidbody>()
                .Where(rb => ((1 << rb.gameObject.layer) & _physicsMask) > 0)
                .ToArray();

            //duplicate rigidbodies and disable original objects
            Dictionary<Rigidbody, Rigidbody> dupMapping = new Dictionary<Rigidbody, Rigidbody>();
            for (int i = 0; i < rbs.Length; i++)
            {
                Rigidbody dup = GameObject.Instantiate(rbs[i]);
                dup.gameObject.layer = _simulationLayer;
                dup.name = $"sim_{dup.name}";

                GameObject.Destroy(dup.GetComponent<ObservableDestroyTrigger>());

                dupMapping.Add(rbs[i], dup);

                this.Enable(rbs[i], false);
            }

            //find all tapes
            Rigidbody[] tapes = dupMapping.Keys
                .Where(rb => rb.gameObject.layer == _snappingLayer)
                .Select(rb => dupMapping[rb])
                .ToArray();
            for (int i = 0; i < tapes.Length; i++)
            {
                //find all objects colliding with tape[i]
                Collider cTape = tapes[i].GetComponent<Collider>();
                Collider[] tapedObjects = Physics.OverlapSphere(
                    tapes[i].position,
                    tapes[i].transform.lossyScale.x,
                    1 << _simulationLayer)
                    .Where(t => (t.GetComponent<Rigidbody>() == null || !tapes.Contains(t.GetComponent<Rigidbody>()))
                        && t.CompareTag(STRUCTURAL_TAG))
                    .ToArray();

                //find a parent to add collided objects to
                Rigidbody currParent = null;
                foreach (Collider collidedObj in tapedObjects) {
                    if (collidedObj.gameObject.transform.parent) {
                        currParent = collidedObj.gameObject.transform.parent.gameObject.GetComponent<Rigidbody>();
                        break;
                    }
                }

                //create new parent object if none is found
                if (currParent == null) {
                    currParent = GameObject.Instantiate(parentObj.GetComponent<Rigidbody>());
                    currParent.gameObject.layer = _simulationLayer;
                    currParent.name = $"sim_{currParent.name}_group{i}";
                }

                //add collided objects to found parent and combine any child objects from other parents found
                foreach (Collider collidedObj in tapedObjects) {
                    if (collidedObj.gameObject.transform.parent) {
                        Rigidbody oldParent = collidedObj.gameObject.transform.parent.gameObject.GetComponent<Rigidbody>();
                        Component[] oldChildren = oldParent.GetComponentsInChildren(typeof(Rigidbody));
                        foreach (Component child in oldChildren) {
                            child.transform.SetParent(currParent.transform, true);
                        }

                        //GameObject.Destroy(collidedObj.gameObject.transform.parent.gameObject);
                    } else {
                        collidedObj.transform.SetParent(currParent.transform, true);
                    }
                }

                //add tape to parent object
                tapes[i].gameObject.transform.SetParent(currParent.transform, true);
            }

            //check for any objects without a parent and give them one as lone entities and delete rigidbody from them
            //marshmallow has special case and is always treated as a lone object
            foreach (KeyValuePair<Rigidbody,Rigidbody> entry in dupMapping) {
                GameObject currObj = entry.Value.gameObject;
                if (currObj.CompareTag("Marshmallow")) {
                    Rigidbody dupParent = GameObject.Instantiate(parentObj.GetComponent<Rigidbody>());
                    dupParent.gameObject.layer = _simulationLayer;
                    dupParent.name = $"sim_{dupParent.name}_marshmallow";

                    currObj.transform.SetParent(dupParent.transform, true);

                    GameObject.Destroy(currObj.GetComponent<TVDGrabbable>());
                    GameObject.Destroy(currObj.GetComponent<Rigidbody>());
                } else if (currObj.transform.parent == null) {
                    Rigidbody dupParent = GameObject.Instantiate(parentObj.GetComponent<Rigidbody>());
                    dupParent.gameObject.layer = _simulationLayer;
                    dupParent.name = $"sim_{dupParent.name}_single";

                    currObj.transform.SetParent(dupParent.transform, true);

                    GameObject.Destroy(currObj.GetComponent<TVDGrabbable>());
                    GameObject.Destroy(currObj.GetComponent<Rigidbody>());
                } else {
                    GameObject.Destroy(currObj.GetComponent<TVDGrabbable>());
                    GameObject.Destroy(currObj.GetComponent<Rigidbody>());
                }               
            }

            //activate physics of all new parent objects created and delete obselete parents (only tape object)
            GameObject[] parents = GameObject.FindGameObjectsWithTag("Parent");
            foreach (GameObject parent in parents) {
                if(parent.transform.childCount == 1 && parent.gameObject.transform.GetChild(0).position == Vector3.zero) {
                    GameObject.Destroy(parent);
                } else {
                    parent.GetComponent<Rigidbody>().useGravity = true;
                    parent.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
                    parent.GetComponent<Rigidbody>().isKinematic = false;
                }
            }       

            /*
            Tests:
                Test (Expected) -> Actual

                One object (creates single parent) -> works
                Two objects (create two parents) -> works

                Two objects connected (creates single parent) -> all objects disappear minus tape and no parents are creted but extra sim objects are made
                Two different connected objects (creates two parents) ->
                One connected cube (creates single parent that holds entire cube) ->

                Check how tapes are working
                Change marshmallow to not be parented

            Concerns:
                How will parent's origin affect objects' positions
            */    
        }

        private void TurnOffPhysics()
        {
            Rigidbody[] allRB = GameObject.FindObjectsOfType<Rigidbody>();

            allRB.Where(rb => ((1 << rb.gameObject.layer) & _physicsMask) > 0)
                .ToList()
                .ForEach(rb => this.Enable(rb));

            allRB.Where(rb => rb.gameObject.layer == _simulationLayer)
                .ToList()
                .ForEach(rb => GameObject.Destroy(rb.gameObject));
        }

        private void InitConJoint(ConfigurableJoint j)
        {
            j.xMotion = ConfigurableJointMotion.Locked;
            j.yMotion = ConfigurableJointMotion.Locked;
            j.zMotion = ConfigurableJointMotion.Locked;
            j.angularXMotion = ConfigurableJointMotion.Limited;
            j.angularYMotion = ConfigurableJointMotion.Limited;
            j.angularZMotion = ConfigurableJointMotion.Limited;

            j.linearLimitSpring = new SoftJointLimitSpring { spring = 1e6f, damper = 9e4f };

            j.lowAngularXLimit = new SoftJointLimit { limit = 90 };
            j.highAngularXLimit = new SoftJointLimit { limit = 90 };
            j.angularYZLimitSpring = new SoftJointLimitSpring { spring = 1e6f, damper = 9e4f };
            j.angularYLimit = new SoftJointLimit { limit = 5 };
            j.angularZLimit = new SoftJointLimit { limit = 5 };

            j.xDrive = new JointDrive { positionSpring = 1e6f, positionDamper = 9e4f };
            j.yDrive = new JointDrive { positionSpring = 1e6f, positionDamper = 9e4f };
            j.zDrive = new JointDrive { positionSpring = 1e6f, positionDamper = 9e4f };

            j.angularXDrive = new JointDrive { positionSpring = 1e6f, positionDamper = 9e4f };
            j.angularYZDrive = new JointDrive { positionSpring = 1e6f, positionDamper = 9e5f };
            j.slerpDrive = new JointDrive { positionSpring = 1e6f, positionDamper = 9e4f };

            j.breakForce = 1e7f;
            j.breakTorque = 1e7f;
        }
        #endregion
        // ==================================================================================
    }
    // ================================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ================================================================================================ 
}
