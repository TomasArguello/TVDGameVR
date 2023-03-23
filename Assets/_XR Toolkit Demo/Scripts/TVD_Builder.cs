using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UniRx;
using UniRx.Triggers;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class TVD_Builder : MonoBehaviour {
    public LayerMask CollidableObjects;
    public MeshRenderer IndicatorPrefab;
    private Dictionary<GameObject, GameObject> _trackedObjs = new Dictionary<GameObject, GameObject>();
    //private Dictionary<GameObject, IDisposable> _trackedSubs = new Dictionary<GameObject, IDisposable>();

    private UnityObjectPool<MeshRenderer> _tapePool;
    private CompositeDisposable _trackingLifetime = new CompositeDisposable();

    [Header("Joint Settings")]
    [SerializeField] private float _breakForce = 10000f;
    [SerializeField] private float _breakTorque = 10000f;

    private int _physicsLayer;
    private int _snappingLayer;
    private int _physicsMask;
    private int _simulationLayer;

    //private const string IS_SIMULATING = "IsSimulating";
    //private const string BUILD_IDS = "BuildIDs";
    //private const string SIM_IDS = "SimIDs";
    private const string STRUCTURAL_TAG = "Structural";

    private bool _isPhysicsOn = false;


    [SerializeField, Space]
    private InputActionProperty _primaryButtonLeft;
    public InputActionProperty PrimaryActionLeft {
        get => _primaryButtonLeft;
        set => SetInputActionProperty(ref _primaryButtonLeft, value);
    }
    //private bool _alreadyPressed = false;

    [SerializeField]
    private InputActionProperty _secondaryButtonRight;
    private bool _alreadyPressedPhysics = false;

    [SerializeField] private InputActionProperty _secondaryLeft;
    [SerializeField] private InputActionProperty _primaryRight;



    // ========================================================================================

    // ========================================================================================
    private void Awake() {
        _physicsLayer = LayerMask.NameToLayer("Interactible");
        _snappingLayer = LayerMask.NameToLayer("Tape");
        _physicsMask = (1 << _physicsLayer) | (1 << _snappingLayer);
        _simulationLayer = LayerMask.NameToLayer("Simulation");

        _tapePool = new UnityObjectPool<MeshRenderer>(IndicatorPrefab,
            (rend) => rend.enabled = true,
            (rend) => { rend.enabled = false; rend.transform.position = Vector3.zero; }
            );
    }

    private void Start() {

    }

    private void Update() {
        //if (_primaryButtonLeft.action?.activeControl?.device is TrackedDevice )
        //if (_primaryButtonLeft.action?.activeControl != null
        //    && _primaryButtonLeft.action.activeControl.IsPressed())
        //{
        //    if (!_alreadyPressed )
        //    {
        //        _alreadyPressed = true;
        //        //this.CreateJoint();
        //    }
        //}
        //else
        //    _alreadyPressed = false;

        if (
            (_secondaryButtonRight.action?.activeControl != null && _secondaryButtonRight.action.activeControl.IsPressed())
            || (_secondaryLeft.action?.activeControl?.IsPressed() ?? false)
            || (_primaryButtonLeft.action?.activeControl?.IsPressed() ?? false)
            || (_primaryRight.action?.activeControl?.IsPressed() ?? false)
            ) {
            if (!_alreadyPressedPhysics) {
                _alreadyPressedPhysics = true;
                this.TogglePhysics();
            }
        } else
            _alreadyPressedPhysics = false;

    }

    public void OnPickup(SelectEnterEventArgs args) {
        if (_isPhysicsOn) return;

        //Debug.Log($"Picked up {args.interactable.name}({args.interactable.GetInstanceID()})");
        //_trackedObjs.Add(args.interactable.gameObject, Transform.Instantiate(IndicatorPrefab).gameObject);
        
        args.interactable.gameObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        if (!args.interactable.gameObject.CompareTag(STRUCTURAL_TAG))
            return;

        //GameObject indicator = _trackedObjs[args.interactable.gameObject];
        args.interactable.gameObject.OnCollisionEnterAsObservable()
            .Where(c => ((1 << c.gameObject.layer) | CollidableObjects) == CollidableObjects && c.gameObject.CompareTag("Structural"))
            .Subscribe(c => {
                //Debug.Log($"{args.interactable.gameObject.name} is colliding with {c.gameObject.name}");
                //indicator.GetComponent<MeshRenderer>().enabled = true;
                //indicator.transform.position = c.GetContact(0).point;

                GameObject tape = _tapePool.Next.gameObject;
                _trackedObjs.Add(c.gameObject, tape);
                tape.transform.position = c.GetContact(0).point;
            })
            .AddTo(_trackingLifetime);
        args.interactable.gameObject.OnCollisionStayAsObservable()
            .Where(c => ((1 << c.gameObject.layer) | CollidableObjects) == CollidableObjects && c.gameObject.CompareTag("Structural"))
            .Subscribe(c => {
                //indicator.transform.position = c.GetContact(0).point;
                if (!_trackedObjs.ContainsKey(c.gameObject)) {
                    _trackedObjs.Add(c.gameObject, _tapePool.Next.gameObject);
                }
                _trackedObjs[c.gameObject].transform.position = c.GetContact(0).point;
            })
            .AddTo(_trackingLifetime);
        args.interactable.gameObject.OnCollisionExitAsObservable()
            .Where(c => ((1 << c.gameObject.layer) | CollidableObjects) == CollidableObjects && c.gameObject.CompareTag("Structural"))
            .Subscribe(c => {
                //indicator.GetComponent<MeshRenderer>().enabled = false;
                if (_trackedObjs.ContainsKey(c.gameObject)) {
                    _tapePool.Clear(_trackedObjs[c.gameObject].GetComponent<MeshRenderer>());
                    _trackedObjs.Remove(c.gameObject);
                }
            })
            .AddTo(_trackingLifetime);

    }
    public void OnDrop(SelectExitEventArgs args) {
        if (_isPhysicsOn) return;

        Rigidbody rb = args.interactable.gameObject.GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeAll;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        //Debug.Log($"Dropped {args.interactable.name}({args.interactable.GetInstanceID()})");
        //if (_trackedObjs.ContainsKey(args.interactable.gameObject))
        //{
        //    GameObject indicator = _trackedObjs[args.interactable.gameObject];
        //    //GameObject.Destroy(indicator);
        //    _trackedObjs.Remove(args.interactable.gameObject);
        //}
        //if (_trackedSubs.ContainsKey(args.interactable.gameObject))
        //{
        //    _trackedSubs[args.interactable.gameObject].Dispose();

        //}

        _trackedObjs.Clear();
        _trackingLifetime.Dispose();
        _trackingLifetime = new CompositeDisposable();

        GameObject[] tapes = _tapePool.Current(_tapePool.ActiveCount).Select(r => r.gameObject).ToArray();
        //for (int i = tapes.Length - 1; i >= 0; --i)
        //{
        //    Collider[] tapedObjects = Physics.OverlapSphere(
        //        tapes[i].transform.position,
        //        tapes[i].transform.lossyScale.x,//tapes[i].GetComponent<PunTapeHandler>().TapeSize,
        //        1 << _physicsLayer | 1 << _snappingLayer);
        //    if (tapedObjects.Count(t => (t.gameObject.layer == _physicsLayer) && t.CompareTag(STRUCTURAL_TAG)) <= 1
        //        || tapedObjects.Any(t => t.gameObject.layer == _snappingLayer))
        //    {
        //        _tapePool.Clear(tapes[i].GetComponent<MeshRenderer>());
        //        Debug.Log($"Cleared tape {tapes[i].NameAndID()}");
        //    }
        //}
        for (int i = 0; i < tapes.Length; i++) {
            Collider[] tapedObjects = Physics.OverlapSphere(
                tapes[i].transform.position,
                tapes[i].transform.lossyScale.x,
                1 << _physicsLayer);

            if (tapedObjects.Length <= 1) {
                _tapePool.Clear(tapes[i].GetComponent<MeshRenderer>());
                continue;
            }


            Collider[] overlappingTape = Physics.OverlapSphere(
                tapes[i].transform.position,
                tapes[i].transform.lossyScale.x,
                1 << _snappingLayer);
            if (overlappingTape.Length > 0) {
                _tapePool.Clear(tapes[i].GetComponent<MeshRenderer>());
                continue;
            }

            GameObject[] allTape = GameObject.FindGameObjectsWithTag("Structural")
                .Where(g => g.layer == _snappingLayer)
                .ToArray();
            for (int j = 0; j < allTape.Length; j++) {
                if (allTape[j] != tapes[i] && (allTape[j].transform.position - tapes[i].transform.position).magnitude < 0.02f) {
                    _tapePool.Clear(tapes[i].GetComponent<MeshRenderer>());
                    break;
                }
            }
        }
    }
    // ========================================================================================

    // ========================================================================================
    public void CreateJoint() {
        //Debug.Log("Jointing!!!");
        if (_trackedObjs.Keys.Count == 0)
            return;

        GameObject heldObj = _trackedObjs.Keys.First();
        if (_trackedObjs[heldObj].GetComponent<MeshRenderer>().enabled) {
            GameObject tape = GameObject.Instantiate(_trackedObjs[heldObj]);

            Collider other = Physics.OverlapSphere(tape.transform.position, tape.transform.lossyScale.x, CollidableObjects)
                .Where(t => t != heldObj.transform)
                .FirstOrDefault();

            if (other == null) {
                Debug.LogWarning("Other not found!");
                GameObject.Destroy(tape);
                return;
            }

            //tape.transform.SetParent(heldObj.transform);
            //FixedJoint j = tape.AddComponent<FixedJoint>();
            //j.breakForce = 10000;
            //j.breakTorque = 10000;
            //j.connectedBody = other.GetComponent<Rigidbody>();
            Debug.Log($"Taped {heldObj.name} to {other.name}");
        }
    }


    void SetInputActionProperty(ref InputActionProperty property, InputActionProperty value) {
        if (Application.isPlaying)
            property.DisableDirectAction();

        property = value;

        if (Application.isPlaying && isActiveAndEnabled)
            property.EnableDirectAction();
    }
    // ========================================================================================

    // ========================================================================================
    #region Physics
    private void Enable(Rigidbody rb, bool enable = true) {
        rb.GetComponent<MeshRenderer>().enabled = enable;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.detectCollisions = enable;
    }

    public void TogglePhysics() {
        if (_isPhysicsOn)
            TurnOffPhysics();
        else
            TurnOnPhysics();

        _isPhysicsOn = !_isPhysicsOn;
    }

    private void TurnOnPhysics() {
        Rigidbody[] rbs = GameObject.FindObjectsOfType<Rigidbody>()
            .Where(rb => ((1 << rb.gameObject.layer) & _physicsMask) > 0)
            .ToArray();

        Dictionary<Rigidbody, Rigidbody> dupMapping = new Dictionary<Rigidbody, Rigidbody>();
        for (int i = 0; i < rbs.Length; i++) {
            Rigidbody dup = GameObject.Instantiate(rbs[i]);
            dup.gameObject.layer = _simulationLayer;
            dup.name = $"sim_{dup.name}";

            //GameObject.Destroy(dup.GetComponent<CostValue>());
            GameObject.Destroy(dup.GetComponent<ObservableDestroyTrigger>());

            dupMapping.Add(rbs[i], dup);

            this.Enable(rbs[i], false);
        }
        Rigidbody[] tapes = dupMapping.Keys
            .Where(rb => rb.gameObject.layer == _snappingLayer)
            .Select(rb => dupMapping[rb])
            .ToArray();
        for (int i = 0; i < tapes.Length; i++) {
            Collider cTape = tapes[i].GetComponent<Collider>();
            Collider[] tapedObjects = Physics.OverlapSphere(
                tapes[i].position,
                tapes[i].transform.lossyScale.x,//tapes[i].GetComponent<PunTapeHandler>().TapeSize,
                1 << _simulationLayer)
                .Where(t => (t.GetComponent<Rigidbody>() == null || !tapes.Contains(t.GetComponent<Rigidbody>()))
                    && t.CompareTag(STRUCTURAL_TAG))
                .ToArray();

            for (int j = 0; j < tapedObjects.Length; j++) {
                Collider c1 = tapedObjects[j];
                Physics.IgnoreCollision(cTape, c1);
                //if (j == 0)
                //{
                //    tapes[i].transform.SetParent(c1.transform);
                //    continue;
                //}

                FixedJoint joint = tapes[i].gameObject.AddComponent<FixedJoint>();
                //ConfigurableJoint joint = tapes[i].gameObject.AddComponent<ConfigurableJoint>();
                joint.connectedBody = c1.GetComponent<Rigidbody>();
                joint.breakForce = _breakForce;
                joint.breakTorque = _breakTorque;
                joint.enableCollision = false;
                joint.anchor = tapes[i].position;
                //joint.axis = 
                //this.InitConJoint(joint);
                joint.OnDestroyAsObservable()
                    .Subscribe(_ => {
                        // BUG: Why does this never get called?
                        Debug.Log("Broken", c1);
                        Physics.IgnoreCollision(cTape, c1, false);
                        for (int k = 0; k < tapedObjects.Length; k++) {
                            Physics.IgnoreCollision(c1, tapedObjects[k], false);
                        }
                    })
                    .AddTo(tapes[i].gameObject);

                for (int k = j + 1; k < tapedObjects.Length; k++) {
                    Collider c2 = tapedObjects[k];
                    Physics.IgnoreCollision(c1, c2);
                    //joint.OnDestroyAsObservable()
                    //    .Subscribe(_ =>
                    //    {
                    //		  // BUG: This seems to be getting called before the joint dies...
                    //        Debug.Log("Brokeded", c1);
                    //        //Physics.IgnoreCollision(c2, c1, false);
                    //    })
                    //    .AddTo(c1);


                    // TODO: What if objects are jointed to multiple tapes?
                    // One joint's failure would cause the objects to collide even though they
                    // are still jointed elsewhere.
                    // I don't think this should be a concern often if at all because our
                    // objects are all lines, but otherwise this could cause problems.
                }
            }
        }
        for (int i = 0; i < dupMapping.Values.Count; i++) {
            Rigidbody dup = dupMapping.Values.ElementAt(i);
            dup.useGravity = true;
            dup.constraints = RigidbodyConstraints.None;
        }

        //if (_feedbackUI != null)
        //    _feedbackUI.StartEvaluation();
    }

    private void TurnOffPhysics() {
        Rigidbody[] allRB = GameObject.FindObjectsOfType<Rigidbody>();

        allRB.Where(rb => ((1 << rb.gameObject.layer) & _physicsMask) > 0)
            .ToList()
            .ForEach(rb => this.Enable(rb));

        allRB.Where(rb => rb.gameObject.layer == _simulationLayer)
            .ToList()
            .ForEach(rb => GameObject.Destroy(rb.gameObject));
    }

    private void InitConJoint(ConfigurableJoint j) {
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
    #endregion Physics
    // ========================================================================================

    // ========================================================================================


}
