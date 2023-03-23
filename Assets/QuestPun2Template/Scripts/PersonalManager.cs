using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using TMPro;
//
//For handling local objects and sending data over the network
//
namespace Networking.Pun2 {
    public class PersonalManager : MonoBehaviourPunCallbacks {
        [SerializeField] GameObject headPrefab;
        [SerializeField] GameObject handRPrefab;
        [SerializeField] GameObject handLPrefab;
        [SerializeField] GameObject ovrCameraRig;
        //[SerializeField] GameObject nameTagPrefab;
        [SerializeField] Transform[] spawnPoints;
        [SerializeField] GameObject pushCube;

        //Tools
        List<GameObject> toolsR;
        List<GameObject> toolsL;
        int currentToolR;
        int currentToolL;

        PhotonView pv;
        //bool rightChecked = false;
        //bool leftChecked = false;

        GameObject leftObj;
        GameObject rightObj;

        private void Awake() {
            /// If the game starts in Room scene, and is not connected, sends the player back to Lobby scene to connect first.
            if (!PhotonNetwork.NetworkingClient.IsConnected) {
                //SceneManager.LoadScene("Photon2Lobby");
                SceneManager.LoadScene("LaunchScene");
                return;
            }
            /////////////////////////////////

            toolsR = new List<GameObject>();
            toolsL = new List<GameObject>();

            Debug.Log("The PhotonNetwork.LocalPlayer.ActorNumber is " + PhotonNetwork.LocalPlayer.ActorNumber);
            if (PhotonNetwork.LocalPlayer.ActorNumber <= spawnPoints.Length) {
                ovrCameraRig.transform.position = spawnPoints[PhotonNetwork.LocalPlayer.ActorNumber - 1].transform.position;
                ovrCameraRig.transform.rotation = spawnPoints[PhotonNetwork.LocalPlayer.ActorNumber - 1].transform.rotation;
            }
        }

        private void Start() {
            //
            //Instantiate Head
            //
            object[] userName = new object[1];
            userName[0] = PhotonNetwork.LocalPlayer.NickName;
            GameObject obj = (PhotonNetwork.Instantiate(headPrefab.name, OculusPlayer.instance.head.transform.position, OculusPlayer.instance.head.transform.rotation, 0, userName));
            obj.GetComponent<SetColor>().SetColorRPC(PhotonNetwork.LocalPlayer.ActorNumber);

            //       
            //Instantiate Nametag
            //
            // Vector3 playerHeadPos = OculusPlayer.instance.head.transform.position;
            // playerHeadPos.y = playerHeadPos.y + 0.12f;
            // Quaternion playerHeadRot = OculusPlayer.instance.head.transform.rotation;
            // playerHeadRot.y = playerHeadRot.y + 180;
            // object[] userName = new object[1];
            // userName[0] = PhotonNetwork.LocalPlayer.NickName;
            // GameObject nametagObj = (PhotonNetwork.Instantiate(nameTagPrefab.name, playerHeadPos, playerHeadRot, 0,userName));

            //
            //Instantiate right hand
            //
            rightObj = (PhotonNetwork.Instantiate(handRPrefab.name, OculusPlayer.instance.rightHand.transform.position, OculusPlayer.instance.rightHand.transform.rotation, 0));
            for (int i = 0; i < rightObj.transform.childCount; i++) {
                toolsR.Add(rightObj.transform.GetChild(i).gameObject);
                rightObj.transform.GetComponentInChildren<SetColor>().SetColorRPC(PhotonNetwork.LocalPlayer.ActorNumber);
                if (i > 0)
                    toolsR[i].transform.parent.GetComponent<PhotonView>().RPC("DisableTool", RpcTarget.AllBuffered, 1);
            }

            //
            //Instantiate left hand
            //
            leftObj = (PhotonNetwork.Instantiate(handLPrefab.name, OculusPlayer.instance.leftHand.transform.position, OculusPlayer.instance.leftHand.transform.rotation, 0));
            for (int i = 0; i < leftObj.transform.childCount; i++) {
                toolsL.Add(leftObj.transform.GetChild(i).gameObject);
                leftObj.transform.GetComponentInChildren<SetColor>().SetColorRPC(PhotonNetwork.LocalPlayer.ActorNumber);
                if (i > 0)
                    toolsL[i].transform.parent.GetComponent<PhotonView>().RPC("DisableTool", RpcTarget.AllBuffered, 1);
            }
        }

        //Detects input from Thumbstick to switch "hand tools"
        private void Update() {
            if (OVRInput.GetUp(OVRInput.Button.PrimaryThumbstick))
                SwitchToolL();

            if (OVRInput.GetUp(OVRInput.Button.SecondaryThumbstick))
                SwitchToolR();

            //if (!rightChecked) {
            //    //Attach finger tip
            //    GameObject pushCubeGoRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //    Transform parentRight = RecursiveFindChild(rightObj.transform, "hands:b_r_index3");
            //    SetPushCube(pushCubeGoRight, parentRight, "right");

            //    //Attach hand collider
            //    GameObject handColliderGoRight = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            //    Transform parentRightHand = RecursiveFindChild(rightObj.transform, "CustomHandRight");
            //    SetCollider(handColliderGoRight, parentRightHand);
            //    rightChecked = true;
            //}

            //if (!leftChecked) {
            //    //Attach finger tip
            //    GameObject pushCubeGoLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //    Transform parentLeft = RecursiveFindChild(leftObj.transform, "hands:b_l_index3");
            //    SetPushCube(pushCubeGoLeft, parentLeft, "left");

            //    //Attach hand collider
            //    GameObject handColliderGoLeft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            //    Transform parentLeftHand = RecursiveFindChild(leftObj.transform, "CustomHandLeft");
            //    SetCollider(handColliderGoLeft, parentLeftHand);
            //    leftChecked = true;
            //}
        }

        //disables current tool and enables next tool
        void SwitchToolR() {
            toolsR[currentToolR].transform.parent.GetComponent<PhotonView>().RPC("DisableTool", RpcTarget.AllBuffered, currentToolR);
            currentToolR++;
            if (currentToolR > toolsR.Count - 1)
                currentToolR = 0;
            toolsR[currentToolR].transform.parent.GetComponent<PhotonView>().RPC("EnableTool", RpcTarget.AllBuffered, currentToolR);
        }

        void SwitchToolL() {
            toolsL[currentToolL].transform.parent.GetComponent<PhotonView>().RPC("DisableTool", RpcTarget.AllBuffered, currentToolL);
            currentToolL++;
            if (currentToolL > toolsL.Count - 1)
                currentToolL = 0;
            toolsL[currentToolL].transform.parent.GetComponent<PhotonView>().RPC("EnableTool", RpcTarget.AllBuffered, currentToolL);
        }


        //If disconnected from server, returns to Lobby to reconnect
        public override void OnDisconnected(DisconnectCause cause) {
            base.OnDisconnected(cause);
            SceneManager.LoadScene(0);
        }

        //So we stop loading scenes if we quit app
        private void OnApplicationQuit() {
            StopAllCoroutines();
        }

        Transform RecursiveFindChild(Transform parent, string childName) {
            foreach (Transform child in parent) {
                if (child.name == childName) {
                    return child;
                } else {
                    Transform found = RecursiveFindChild(child, childName);
                    if (found != null) {
                        return found;
                    }
                }
            }
            return null;
        }



        //void SetPushCube(GameObject fingerTipCube, Transform parentObj, string leftOrRight) {
        //    fingerTipCube.transform.parent = parentObj;
        //    fingerTipCube.transform.localPosition = (leftOrRight == "right") ? new Vector3(0.0154f, -0.0198f, 0.0141f) : new Vector3(-0.0154f, 0.0198f, -0.0141f);
        //    fingerTipCube.transform.localScale = new Vector3(0.04f, 0.04f, 0.04f);
        //    fingerTipCube.GetComponent<MeshRenderer>().enabled = false;
        //    fingerTipCube.AddComponent<Rigidbody>();
        //    fingerTipCube.GetComponent<Rigidbody>().useGravity = false;
        //    fingerTipCube.GetComponent<Rigidbody>().isKinematic = true;
        //    fingerTipCube.tag = "PlayerFinger";
        //}

        //void SetCollider(GameObject collider, Transform parentObj) {
        //    collider.transform.parent = parentObj;
        //    collider.transform.localPosition = new Vector3(0, 0, 0.04f);
        //    collider.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        //    collider.GetComponent<MeshRenderer>().enabled = false;
        //}

        //GameObject[] FindGameObjectsWithName(string name) {
        //    GameObject[] gameObjects = GameObject.FindObjectsOfType<GameObject>();
        //    GameObject[] arr = new GameObject[gameObjects.Length];
        //    int FluentNumber = 0;
        //    for (int i = 0; i < gameObjects.Length; i++) {
        //        if (gameObjects[i].name == name) {
        //            arr[FluentNumber] = gameObjects[i];
        //            FluentNumber++;
        //        }
        //    }
        //    Array.Resize(ref arr, FluentNumber);
        //    return arr;
        //}
    }
}
