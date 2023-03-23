using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UniRx;
using Photon.Pun;

namespace Tamu.Tvd.VR {
    // ================================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ================================================================================================
    /**
     * Override the OVRGrabber to expose the choice of grab button to the inspector, as well as make
     * other properties publicly visible.
     */
    // ================================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ================================================================================================
    [RequireComponent(typeof(PhotonView))]
    public class TVDGrabber : OVRGrabber {
        // ==================================================================================
        // Fields & Properties
        // ==================================================================================

        // OVRInput is a Flags enum, so we can specify multiple inputs in one variable instead of a list
        [Space, SerializeField]
        //protected OVRInput.Axis1D _grabInputs =
        //    OVRInput.Axis1D.PrimaryIndexTrigger | OVRInput.Axis1D.PrimaryHandTrigger;
        protected OVRInput.Axis1D _grabInputs =
            OVRInput.Axis1D.PrimaryIndexTrigger;

        [field: SerializeField] public UnityEvent OnGrab { get; protected set; }
        [field: SerializeField] public UnityEvent OnRelease { get; protected set; }

        public Collider[] GrabVolumes => m_grabVolumes.ToArray();
        public OVRInput.Axis1D GrabInputs => _grabInputs;
        public OVRInput.Controller Controller => m_controller;

        PhotonView pv;

        // ==================================================================================
        // MonoBehaviour
        // ==================================================================================
        protected override void Awake() {
            pv = GetComponent<PhotonView>();

            // Base.Awake() -------------------------
            m_anchorOffsetPosition = transform.localPosition;
            m_anchorOffsetRotation = transform.localRotation;
            //m_anchorOffsetRotation = Quaternion.identity;

            if (!m_moveHandPosition) {
                OVRCameraRig rig = transform.GetComponentInParent<OVRCameraRig>();
                if (rig != null) {
                    rig.UpdatedAnchors += (r) => { TVDUpdatedAnchors(); }; // but call our version of UpdatedAnchors
                    m_operatingWithoutOVRCameraRig = false;
                }
            }
            // --------------------------------------

            // It would be nicer to override the GrabBegin and GrabbableRelease methods
            // and call our events there, but the latter isn't virtual -_-
            this.ObserveEveryValueChanged(g => g.m_grabbedObj)
                .Subscribe(obj => { if (obj == null) OnRelease.Invoke(); else OnGrab.Invoke(); })
                .AddTo(this);
        }
        // ==================================================================================
        // Methods
        // ==================================================================================
        protected virtual void TVDUpdatedAnchors() {
            Vector3 destPos = m_parentTransform.TransformPoint(m_anchorOffsetPosition);
            Quaternion destRot = m_parentTransform.rotation * m_anchorOffsetRotation;
            //Quaternion destRot = m_parentTransform.rotation;
            //Quaternion destRot = m_anchorOffsetRotation;

            if (m_moveHandPosition) {
                GetComponent<Rigidbody>().MovePosition(destPos);
                //GetComponent<Rigidbody>().MoveRotation(destRot);
            }

            if (!m_parentHeldObject) {
                MoveGrabbedObject(destPos, destRot);
            }

            m_lastPos = transform.position;
            m_lastRot = transform.rotation;

            float prevFlex = m_prevFlex;
            // This is the part we are replacing ------------------------
            // Instead of tying the grab to one axis, allow us to specify
            // in the inspector which input axes trigger a grab event
            m_prevFlex = OVRInput.Get(_grabInputs, m_controller);

            //Debug.Log($"Flex: {m_prevFlex:0.000}");
            // ----------------------------------------------------------

            CheckForGrabOrRelease(prevFlex);
        }

        //Basically, if photonview is mine, update anchors from Grabber
        public override void Update() {
            if (pv.IsMine) {
                base.Update();
            }
        }
        // ==================================================================================
    }
    // ================================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ================================================================================================ 
}