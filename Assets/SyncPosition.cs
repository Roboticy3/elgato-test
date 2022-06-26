using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SyncPosition : MonoBehaviourPunCallbacks, IPunObservable
{
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting){
            stream.SendNext(transform.position);
        }
        else {
            transform.position = (Vector3)stream.ReceiveNext();
        }
    }
}
