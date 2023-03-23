using Photon.Pun;
using UnityEngine;
//
//For managing different tools over the network
//
namespace Networking.Pun2
{
    public class ToolManager : MonoBehaviour
    {
        public GameObject cubeTrigger;
        
        [PunRPC]
        public void DisableTool(int n)
        {
            transform.GetChild(n).gameObject.SetActive(false);
        }

        [PunRPC]
        public void EnableTool(int n)
        {
            transform.GetChild(n).gameObject.SetActive(true);
        }
        
        //Added by Tomas, 6/10/2022
        private void Start()
        {
            //Check and see if the child PunHandR belongs to the local client
            if (!transform.GetChild(0).GetComponent<PhotonView>().IsMine)
            {
                cubeTrigger.SetActive(false);
            }
        }
    }
}
