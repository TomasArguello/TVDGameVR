using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using Photon.Pun;

namespace Tamu.Tvd.VR
{
    ///[RequireComponent(typeof(PhysicalButton))]
    public class ResetButton : MonoBehaviour
    {
        private PhysicalButton _button;
        private int _physicsLayer;
        private int _snappingLayer;
        private int _physicsMask;
        private int _simulationLayer;

        public Color StartColor = Color.green;
        public Color StopColor = new Color(1, .75f, 0);

        public Sprite activated;
        public Sprite deactivated;
        public SpriteRenderer buttonSprite;

        public PhotonView PV;

        private void Awake()
        {
            _physicsLayer = LayerMask.NameToLayer("Physics Objects");
            _snappingLayer = LayerMask.NameToLayer("Tape");
            _physicsMask = (1 << _physicsLayer) | (1 << _snappingLayer);
            _simulationLayer = LayerMask.NameToLayer("Simulation Objects");
            PV = GetComponent<PhotonView>();
            //_button = this.GetComponent<PhysicalButton>();
        }

        private void OnTriggerEnter(Collider other)
        {
            /*
            if (other.gameObject.CompareTag("PlayerFinger"))
            {
                Reset();
            }
            */
        }

        public void Reset()
        {
            Rigidbody[] rbs = GameObject.FindObjectsOfType<Rigidbody>()
                .Where(rb => ((1 << rb.gameObject.layer) & _physicsMask) > 0)
                .ToArray();

            for(int i = 0; i < rbs.Length; i++)
            {
                Debug.Log("rbs has " + rbs[i].name);
            }

            for(int i = 0; i < rbs.Length; i++)
            {
                if(rbs[i].gameObject.layer == _snappingLayer)
                {
                    Debug.Log("Encountered a tape, gonna erase that later!");
                    continue;
                }
                
                if (!rbs[i].GetComponent<PhotonView>().IsMine)
                {
                    Debug.Log("Intiated transfer of ownership of " + rbs[i].gameObject.name + " from " + rbs[i].GetComponent<PhotonView>().Owner.NickName + " to " + PhotonNetwork.LocalPlayer.NickName);
                    rbs[i].GetComponent<PhotonView>().TransferOwnership(PhotonNetwork.LocalPlayer.ActorNumber);
                    //PhotonNetwork.Destroy(rbs[i].gameObject);
                }
                else
                {
                    PhotonNetwork.Destroy(rbs[i].gameObject);
                }
            }

            StartCoroutine(DestroyRestOfMats());
        }

        IEnumerator DestroyRestOfMats()
        {

            yield return new WaitForSeconds(.5f);
            
            Rigidbody[] rbs = GameObject.FindObjectsOfType<Rigidbody>()
                .Where(rb => ((1 << rb.gameObject.layer) & _physicsMask) > 0)
                .ToArray();

            for(int i = 0; i < rbs.Length; i++)
            {
                if(rbs[i].gameObject.layer == _snappingLayer)
                {
                    continue;
                }
                else
                {
                    PhotonNetwork.Destroy(rbs[i].gameObject);
                }
            }

            DestroyTape(rbs);
        }

        public void DestroyTape(Rigidbody[] rbs)
        {
            foreach(Rigidbody rb in rbs)
            {
                if(rb.gameObject.layer == _snappingLayer)
                {
                    GameObject.Destroy(rb.gameObject);
                }
                else
                {
                    Debug.Log("Not sure what " + rb.gameObject.name + " is doing here but oh well");
                    continue;
                }
            }
        }

        [PunRPC]
        public void DestroyTapesSync()
        {
            Rigidbody[] rbs = GameObject.FindObjectsOfType<Rigidbody>()
                .Where(rb => rb.gameObject.layer == _snappingLayer)
                .ToArray();
            foreach(Rigidbody rb in rbs)
            {
                GameObject.Destroy(rb.gameObject);
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ResetButton))]
        public class ResetButtonEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();
                ResetButton resetButt = (ResetButton)target;
                if (GUILayout.Button("Reset"))
                {
                    resetButt.Reset();
                }
            }
        }
#endif
    }
}

