using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;

public class nicknameinput : MonoBehaviourPunCallbacks
{
    public PhotonView PV;
    public Text Username;
    
    void Start()
    {
        Username.text = PV.Owner.NickName;
    }
}
