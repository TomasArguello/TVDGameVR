using UnityEngine;
using UnityEngine.Events;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;

namespace Tamu.Tvd.VR {
    // ================================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ================================================================================================
    /**
     *  Override the OVRGrabbable to signal events when the object is grabbed or released.
     *  Also manage control over Rigidbody constraints, unfreezing to allow for grabbing, and restoring
     *  prior contstraints when released.
     */
    // ================================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ================================================================================================
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PhotonView))]
    public class TVDGrabbable : OVRGrabbable {
        protected Rigidbody rb;
        protected RigidbodyConstraints _constraintsBeforeGrab;
        [SerializeField] bool hideHandOnGrab;
        public PhotonView pv;
        public CostPanelController costPanel;

        [HideInInspector] public bool hasEverBeenGrabbed;

        protected virtual void Awake() {
            pv = GetComponent<PhotonView>();

            // Base.Awake() -------------------------
            if (m_grabPoints.Length == 0) {
                // Get the collider from the grabbable
                Collider collider = this.GetComponent<Collider>();
                if (collider == null) {
                    throw new System.ArgumentException("Grabbables cannot have zero grab points and no collider -- please add a grab point or collider.");
                }

                // Create a default grab point
                m_grabPoints = new Collider[1] { collider };
            }
            // --------------------------------------

            rb = this.GetComponent<Rigidbody>();

            PhotonNetwork.AddCallbackTarget(this);

        }

        private void OnDestroy()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
        }



        protected override void Start()
        {
            base.Start();
            costPanel = GameObject.Find("Cost_Button").GetComponent<CostPanelController>();
            //Basically if a user turns on their Material panel, each material gets spawned via
            //  PhotonNetwork.Instantiate(), but the problem is that the materials in that panel are
            //  visible to everyone else. So this turns the mesh renderer off once it spawns. The 
            //  renderer should be turned back on once the user pulls it off from the Material panel.
            //  This is done in the TVDGrabbableSpawner script, under the Spawn() function.
            //  Also, the constraints that make the materials float in the air are assigned in the Spawn()
            //  function located in TVDGrabbableSpawner, but it never gets called on other clients. As 
            //  such, the constraints must be assigned at Start() in this script instead. Otherwise,
            //  the materials will not behave correctly.
            
            if (!pv.IsMine)
            {
                gameObject.GetComponent<Renderer>().enabled = false;
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezeAll;
            }

            //Debug.Log("The constraints of " + name +" at Start are " + rb.constraints);
            //Debug.Log("The constraints of beforeGrab at Start are " + _constraintsBeforeGrab);
        }

        //This is the function that turns on the renderer of the object, its called in the Spawn function
        //  of TVDGrabbableSpawner. It also changes the bool hasEverBeenGrabbed to true so that the
        //  total cost can be updated and synced properly.
        public void TurnOnRend()
        {
            pv.RPC("TurnOnRendRPC", RpcTarget.All);
            pv.RPC("CalcCost", RpcTarget.All);
        }



        [field: SerializeField] public UnityEvent OnGrab { get; private set; } = new UnityEvent();
        public override void GrabBegin(OVRGrabber hand, Collider grabPoint) {
            //base.GrabBegin(hand, grabPoint);
            //OnGrab.Invoke();
            int localActorNum = PhotonNetwork.LocalPlayer.ActorNumber;
            hasEverBeenGrabbed = true;

            pv.RPC("ForceRelease", RpcTarget.Others);
            m_grabbedBy = hand;
            m_grabbedCollider = grabPoint;

            pv.RPC("DebugLogHelp", RpcTarget.Others, "This " + name + " is owned by " + pv.Owner.ActorNumber + " and its constraints are " + rb.constraints + " while the constraintsBeforeGrab are " + _constraintsBeforeGrab, localActorNum);

            pv.RPC("StartNetworkedGrabbing", RpcTarget.AllBuffered);
            if (pv.Owner == PhotonNetwork.LocalPlayer)
            {
                Debug.Log("No need to transfer ownership!");
            }
            else
            {
                pv.TransferOwnership(PhotonNetwork.LocalPlayer);
            }



            /*
            // OLD CODE
            //pv.TransferOwnership(PhotonNetwork.LocalPlayer);

            if (pv.IsMine) {
                pv.RPC("SetKinematicTrue", RpcTarget.All); //changes the kinematic state of the object to all players when its grabbed
                if (OnGrab != null) {
                    OnGrab.Invoke();
                    _constraintsBeforeGrab = rb.constraints;   // Set after event to allow events to change constraints
                    rb.constraints = RigidbodyConstraints.None;
                }
                if (hideHandOnGrab)
                    m_grabbedBy.GetComponentInChildren<SkinnedMeshRenderer>().enabled = false;
            }
            else
            {
                pv.RPC("DebugLogHelp", RpcTarget.Others, "This " + name + " is not owned by  the local client", localActorNum);
                pv.RPC("DebugLogHelp", RpcTarget.Others, "This " + name + " is owned by " + pv.Owner.ActorNumber, localActorNum);
            }
            */

        }

        [field: SerializeField] public UnityEvent OnRelease { get; private set; } = new UnityEvent();
        public override void GrabEnd(Vector3 linearVelocity, Vector3 angularVelocity) {
            //base.GrabEnd(linearVelocity, angularVelocity);

            //onRelease.Invoke();

            pv.RPC("StopNetworkedGrabbing", RpcTarget.AllBuffered, linearVelocity, angularVelocity);

            /*
            if (pv.IsMine) {
                rb.isKinematic = m_grabbedKinematic;
                pv.RPC("SetKinematicFalse", RpcTarget.All);
                rb.velocity = linearVelocity;
                rb.angularVelocity = angularVelocity;
                if (hideHandOnGrab)
                    m_grabbedBy.GetComponentInChildren<SkinnedMeshRenderer>().enabled = true;
                m_grabbedBy = null;
                m_grabbedCollider = null;
                if (OnRelease != null) {
                    rb.constraints = _constraintsBeforeGrab;   // Set before event to allow events to change constraints
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    OnRelease.Invoke();
                }
            }
            */
        }

        public Collider[] grabPoints {
            get { return m_grabPoints; }
            set { grabPoints = value; }
        }

        virtual public void CustomGrabCollider(Collider[] collider) {
            m_grabPoints = collider;
        }

        [Photon.Pun.PunRPC]
        public void SetKinematicTrue() {
            gameObject.GetComponent<Rigidbody>().isKinematic = true;
            Debug.Log("The object is now kinematic!");
        }

        [PunRPC]
        public void SetKinematicFalse() {
            rb.isKinematic = m_grabbedKinematic;
            Debug.Log("The kinematic state of the " + name + " is " + rb.isKinematic);
            Debug.Log("The object is no longer kinematic!");
        }

        [PunRPC]
        public void ForceRelease() {

            if (m_grabbedBy != null) {
                //Debug.Log("The object " + m_grabbedBy.name + " is about to be Force Released!");
                m_grabbedBy.ForceRelease(this);
                //Debug.Log("The object " + m_grabbedBy.name + " has been forcibly released!");
            }
            else
            {
                Debug.Log("m_grabbed seems to be null");
                //Debug.Log("m_grabbed = " + m_grabbedBy.name);
            }
        }

        [PunRPC]
        public void TurnOnRendRPC()
        {
            gameObject.GetComponent<Renderer>().enabled = true;
            hasEverBeenGrabbed = true;
            Debug.Log("The renderer of " + gameObject.name + " has been turned on. Or it should have...");
        }

        [PunRPC]
        public void CalcCost()
        {
            if (costPanel.toggle)
            {
                costPanel.CalculateCost();
            }
        }

        [PunRPC]
        public void DebugLogHelp(string helpMess, int actorNum)
        {
            Debug.Log("Received message from actor " + actorNum + ": " + helpMess);
        }

        [PunRPC]
        public void StartNetworkedGrabbing()
        {
            Debug.Log("The constraints of beforeGrab at the very beginning of StartNetworkedGrabbing are " + _constraintsBeforeGrab);
            gameObject.GetComponent<Rigidbody>().isKinematic = true;
            Debug.Log("The object is now kinematic!");
            if (OnGrab != null)
            {
                OnGrab.Invoke();
                _constraintsBeforeGrab = rb.constraints;   // Set after event to allow events to change constraints
                Debug.Log("The constraints beforeGrab right after getting them from rb are " + _constraintsBeforeGrab);
                rb.constraints = RigidbodyConstraints.None;
                Debug.Log("The constraints of rb are now " + rb.constraints);
            }
            if (hideHandOnGrab)
                m_grabbedBy.GetComponentInChildren<SkinnedMeshRenderer>().enabled = false;
        }

        [PunRPC]
        public void StopNetworkedGrabbing(Vector3 linearVelocity, Vector3 angularVelocity)
        {
            rb.isKinematic = m_grabbedKinematic;
            Debug.Log("The kinematic state of the " + name + " is " + rb.isKinematic);
            Debug.Log("The object is no longer kinematic!");
            Debug.Log("Gotta check, is gravity on?? useGravity: " + rb.useGravity);
            rb.velocity = linearVelocity;
            rb.angularVelocity = angularVelocity;
            if (hideHandOnGrab)
                m_grabbedBy.GetComponentInChildren<SkinnedMeshRenderer>().enabled = true;
            m_grabbedBy = null;
            m_grabbedCollider = null;
            if (OnRelease != null)
            {
                rb.constraints = _constraintsBeforeGrab;   // Set before event to allow events to change constraints
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                Debug.Log("OnRelease is not null!");
                OnRelease.Invoke();
            }
            else
            {
                Debug.Log("OnRelease was null!");
            }
        }
    }
    // ================================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ================================================================================================ 
}
