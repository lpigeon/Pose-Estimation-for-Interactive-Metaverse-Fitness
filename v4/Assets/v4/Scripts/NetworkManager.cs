using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;


public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("DisconnectPanel")]
    public GameObject DisconnectPanel;
    public InputField NickNameInput;

    [Header("LobbyPanel")]
    public GameObject LobbyPanel;
    public InputField RoomInput;
    public Text WelcomeText;
    public Text LobbyInfoText;
    public Button[] CellBtn;
    public Button PreviousBtn;
    public Button NextBtn;
    public GameObject ManPhotonObject;
    public GameObject WomanPhotonObject;
    public Button man;
    public Button woman;
    public Button reset;

    [Header("RoomPanel")]
    public GameObject RoomPanel;
    public Text ListText;
    public Text RoomInfoText;
    public Text[] ChatText;
    public InputField ChatInput;
    public Button StartGameBtn;
    

    [Header("ETCPanel")]
    public GameObject GamePanel;
    public GameObject ExitPanel;
    public GameObject SkyBoxPanel;
    public GameObject ManPanel;
    public GameObject WomanPanel;

    [Header("GamePanel")]
    public Material cloud;
    public Material star;
    public Material night;
    public Material sea;
    public Button Ok;
    public GameObject obj;

    [Header("ETC")]
    public Text StatusText;
    public PhotonView PV;
    public static string nickname;
    public int Manbuttoncheck = 0;
    public int Womanbuttoncheck = 0;
    public Slider loading;
    public Animator slider;
    public Image Title;

    List<RoomInfo> myList = new List<RoomInfo>();
    int currentPage = 1, maxPage, multiple;


    #region 방리스트 갱신
    // ◀버튼 -2 , ▶버튼 -1 , 셀 숫자
    public void MyListClick(int num)
    {
        if (num == -2) --currentPage;
        else if (num == -1) ++currentPage;
        else PhotonNetwork.JoinRoom(myList[multiple + num].Name);
        MyListRenewal();
    }

    void MyListRenewal()
    {
        // 최대페이지
        maxPage = (myList.Count % CellBtn.Length == 0) ? myList.Count / CellBtn.Length : myList.Count / CellBtn.Length + 1;

        // 이전, 다음버튼
        PreviousBtn.interactable = (currentPage <= 1) ? false : true;
        NextBtn.interactable = (currentPage >= maxPage) ? false : true;

        // 페이지에 맞는 리스트 대입
        multiple = (currentPage - 1) * CellBtn.Length;
        for (int i = 0; i < CellBtn.Length; i++)
        {
            CellBtn[i].interactable = (multiple + i < myList.Count) ? true : false;
            CellBtn[i].transform.GetChild(0).GetComponent<Text>().text = (multiple + i < myList.Count) ? myList[multiple + i].Name : "";
            CellBtn[i].transform.GetChild(1).GetComponent<Text>().text = (multiple + i < myList.Count) ? myList[multiple + i].PlayerCount + "/" + myList[multiple + i].MaxPlayers : "";
        }
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        int roomCount = roomList.Count;
        for (int i = 0; i < roomCount; i++)
        {
            if (!roomList[i].RemovedFromList)
            {
                if (!myList.Contains(roomList[i])) myList.Add(roomList[i]);
                else myList[myList.IndexOf(roomList[i])] = roomList[i];
            }
            else if (myList.IndexOf(roomList[i]) != -1) myList.RemoveAt(myList.IndexOf(roomList[i]));
        }
        MyListRenewal();
    }
    #endregion

    #region 서버연결
    void Awake()
    {
        ExitPanel.SetActive(false);
        Screen.SetResolution(960, 540, false);
        Manbuttoncheck = 0;
        Womanbuttoncheck = 0;
    }
    void Update()
    {
        StatusText.text = PhotonNetwork.NetworkClientState.ToString();
        LobbyInfoText.text = (PhotonNetwork.CountOfPlayers - PhotonNetwork.CountOfPlayersInRooms) + "로비 / " + PhotonNetwork.CountOfPlayers + "접속";
    }

    public void Connect()
    {
        Title.fillAmount = 0;
        loading.gameObject.SetActive(true);
        loading.GetComponent<Animator>().SetTrigger("on");
        PhotonNetwork.ConnectUsingSettings();//connect 함수를 호출하면 서버에 연결을 한다.
    }

    public override void OnConnectedToMaster() => PhotonNetwork.JoinLobby(); //callback으로 onconnectedtomaster가 되면 로비에join한다.

    public override void OnJoinedLobby()
    {
        slider.Rebind();
        LobbyPanel.SetActive(true);
        RoomPanel.SetActive(false);
        DisconnectPanel.SetActive(false);
        ExitPanel.SetActive(false);
        StatusText.enabled = false;
        loading.gameObject.SetActive(false);
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.LocalPlayer.NickName = NickNameInput.text;
        nickname = PhotonNetwork.LocalPlayer.NickName;
        WelcomeText.text = PhotonNetwork.LocalPlayer.NickName + "님 환영합니다";
        myList.Clear();
    }

    public void Disconnect() => PhotonNetwork.Disconnect();

    public override void OnDisconnected(DisconnectCause cause)
    {
        loading.gameObject.SetActive(false);
        DisconnectPanel.SetActive(true);
        LobbyPanel.SetActive(false);
        RoomPanel.SetActive(false);
        ExitPanel.SetActive(false);
        Manbuttoncheck = 0;
        Womanbuttoncheck = 0;
    }
    #endregion

    #region 게임
    public void CreateRoom() => PhotonNetwork.CreateRoom(RoomInput.text == "" ? "Room" + Random.Range(0, 100) : RoomInput.text, new RoomOptions { MaxPlayers = 4 });

    public void JoinRandomRoom() => PhotonNetwork.JoinRandomRoom();

    public void LeaveRoom() => PhotonNetwork.LeaveRoom();

    public override void OnJoinedRoom()
    {
        RoomPanel.SetActive(false);
        LobbyPanel.SetActive(false);
        ManPanel.SetActive(false);
        WomanPanel.SetActive(false);
        ExitPanel.SetActive(true);
        GamePanel.SetActive(true);
        if(Manbuttoncheck >=1 || Womanbuttoncheck >=1)
        {
            if(PhotonNetwork.IsMasterClient)
            {
                SkyBoxPanel.SetActive(true);
            }
            if(Manbuttoncheck >= 1)
            {
                PhotonNetwork.Instantiate(
                    ManPhotonObject.name,
                    new Vector3(0f, 0f, 0f),
                    Quaternion.identity,
                    0
                );

                GameObject mainCamera = GameObject.FindWithTag("MainCamera");
                mainCamera.GetComponent<UnityChan.ThirdPersonCamera>().enabled = true;
            }

            if(Womanbuttoncheck >= 1)
            {
                PhotonNetwork.Instantiate(
                    WomanPhotonObject.name,
                    new Vector3(0f, 0f, 0f),
                    Quaternion.identity,
                    0
                );

                GameObject mainCamera = GameObject.FindWithTag("MainCamera");
                mainCamera.GetComponent<UnityChan.ThirdPersonCamera>().enabled = true;
            }
        }
        else
        {
            int randomnum = Random.Range(0,100);
            Debug.Log(randomnum);
            if(randomnum%2 == 1)
            {
                PhotonNetwork.Instantiate(
                    ManPhotonObject.name,
                    new Vector3(0f, 0f, 0f),
                    Quaternion.identity,
                    0
                );

                GameObject mainCamera = GameObject.FindWithTag("MainCamera");
                mainCamera.GetComponent<UnityChan.ThirdPersonCamera>().enabled = true;
            }
            else
            {
                PhotonNetwork.Instantiate(
                    WomanPhotonObject.name,
                    new Vector3(0f, 0f, 0f),
                    Quaternion.identity,
                    0
                );

                GameObject mainCamera = GameObject.FindWithTag("MainCamera");
                mainCamera.GetComponent<UnityChan.ThirdPersonCamera>().enabled = true;
            }
        }
    }


    public void ManButtonClicked() {
        Manbuttoncheck++;
        Womanbuttoncheck = 0;
        ManPanel.SetActive(true);
        WomanPanel.SetActive(false);
    }

    public void WomanButtonClicked() {
        Womanbuttoncheck++;
        Manbuttoncheck = 0;
        ManPanel.SetActive(false);
        WomanPanel.SetActive(true);
    }

    public void ResetClicked()
    {
        Womanbuttoncheck = 0;
        Manbuttoncheck = 0;
        ManPanel.SetActive(false);
        WomanPanel.SetActive(false);
    }

    public override void OnCreateRoomFailed(short returnCode, string message) { RoomInput.text = ""; CreateRoom(); } 

    public override void OnJoinRandomFailed(short returnCode, string message) { RoomInput.text = ""; CreateRoom(); }

    #endregion

    #region skybox
    [PunRPC]
    public void SkyBox1()
    {
        RenderSettings.skybox = cloud;
    }
    [PunRPC]
    public void SkyBox2()
    {
        RenderSettings.skybox = star;
    }
    [PunRPC]
    public void SkyBox3()
    {
        RenderSettings.skybox = night;
    }
    [PunRPC]
    public void SkyBox4()
    {
        RenderSettings.skybox = sea;
    }

    public void sky1btn()
    {
        PV.RPC("SkyBox1", RpcTarget.AllBufferedViaServer);
    }

    public void sky2btn()
    {
        PV.RPC("SkyBox2", RpcTarget.AllBufferedViaServer);
    }

    public void sky3btn()
    {
        PV.RPC("SkyBox3", RpcTarget.AllBufferedViaServer);
    }

    public void sky4btn()
    {
        PV.RPC("SkyBox4", RpcTarget.AllBufferedViaServer);
    }
    #endregion

    #region land

    [PunRPC]
    public void call1()
    {
        obj.GetComponent<meshchange>().mesh1();
    }

    [PunRPC]
    public void call2()
    {
        obj.GetComponent<meshchange>().mesh2();
    }

    [PunRPC]
    public void call3()
    {
        obj.GetComponent<meshchange>().mesh3();
    }

    [PunRPC]
    public void call4()
    {
        obj.GetComponent<meshchange>().mesh4();
    }

    public void mesh1btn()
    {
        call1();
        PV.RPC("call1", RpcTarget.AllBufferedViaServer);
    }

    public void mesh2btn()
    {
        call2();
        PV.RPC("call2", RpcTarget.AllBufferedViaServer);
    }

    public void mesh3btn()
    {
        call3();
        PV.RPC("call3", RpcTarget.AllBufferedViaServer);
    } 

    public void mesh4btn()
    {
        call4();
        PV.RPC("call4", RpcTarget.AllBufferedViaServer);
    }  
    public void ok()
    {
        SkyBoxPanel.SetActive(false);
    }
    #endregion
}
