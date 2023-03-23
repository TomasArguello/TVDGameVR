using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using Photon.Pun;

namespace Tamu.Tvd.VR {
    // ================================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ================================================================================================
    /**
     *  Automatically place Tape objects at collision points for the grabbed object of a TVDGrabber.
     *  
     *  TODO: Replace all instances of tape Physics or distance checking in this script with Tape class methods to do so
     */
    // ================================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ================================================================================================
    [RequireComponent(typeof(TVDGrabber))]
    public class AutoTaper : MonoBehaviour {
        // ==================================================================================
        // Fields & Properties
        // ==================================================================================
        private TVDGrabber _grabber;

        public LayerMask TapableObjects;
        private const string STRUCTURAL_TAG = "Structural";
        private const string PANEL_TAG = "UIPanel";
        [SerializeField] private MeshRenderer _tapePrefab;
        private UnityObjectPool<MeshRenderer> _tapePool;

        private Dictionary<GameObject, GameObject> _trackedObjs = new Dictionary<GameObject, GameObject>();
        private CompositeDisposable _trackingLifetime = new CompositeDisposable();

        public PhotonView PV;

        // ==================================================================================
        // MonoBehaviour
        // ==================================================================================
        // ------------------------------------------------------------------------
        void Awake() {
            _grabber = this.GetComponent<TVDGrabber>();

            _tapePool = new UnityObjectPool<MeshRenderer>(
                _tapePrefab,
                (rend) => rend.enabled = true,
                (rend) => { rend.enabled = false; rend.transform.position = Vector3.zero; }
                );

            PV = GetComponent<PhotonView>();
        }
        // ------------------------------------------------------------------------
        void Start() {
            // Track collisions of grabbed object for potential tape placement
            _grabber.OnGrab.AddListener(() => {
                _grabber.grabbedObject.OnCollisionEnterAsObservable()
                    .Where(c => ((1 << c.gameObject.layer) | TapableObjects) == TapableObjects && c.gameObject.CompareTag(STRUCTURAL_TAG))
                    .Subscribe(c => {
                        GameObject tape = _tapePool.Next.gameObject;
                        GameObject cObj = c.gameObject;
                        _trackedObjs.Add(cObj, tape);
                        tape.transform.position = c.GetContact(0).point;
                        PV.RPC("DebugLogSync", RpcTarget.AllBuffered, "Something collided and now a tape has spawned!!", PhotonNetwork.LocalPlayer.NickName);
                        //Add the RPC function here
                        PV.RPC("OnColEnterSync", RpcTarget.OthersBuffered, cObj.GetPhotonView().ViewID, tape.transform.position);
                    })
                    .AddTo(_trackingLifetime);

                _grabber.grabbedObject.OnCollisionStayAsObservable()
                    .Where(c => _trackedObjs.ContainsKey(c.gameObject))
                    .Subscribe(c => {
                        GameObject tempC = c.gameObject;
                        Vector3 tempContact = c.GetContact(0).point;
                        _trackedObjs[tempC].transform.position = tempContact;
                        PV.RPC("OnColStaySync", RpcTarget.OthersBuffered, tempC.GetPhotonView().ViewID, tempContact);
                    })
                    .AddTo(_trackingLifetime);

                _grabber.grabbedObject.OnCollisionExitAsObservable()
                    .Where(c => _trackedObjs.ContainsKey(c.gameObject))
                    .Subscribe(c => {
                        GameObject tempC = c.gameObject;
                        _tapePool.Clear(_trackedObjs[tempC].GetComponent<MeshRenderer>());
                        _trackedObjs.Remove(tempC);
                        PV.RPC("OnColExitSync", RpcTarget.OthersBuffered, tempC.GetPhotonView().ViewID);
                    })
                    .AddTo(_trackingLifetime);

                GameObject[] allTape = FindAllTape();
                VerifyTape(allTape, allTape.Select(t => t.transform).ToArray());
                PV.RPC("OnGrabSync",RpcTarget.OthersBuffered);
                // TODO: Find a way to avoid checking all tape every time, e.g. just tape that is in contact with the grabbed object
                // Unfortunately we can't do the following because the grabbed object is already snapped to the hand by the time
                // this event fires
                //VerifyTape(allTape, allTape
                //	.Where(t => Physics.OverlapSphere(t.transform.position, t.transform.lossyScale.x, TapableObjects)
                //		.Select(c => c.gameObject)
                //		.Contains(_grabber.grabbedObject.gameObject)
                //		).Select(g => g.transform).ToArray()
                //	);
            });

            // Stop tracking and remove any excess placed tape
            _grabber.OnRelease.AddListener(() => {
                _trackedObjs.Clear();
                _trackingLifetime.Dispose();
                _trackingLifetime = new CompositeDisposable();

                Transform[] tapes = _tapePool.Current(_tapePool.ActiveCount).Select(r => r.transform).ToArray();
                VerifyTape(FindAllTape(), tapes);
                PV.RPC("OnReleaseSync", RpcTarget.OthersBuffered);
            });
        }
        // ------------------------------------------------------------------------
        // ==================================================================================
        // Methods
        // ==================================================================================
        public GameObject[] FindAllTape() =>
            GameObject.FindGameObjectsWithTag(STRUCTURAL_TAG)
            .Where(g => g.layer == _tapePrefab.gameObject.layer && g.GetComponent<MeshRenderer>().enabled) // TODO: Find a better way of determining cross-pool activity state
            .ToArray();

        // Clear any objects from 'tapes' that are superfluous according to what is already covered by objects in 'compareToTapes'
        private void VerifyTape(GameObject[] compareToTapes = null, params Transform[] tapes) {
            if (compareToTapes == null)
                compareToTapes = FindAllTape();

            for (int i = 0; i < tapes.Length; i++) {
                Collider[] tapedObjects = OverlappingObjects(tapes[i], TapableObjects);

                if (tapedObjects.Length <= 1)   // Not actually connecting multiple objects
                {
                    if (!_tapePool.Clear(tapes[i].GetComponent<MeshRenderer>()))
                        GameObject.Destroy(tapes[i].gameObject);
                    continue;
                }

                for (int j = 0; j < compareToTapes.Length; j++) {
                    if (compareToTapes[j] != tapes[i].gameObject
                        && (compareToTapes[j].transform.position - tapes[i].position).magnitude < tapes[i].transform.lossyScale.x / 2f) {
                        Collider[] theirObjects = OverlappingObjects(compareToTapes[j].transform, TapableObjects);

                        if (tapedObjects.All(o => theirObjects.Contains(o))) // Another tape already covers what this tape would connect
                        {
                            _tapePool.Clear(tapes[i].GetComponent<MeshRenderer>());
                            continue;
                        }
                    }
                }
            }
        }

        // TODO: Replace with Tape class' means of checking overlaps
        private Collider[] OverlappingObjects(Transform tape, LayerMask mask) {
            return Physics.OverlapSphere(tape.position, tape.lossyScale.x * tape.GetComponent<SphereCollider>().radius, mask);
        }
        // ==================================================================================

        [PunRPC]
        void OnColEnterSync(int objectPhotonID, Vector3 contactPoint)
        {
            GameObject tape = _tapePool.Next.gameObject;
            //Debug.Log("The PhotonView is " + PhotonNetwork.GetPhotonView(objectPhotonID).ViewID);
            GameObject cObj = PhotonNetwork.GetPhotonView(objectPhotonID).gameObject;
            _trackedObjs.Add(cObj, tape);
            tape.transform.position = contactPoint;
            Debug.Log("Successfully Entered via RPC!!");
        }

        [PunRPC]
        void OnColStaySync(int objectPhotonID, Vector3 contactPoint)
        {
            _trackedObjs[PhotonNetwork.GetPhotonView(objectPhotonID).gameObject].transform.position = contactPoint;
        }

        [PunRPC]
        void OnColExitSync(int objectPhotonID)
        {
            _tapePool.Clear(_trackedObjs[PhotonNetwork.GetPhotonView(objectPhotonID).gameObject].GetComponent<MeshRenderer>());
            _trackedObjs.Remove(PhotonNetwork.GetPhotonView(objectPhotonID).gameObject);
        }

        [PunRPC]
        void OnGrabSync()
        {
            GameObject[] allTape = FindAllTape();
            VerifyTape(allTape, allTape.Select(t => t.transform).ToArray());
        }

        [PunRPC]
        void OnReleaseSync()
        {
            _trackedObjs.Clear();
            _trackingLifetime.Dispose();
            _trackingLifetime = new CompositeDisposable();

            Transform[] tapes = _tapePool.Current(_tapePool.ActiveCount).Select(r => r.transform).ToArray();
            VerifyTape(FindAllTape(), tapes);
        }

        [PunRPC]
        void DebugLogSync(string message, string user)
        {
            Debug.Log("Received from " + user + ": " + message);
        }

    }
    // ================================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ================================================================================================ 
}
