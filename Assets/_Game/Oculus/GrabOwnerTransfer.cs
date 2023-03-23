using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Tamu.Tvd.VR;

public class GrabOwnerTransfer : MonoBehaviourPunCallbacks, IPunOwnershipCallbacks
{

    public TVDGrabbable grabbable;

    public void OnOwnershipRequest(PhotonView targetView, Player requestingPlayer)
    {
        
    }

    public void OnOwnershipTransfered(PhotonView targetView, Player previousOwner)
    {
        //photonView.RPC("DebugLogHelp",RpcTarget.Others,"The OnOwnershipTransfered function has been called!!", PhotonNetwork.LocalPlayer.ActorNumber);
        if(targetView != photonView)
        {
            return;
        }
        Debug.Log("The ownership has been transferred!");
        if(targetView != photonView && previousOwner != PhotonNetwork.LocalPlayer)
        {
            
            Debug.Log("It seems that the previous player is not the same as the local player!");
        }
        //grabbable.grabStarted();
    }

    public void OnOwnershipTransferFailed(PhotonView targetView, Player senderOfFailedRequest)
    {
        
    }

    /*
    public void OnOwnerChange(Player newOwner, Player previousOwner)
    {
        //grabbable.debugLogHelpFunc("OnOwnerChange finally fucking worked!!", PhotonNetwork.LocalPlayer.ActorNumber);
        //photonView.RPC("DebugLogHelp", RpcTarget.Others, "OnOwnerChange finally fucking worked!", PhotonNetwork.LocalPlayer.ActorNumber);
        photonView.RPC("DebugLogHelp", RpcTarget.Others, "OnOwnerChange finally fucking worked!!", PhotonNetwork.LocalPlayer.ActorNumber);
        throw new System.NotImplementedException();
    }
    */
    private void Awake()
    {
        PhotonNetwork.AddCallbackTarget(this);
        
        grabbable = gameObject.GetComponent<TVDGrabbable>();
    }

    private void OnDestroy()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    
}
