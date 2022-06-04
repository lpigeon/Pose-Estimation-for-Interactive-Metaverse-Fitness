using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using System;


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
    public Button MapSelectBtn;
    

    [Header("ETCPanel")]
    public GameObject GamePanel;
    public GameObject ExitPanel;
    public GameObject SkyBoxPanel;
    public GameObject ManPanel;
    public GameObject WomanPanel;
    public GameObject TilePlane;
    public GameObject HealthPlane;
    public GameObject GrassPlane;
    public GameObject MapCheckPanel;

    [Header("GamePanel")]
    public Material cloud;
    public Material star;
    public Material night;
    public Material sea;
    public Button Ok;
    public GameObject obj;
    public Button ResetMap; 
    public GameObject ExSelect;
    public GameObject ExTypePanel;
    public Text FirstEx;
    public Text SecondEx;
    public Text ThirdEx;
    public GameObject First_Second;
    public GameObject Second_Third;
    public Button SquatBtn;
    public Button LungeBtn;
    public Button BackLungeBtn;
    public Button SelectOK;
    public Button btn1;
    public Button btn2;
    public Button btn3;
    public Button btn4;
    public int SkyNum;
    public int OkNum;
    
    [Header("Exercise")]
    public Slider Timer_20;
    public Slider Timer_10;
    public Button StartEx;
    public int CountDownTime;
    public Text CountDownText;
    public Text ExtimeText;
    public Text RestText;
    public Text StartText;
    public Text RestTimeText;
    float currentTime;
    float currentRestTime;
    public int startTime;
    public int RestTime;
    public GameObject Timer20Panel;
    public GameObject Timer10Panel;
    public GameObject NumberPanel;
    public int ExTime;
    static public int SetTime;
    public Text ChnageExNum;
    public Color Extype1;
    public Color Extype2;
    public Color Extype3;
    public Animator slider_10;
    public Animator slider_20;


    [Header("ETC")]
    public Text StatusText;
    public PhotonView PV;
    public static string nickname;
    public int Manbuttoncheck = 0;
    public int Womanbuttoncheck = 0;
    public int Healthcheck = 0;
    public int Grasscheck = 0;
    public int SelectNum = 0;
    public int ExNum = 0;
    public int SeqNum = 0;
    public int Seq = 0;
    public Slider loading;
    public Animator slider;

    List<RoomInfo> myList = new List<RoomInfo>();
    int currentPage = 1, maxPage, multiple;
    bool timerActive = false;
    bool RestActive = false;

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
        Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);
        Manbuttoncheck = 0;
        Womanbuttoncheck = 0;
        SelectNum = 0;
    }
    void Update()
    {
        StatusText.text = PhotonNetwork.NetworkClientState.ToString();
        LobbyInfoText.text = (PhotonNetwork.CountOfPlayers - PhotonNetwork.CountOfPlayersInRooms) + "로비 / " + PhotonNetwork.CountOfPlayers + "접속";
        //Z입력 부분
        if(Input.GetKeyDown(KeyCode.Z) && SelectNum%2 == 0)
        {
            SelectNum += 1;
            ExSelect.SetActive(true);
            ExTypePanel.SetActive(true);
            ExNum = 1;
        }
        else if(Input.GetKeyDown(KeyCode.Z)&& SelectNum%2 == 1)
        {
            SelectNum += 1;
            SelectOK.Select();
            ExSelect.SetActive(false);
            ExNum = 0;
        }
        //X 입력 부분
        if(Input.GetKeyDown(KeyCode.X))
        {
            if(ExNum == 1)
            {
                SquatBtn.Select();
                ExNum = 2;
                SeqNum = 1;
            }
            else if(ExNum == 2)
            {
                LungeBtn.Select();
                ExNum = 3;
                SeqNum = 1;
            }
            else if(ExNum == 3)
            {
                BackLungeBtn.Select();
                ExNum = 1;
                SeqNum = 1;
            }
        }
        if(Input.GetKeyDown(KeyCode.K))
        {
            if(OkNum == 0)
            {
                if(SkyNum == 1)
                {
                    btn1.Select();
                    sky1btn();
                    SkyNum = 2;
                }
                else if(SkyNum == 2)
                {
                    btn2.Select();
                    sky2btn();
                    SkyNum = 3;
                }
                else if(SkyNum == 3)
                {
                    btn3.Select();
                    sky3btn();
                    SkyNum = 4;
                }
                else if(SkyNum == 4)
                {
                    btn4.Select();
                    sky4btn();
                    SkyNum = 1;
                }
            }
        }
        if(Input.GetKeyDown(KeyCode.L))
        {
            OkNum = 1;
            SkyBoxPanel.SetActive(false);
        }
        //C 입력 부분
        if(Input.GetKeyDown(KeyCode.C) && SeqNum == 1)
        {
            if(ExNum == 2)
            {
                if(Seq == 0)
                {
                    FirstEx.text = "Squat";
                    Seq = 1;
                    First_Second.SetActive(true);
                }
                else if(Seq == 1)
                {
                    SecondEx.text = "Squat";
                    Seq = 2;
                    Second_Third.SetActive(true);
                }
                else if(Seq == 2)
                {
                    ThirdEx.text = "Squat";
                    Seq = 0;
                }
            }
            if(ExNum == 3)
            {
                if(Seq == 0)
                {
                    FirstEx.text = "Lunge";
                    Seq = 1;
                    First_Second.SetActive(true);
                }
                else if(Seq == 1)
                {
                    SecondEx.text = "Lunge";
                    Seq = 2;
                    Second_Third.SetActive(true);
                }
                else if(Seq == 2)
                {
                    ThirdEx.text = "Lunge";
                    Seq = 0;
                }
            }
            if(ExNum == 1)
            {
                if(Seq == 0)
                {
                    FirstEx.text = "Back\nLunge";
                    Seq = 1;
                    First_Second.SetActive(true);
                }
                else if(Seq == 1)
                {
                    SecondEx.text = "Back\nLunge";
                    Seq = 2;
                    Second_Third.SetActive(true);
                }
                else if(Seq == 2)
                {
                    ThirdEx.text = "Back\nLunge";
                    Seq = 0;
                }
            }
        }

        if(Input.GetKeyDown(KeyCode.P))
        {
            ExtimeText.text = "20:00";
            StartExercise();
        }
        if(timerActive == true)
        {
            ChnageExNum.text = ExTime.ToString();
            Timer_20.gameObject.SetActive(true);
            Timer_20.GetComponent<Animator>().SetTrigger("20");
            currentTime = currentTime - Time.deltaTime;
            if(currentTime <= 0)
            {
                currentRestTime = RestTime;
                timerActive = false;
                Timer20Panel.SetActive(false);
                Timer10Panel.SetActive(true);
                startResttimer();
            }
            TimeSpan time = TimeSpan.FromSeconds(currentTime);
            ExtimeText.text = time.Seconds.ToString() + ":" + Mathf.Round(time.Milliseconds*0.1f).ToString();
        }
        else if(RestActive == true)
        {
            StartCoroutine(Rest());
            currentRestTime = currentRestTime - Time.deltaTime;
            if(currentRestTime <= 0)
            {
                currentTime = startTime;
                RestActive = false;
                Timer10Panel.SetActive(false);
                Timer20Panel.SetActive(true);
                startExtimer();
            }
            TimeSpan time = TimeSpan.FromSeconds(currentRestTime);
            RestTimeText.text = time.Seconds.ToString() + ":" + Mathf.Round(time.Milliseconds*0.1f).ToString();
        }
        if(ExTime == 5)
        {
            SetTime += 1;
            if(SetTime <= 3)
            {
                StartCoroutine(CountDownToStart());
            }
        }
        if(SetTime == 1)
        {
            Extype1.a = 1.0f;
            FirstEx.color = Extype1;
        }
        else if(SetTime == 2)
        {
            Extype1.a = 100/255f;
            Extype2.a = 1.0f;
            FirstEx.color = Extype1;
            SecondEx.color = Extype2;
        }
        else if(SetTime == 3)
        {
            Extype1.a = 100/255f;
            Extype2.a = 100/255f;
            Extype3.a = 1.0f;
            FirstEx.color = Extype1;
            SecondEx.color = Extype2;
            ThirdEx.color = Extype3;
        }
        else if(SetTime == 4)
        {
            timerActive = false;
            RestActive = false;
            slider_10.Rebind();
            slider_20.Rebind();
            Timer10Panel.SetActive(false);
            Timer20Panel.SetActive(false);
            NumberPanel.SetActive(false);
            StartEx.gameObject.SetActive(true);
            currentRestTime = 10;
            currentTime = 20;
            CountDownTime = 3;
            SetTime = 0;
            ExTime = 0;
            normaltextcolor();
        }
    }
    
    public void Connect()
    {
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
    public void CreateRoom() => PhotonNetwork.CreateRoom(RoomInput.text == "" ? "Room" + UnityEngine.Random.Range(0, 100) : RoomInput.text, new RoomOptions { MaxPlayers = 4 });

    public void JoinRandomRoom() => PhotonNetwork.JoinRandomRoom();

    public void LeaveRoom() => PhotonNetwork.LeaveRoom();

    public override void OnJoinedRoom()
    {
        if(Healthcheck == 1)
        {
            PV.RPC("HealthMapClicked", RpcTarget.AllBufferedViaServer);
        }
        else if(Grasscheck == 1)
        {
            PV.RPC("GrassMapClicked", RpcTarget.AllBufferedViaServer);
        }
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
            if(PhotonNetwork.IsMasterClient)
            {
                SkyBoxPanel.SetActive(true);
            }
            int randomnum = UnityEngine.Random.Range(0,100);
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
    [PunRPC]
    public void HealthMapClicked(){
        HealthPlane.SetActive(true);
        GrassPlane.SetActive(false);
        TilePlane.SetActive(false);
        Healthcheck = 1;
        Grasscheck = 0;
        if(Healthcheck == 1 || Grasscheck == 1)
        {
            MapCheckPanel.SetActive(false);
        }
    }

    [PunRPC]
    public void GrassMapClicked(){
        HealthPlane.SetActive(false);
        GrassPlane.SetActive(true);
        TilePlane.SetActive(false);
        Healthcheck = 0;
        Grasscheck = 1;
        if(Healthcheck == 1 || Grasscheck == 1)
        {
            MapCheckPanel.SetActive(false);
        }
    }
    
    public void SelectMap()
    {
        MapCheckPanel.SetActive(true);
    }

    public void ResetClicked()
    {
        Womanbuttoncheck = 0;
        Manbuttoncheck = 0;
        Healthcheck = 0;
        Grasscheck = 0;
        ManPanel.SetActive(false);
        WomanPanel.SetActive(false);
        HealthPlane.SetActive(false);
        GrassPlane.SetActive(false);
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

    public void ResetMapped()
    {
        if(PhotonNetwork.IsMasterClient)
        {
            SkyBoxPanel.SetActive(true);
        }
        OkNum = 0;
    }
    #endregion

    #region Timer

    void Start()
    {
        currentTime = startTime;
        currentRestTime = RestTime;
        Extype1 = new Color(1.0f, 1.0f, 1.0f);
        Extype2 = new Color(1.0f, 1.0f, 1.0f);
        Extype3 = new Color(1.0f, 1.0f, 1.0f);
    }

    void StartExercise()
    {
        Timer20Panel.SetActive(true);
        NumberPanel.SetActive(true);
        StartEx.gameObject.SetActive(false);
        Extype1.a = 100/255f;
        Extype2.a = 100/255f;
        Extype3.a = 100/255f;
        FirstEx.color = Extype1;
        SecondEx.color = Extype2;
        ThirdEx.color = Extype3;
        StartCoroutine(CountDownToStart());
        SetTime = 1;
    }

    IEnumerator CountDownToStart()
    {
        while(CountDownTime > 0)
        {
            CountDownText.text = CountDownTime.ToString();
            yield return new WaitForSeconds(1f);
            CountDownTime--;
        }
        ExTime = 0;
        CountDownText.text = "시작";
        yield return new WaitForSeconds(1f);
        CountDownText.text = "";
        Timer_20.gameObject.SetActive(true);
        Timer_20.GetComponent<Animator>().SetTrigger("20");
        startExtimer();
    }

    IEnumerator Rest()
    {
        Timer_10.gameObject.SetActive(true);
        Timer_10.GetComponent<Animator>().SetTrigger("10");
        if(RestActive == true)
        {
            RestText.text = "휴식";
            yield return new WaitForSeconds(1f);
            RestText.text = "";
            RestText.enabled = false;
            StartText.enabled = true;
        }
        if(timerActive == true)
        {
            StartText.text = "시작";
            yield return new WaitForSeconds(1f);
            StartText.text = "";
            StartText.enabled = false;
            RestText.enabled = true;
        }
    }

    void startExtimer()
    {
        timerActive = true;
        ExTime += 1;
    }

    void startResttimer()
    {
        RestActive = true;
    }
    #endregion

    void normaltextcolor()
    {
        Extype1.a = 1.0f;
        Extype2.a = 1.0f;
        Extype3.a = 1.0f;
        FirstEx.color = Extype1;
        SecondEx.color = Extype2;
        ThirdEx.color = Extype3;
    }
}
