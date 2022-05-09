using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using static NetworkManager;

public class meshchange : MonoBehaviourPunCallbacks
{
    [SerializeField]
    public Material cloud_land;
    public Material star_land;
    public Material night_land;
    public Material sea_land;
    private MeshRenderer mesh;

    public void mesh1()
    {
        mesh = GetComponent<MeshRenderer>();
        mesh.material = cloud_land;
    }

    public void mesh2()
    {
        mesh = GetComponent<MeshRenderer>();
        mesh.material = star_land;
    }

    public void mesh3()
    {
        mesh = GetComponent<MeshRenderer>();
        mesh.material = night_land;
    }

    public void mesh4()
    {
        mesh = GetComponent<MeshRenderer>();
        mesh.material = sea_land;
    }
}

