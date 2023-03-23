using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;

public class NameTagCreation : MonoBehaviour, IPunInstantiateMagicCallback
{
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        info.photonView.GetComponentInChildren<TMP_Text>().text = info.photonView.InstantiationData[0].ToString();
        Debug.Log("OnPhotonInstantiate was called. The new name is " + info.photonView.GetComponentInChildren<TMP_Text>().text);
    }
}
