using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//
//Sets the color of the first MeshRenderer/SkinnedMeshRenderer found with GetComponentInChildren
//
namespace Networking.Pun2
{
    public class SetColor : MonoBehaviourPun
    {
        Color playerColor;
        public void SetColorRPC(int n)
        {
            GetComponent<PhotonView>().RPC("RPC_SetColor", RpcTarget.AllBuffered, n);
        }

        [PunRPC]
        void RPC_SetColor(int n)
        {
            switch (n)
            {
                case 1:
                    playerColor = Color.red;
                    break;
                case 2:
                    playerColor = Color.cyan;
                    break;
                case 3:
                    playerColor = Color.green;
                    break;
                case 4:
                    playerColor = Color.yellow;
                    break;
                case 5:
                    playerColor = Color.magenta;
                    break;
                default:
                    playerColor = Color.black;
                    break;
            }
            playerColor = Color.Lerp(Color.white, playerColor, 0.5f);
            
            //NOTE: GetComponentInChildren works via Depth First Search, which is why these two commands had to be switched so that the hands would
            //  actually change color and not the random Cube that is in the hands of each player
            if (GetComponentInChildren<SkinnedMeshRenderer>() != null)
            {
                //Debug.Log("The color of the " + gameObject.name + " before being changed is " + GetComponentInChildren<SkinnedMeshRenderer>().material.color);
                GetComponentInChildren<SkinnedMeshRenderer>().material.color = playerColor;
                //Debug.Log("The color of the " + gameObject.name + " is " + GetComponentInChildren<SkinnedMeshRenderer>().material.color);
            }
            
            else if (GetComponentInChildren<MeshRenderer>() != null)
            {
                //Debug.Log("The color of the " + gameObject.name + " before being changed is " + GetComponentInChildren<MeshRenderer>().material.color);
                GetComponentInChildren<MeshRenderer>().material.color = playerColor;
                //Debug.Log("The color of the " + gameObject.name + " is " + GetComponentInChildren<MeshRenderer>().material.color);
            }
        }
    }
}
