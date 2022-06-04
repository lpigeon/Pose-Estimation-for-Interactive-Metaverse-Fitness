//
// Mecanimã®ã‚¢ãƒ‹ãƒ¡ãƒ¼ã‚·ãƒ§ãƒ³ãƒ‡ãƒ¼ã‚¿ãŒã€åŽŸç‚¹ã§ç§»å‹•ã—ãªã„å ´åˆã® Rigidbodyä»˜ãã‚³ãƒ³ãƒˆãƒ­ãƒ¼ãƒ©
// ã‚µãƒ³ãƒ—ãƒ«
// 2014/03/13 N.Kobyasahi
//
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Text;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine.UI;

// pos.txtã®ãƒ‡ãƒ¼ã‚¿
// https://github.com/miu200521358/3d-pose-baseline-vmd/blob/master/doc/Output.md
// 0 :Hip
// 1 :RHip
// 2 :RKnee
// 3 :RFoot
// 4 :LHip
// 5 :LKnee
// 6 :LFoot
// 7 :Spine
// 8 :Thorax
// 9 :Neck/Nose
// 10:Head
// 11:LShoulder
// 12:LElbow
// 13:LWrist
// 14:RShoulder
// 15:RElbow
// 16:RWrist

namespace UnityChan
{
// å¿…è¦ãªã‚³ãƒ³ãƒãƒ¼ãƒãƒ³ãƒˆã®åˆ—è¨˜
	[RequireComponent(typeof(Animator))]
	[RequireComponent(typeof(CapsuleCollider))]
	[RequireComponent(typeof(Rigidbody))]

	public class ControlScript : MonoBehaviourPunCallbacks, IPunObservable
	{

		public float animSpeed = 1.5f;				// ã‚¢ãƒ‹ãƒ¡ãƒ¼ã‚·ãƒ§ãƒ³å†ç”Ÿé€Ÿåº¦è¨­å®š
		public float lookSmoother = 3.0f;			// a smoothing setting for camera motion
		public bool useCurves = true;				// Mecanimã§ã‚«ãƒ¼ãƒ–èª¿æ•´ã‚’ä½¿ã†ã‹è¨­å®šã™ã‚‹
		// ã“ã®ã‚¹ã‚¤ãƒƒãƒãŒå…¥ã£ã¦ã„ãªã„ã¨ã‚«ãƒ¼ãƒ–ã¯ä½¿ã‚ã‚Œãªã„
		public float useCurvesHeight = 0.5f;		// ã‚«ãƒ¼ãƒ–è£œæ­£ã®æœ‰åŠ¹é«˜ã•ï¼ˆåœ°é¢ã‚’ã™ã‚ŠæŠœã‘ã‚„ã™ã„æ™‚ã«ã¯å¤§ããã™ã‚‹ï¼‰

		// ä»¥ä¸‹ã‚­ãƒ£ãƒ©ã‚¯ã‚¿ãƒ¼ã‚³ãƒ³ãƒˆãƒ­ãƒ¼ãƒ©ç”¨ãƒ‘ãƒ©ãƒ¡ã‚¿
		// å‰é€²é€Ÿåº¦
		public float forwardSpeed = 7.0f;
		// å¾Œé€€é€Ÿåº¦
		public float backwardSpeed = 2.0f;
		// æ—‹å›žé€Ÿåº¦
		public float rotateSpeed = 2.0f;
		// ã‚¸ãƒ£ãƒ³ãƒ—å¨åŠ›
		public float jumpPower = 3.0f; 
		// ã‚­ãƒ£ãƒ©ã‚¯ã‚¿ãƒ¼ã‚³ãƒ³ãƒˆãƒ­ãƒ¼ãƒ©ï¼ˆã‚«ãƒ—ã‚»ãƒ«ã‚³ãƒ©ã‚¤ãƒ€ï¼‰ã®å‚ç…§
		private CapsuleCollider col;
		private Rigidbody rb;
		// ã‚­ãƒ£ãƒ©ã‚¯ã‚¿ãƒ¼ã‚³ãƒ³ãƒˆãƒ­ãƒ¼ãƒ©ï¼ˆã‚«ãƒ—ã‚»ãƒ«ã‚³ãƒ©ã‚¤ãƒ€ï¼‰ã®ç§»å‹•é‡
		private Vector3 velocity;
		// CapsuleColliderã§è¨­å®šã•ã‚Œã¦ã„ã‚‹ã‚³ãƒ©ã‚¤ãƒ€ã®Heihtã€Centerã®åˆæœŸå€¤ã‚’åŽã‚ã‚‹å¤‰æ•°
		private float orgColHight;
		private Vector3 orgVectColCenter;
		public Animator anim;							// ã‚­ãƒ£ãƒ©ã«ã‚¢ã‚¿ãƒƒãƒã•ã‚Œã‚‹ã‚¢ãƒ‹ãƒ¡ãƒ¼ã‚¿ãƒ¼ã¸ã®å‚ç…§
		private AnimatorStateInfo currentBaseState;			// base layerã§ä½¿ã‚ã‚Œã‚‹ã€ã‚¢ãƒ‹ãƒ¡ãƒ¼ã‚¿ãƒ¼ã®ç¾åœ¨ã®çŠ¶æ…‹ã®å‚ç…§

		private GameObject cameraObject;	// ãƒ¡ã‚¤ãƒ³ã‚«ãƒ¡ãƒ©ã¸ã®å‚ç…§
		
		// ã‚¢ãƒ‹ãƒ¡ãƒ¼ã‚¿ãƒ¼å„ã‚¹ãƒ†ãƒ¼ãƒˆã¸ã®å‚ç…§
		static int idleState = Animator.StringToHash ("Base Layer.Idle");
		static int locoState = Animator.StringToHash ("Base Layer.Locomotion");
		static int jumpState = Animator.StringToHash ("Base Layer.Jump");
		static int restState = Animator.StringToHash ("Base Layer.Rest");

        //pos.cs start
        float scale_ratio = 0.001f;  // pos.txtã¨Unityãƒ¢ãƒ‡ãƒ«ã®ã‚¹ã‚±ãƒ¼ãƒ«æ¯”çŽ‡
                                 // pos.txtã®å˜ä½ã¯mmã§Unityã¯mã®ãŸã‚ã€0.001ã«è¿‘ã„å€¤ã‚’æŒ‡å®šã€‚ãƒ¢ãƒ‡ãƒ«ã®å¤§ãã•ã«ã‚ˆã£ã¦èª¿æ•´ã™ã‚‹
        float heal_position = 0.00f; // è¶³ã®æ²ˆã¿ã®è£œæ­£å€¤(å˜ä½ï¼šm)ã€‚ãƒ—ãƒ©ã‚¹å€¤ã§ä½“å…¨ä½“ãŒä¸Šã¸ç§»å‹•ã™ã‚‹
        float head_angle = 25f; // é¡”ã®å‘ãã®èª¿æ•´ é¡”ã‚’15åº¦ä¸Šã’ã‚‹

        public string[] str = new string[52];
        //public String pos_filename; // pos.txtã®ãƒ•ã‚¡ã‚¤ãƒ«å
        //public int start_frame; // é–‹å§‹ãƒ•ãƒ¬ãƒ¼ãƒ 
        //public String end_frame; // çµ‚äº†ãƒ•ãƒ¬ãƒ¼ãƒ   
        //float play_time; // å†ç”Ÿæ™‚é–“ 
        Transform[] bone_t; // ãƒ¢ãƒ‡ãƒ«ã®ãƒœãƒ¼ãƒ³ã®Transform
        Transform[] cube_t; // ãƒ‡ãƒãƒƒã‚¯è¡¨ç¤ºç”¨ã®Cubeã®Transform
        Vector3 init_position; // åˆæœŸã®ã‚»ãƒ³ã‚¿ãƒ¼ã®ä½ç½®
        Quaternion[] init_rot; // åˆæœŸã®å›žè»¢å€¤
        Quaternion[] init_inv; // åˆæœŸã®ãƒœãƒ¼ãƒ³ã®æ–¹å‘ã‹ã‚‰è¨ˆç®—ã•ã‚Œã‚‹ã‚¯ã‚ªãƒ¼ã‚¿ãƒ‹ã‚ªãƒ³ã®Inverse
        //List<Vector3[]> pos; // pos.txtã®ãƒ‡ãƒ¼ã‚¿ã‚’ä¿æŒã™ã‚‹ã‚³ãƒ³ãƒ†ãƒŠ
        int[] bones = new int[10] { 1, 2, 4, 5, 7, 8, 11, 12, 14, 15 }; // è¦ªãƒœãƒ¼ãƒ³
        int[] child_bones = new int[10] { 2, 3, 5, 6, 8, 10, 12, 13, 15, 16 }; // bonesã«å¯¾å¿œã™ã‚‹å­ãƒœãƒ¼ãƒ³
        static int bone_num = 17;
        string data = null;
        string count;
        int _port = 5005;
        int bytesRec = 0;
        byte[] buffer = new Byte[1024];
        IPEndPoint localEndPoint = null;
        Socket listener = null;
        Socket socket = null;
        static Vector3[] now_pos = new Vector3[bone_num];

        public GameObject PhotonObject;
        bool pressed = false;
        Vector3 position_weight;
        
        public PhotonView PV;

        private const byte POSE_CHANGE = 0;
        int cheak_end = 0;
        public string nickname = null;
        public Text cnt;
        public int SetTime;
        public GameObject GameMusic;
        public GameObject BackMusic;

		// åˆæœŸåŒ–
		void Start ()
		{
                        BackMusic.SetActive(true);
			// Animatorã‚³ãƒ³ãƒãƒ¼ãƒãƒ³ãƒˆã‚’å–å¾—ã™ã‚‹
			anim = GetComponent<Animator> ();
			// CapsuleColliderã‚³ãƒ³ãƒãƒ¼ãƒãƒ³ãƒˆã‚’å–å¾—ã™ã‚‹ï¼ˆã‚«ãƒ—ã‚»ãƒ«åž‹ã‚³ãƒªã‚¸ãƒ§ãƒ³ï¼‰
			col = GetComponent<CapsuleCollider> ();
			rb = GetComponent<Rigidbody> ();
			//ãƒ¡ã‚¤ãƒ³ã‚«ãƒ¡ãƒ©ã‚’å–å¾—ã™ã‚‹
			cameraObject = GameObject.FindWithTag ("MainCamera");
			// CapsuleColliderã‚³ãƒ³ãƒãƒ¼ãƒãƒ³ãƒˆã®Heightã€Centerã®åˆæœŸå€¤ã‚’ä¿å­˜ã™ã‚‹
			orgColHight = col.height;
			orgVectColCenter = col.center;

            GetInitInfo();

            //localEndPoint = new IPEndPoint(IPAddress.Parse("192.168.35.178"), _port);
            //listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            //listener.Bind(localEndPoint);
         

            // OnlineUIì—ì„œ staticìœ¼ë¡œ ì„ ì–¸ëœ nickname import
            nickname = NetworkManager.nickname.ToString();
            Debug.Log(nickname);
     
		}

        // BoneTransformã®å–å¾—ã€‚å›žè»¢ã®åˆæœŸå€¤ã‚’å–å¾—
        void GetInitInfo()
        {
            bone_t = new Transform[bone_num];
            init_rot = new Quaternion[bone_num];
            init_inv = new Quaternion[bone_num];

            bone_t[0] = anim.GetBoneTransform(HumanBodyBones.Hips);
            bone_t[1] = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            bone_t[2] = anim.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            bone_t[3] = anim.GetBoneTransform(HumanBodyBones.RightFoot);
            bone_t[4] = anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            bone_t[5] = anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            bone_t[6] = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
            bone_t[7] = anim.GetBoneTransform(HumanBodyBones.Spine);
            bone_t[8] = anim.GetBoneTransform(HumanBodyBones.Neck);
            bone_t[10] = anim.GetBoneTransform(HumanBodyBones.Head);
            bone_t[11] = anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            bone_t[12] = anim.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            bone_t[13] = anim.GetBoneTransform(HumanBodyBones.LeftHand);
            bone_t[14] = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
            bone_t[15] = anim.GetBoneTransform(HumanBodyBones.RightLowerArm);
            bone_t[16] = anim.GetBoneTransform(HumanBodyBones.RightHand);

            // Spine,LHip,RHipã§ä¸‰è§’å½¢ã‚’ä½œã£ã¦ãã‚Œã‚’å‰æ–¹å‘ã¨ã™ã‚‹ã€‚
            Vector3 init_forward = TriangleNormal(bone_t[7].position, bone_t[4].position, bone_t[1].position);
            init_inv[0] = Quaternion.Inverse(Quaternion.LookRotation(init_forward));

            init_position = bone_t[0].position;
            init_rot[0] = bone_t[0].rotation;
            for (int i = 0; i < bones.Length; i++)
            {
                int b = bones[i];
                int cb = child_bones[i];

                // å¯¾è±¡ãƒ¢ãƒ‡ãƒ«ã®å›žè»¢ã®åˆæœŸå€¤
                init_rot[b] = bone_t[b].rotation;
                // åˆæœŸã®ãƒœãƒ¼ãƒ³ã®æ–¹å‘ã‹ã‚‰è¨ˆç®—ã•ã‚Œã‚‹ã‚¯ã‚ªãƒ¼ã‚¿ãƒ‹ã‚ªãƒ³
                init_inv[b] = Quaternion.Inverse(Quaternion.LookRotation(bone_t[b].position - bone_t[cb].position, init_forward));
            }
        }

        // æŒ‡å®šã®3ç‚¹ã§ã§ãã‚‹ä¸‰è§’å½¢ã«ç›´äº¤ã™ã‚‹é•·ã•1ã®ãƒ™ã‚¯ãƒˆãƒ«ã‚’è¿”ã™
        Vector3 TriangleNormal(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 d1 = a - b;
            Vector3 d2 = a - c;

            Vector3 dd = Vector3.Cross(d1, d2);
            dd.Normalize();

            return dd;
        }
	
        void Update()
        {
		SetTime = NetworkManager.SetTime;
            if(PV.IsMine)
            {
                if(Input.GetKey(KeyCode.P) && pressed == false)
                {
                    //GameMusic.SetActive(false);
                    pressed = true;
                    gameObject.GetComponent<Animator>().enabled = false;
                }


                if(pressed == true)
                {
			if(SetTime == 0)
			{
			    GameMusic.SetActive(false);
			    pressed = false;
			    gameObject.GetComponent<Animator>().enabled = true;
			}
                    GameMusic.SetActive(true);
                    BackMusic.SetActive(false);
                    //listener.Listen(10);
                    //socket = listener.Accept();

                    //int bytesRec = socket.Receive(buffer);
                   // data = Encoding.ASCII.GetString(buffer, 0, bytesRec);
			StreamReader sr = new StreamReader("/home/ji/Desktop/pose_data.csv");
		    string data = sr.ReadLine();

                    position_weight = transform.position*1000;
                    if (data != "" && data!=null)
                    {
                        
               
                        str = data.Split(' ');
				count = str.Last();
                        cnt.text = count.ToString();
                        now_pos = new Vector3[bone_num];
                        for (int i = 0; i < (str.Length-2); i += 3)
                        {
                            now_pos[i / 3] = new Vector3(-float.Parse(str[i]), float.Parse(str[i + 2]), -float.Parse(str[i + 1])) + position_weight; // position weight add
                        }

                        // ã‚»ãƒ³ã‚¿ãƒ¼ã®ç§»å‹•ã¨å›žè»¢
                        Vector3 pos_forward = TriangleNormal(now_pos[7], now_pos[4], now_pos[1]);
                        bone_t[0].position = now_pos[0] * scale_ratio + new Vector3(init_position.x, heal_position, init_position.z);
                        bone_t[0].rotation = Quaternion.LookRotation(pos_forward) * init_rot[0];

                        // å„ãƒœãƒ¼ãƒ³ã®å›žè»¢
                        for (int i = 0; i < bones.Length; i++)
                        {
                            int b = bones[i];
                            int cb = child_bones[i];
                            bone_t[b].rotation = Quaternion.LookRotation(now_pos[b] - now_pos[cb], pos_forward) * init_inv[b] * init_rot[b];
                        }

                        // é¡”ã®å‘ãã‚’ä¸Šã’ã‚‹èª¿æ•´ã€‚ä¸¡è‚©ã‚’çµã¶ç·šã‚’è»¸ã¨ã—ã¦å›žè»¢
                        bone_t[8].rotation = Quaternion.AngleAxis(head_angle, bone_t[11].position - bone_t[14].position) * bone_t[8].rotation;

                        buffer = new Byte[1024];
                        data = null;
                        bytesRec = 0;
                    }
                    PV.RPC("sendNickname", RpcTarget.AllBuffered, nickname);
                    SendEvent();
                }
            }
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if(stream.IsWriting) stream.SendNext(cnt.text);
            else cnt.text = (string)stream.ReceiveNext();
        }

        // rpcë¥¼ ì‚¬ìš©í•˜ì—¬ nickname ë™ê¸°í™”
        [PunRPC]

        void sendNickname(string _nickname)
        {
            nickname = _nickname;
        }

        void SendEvent()
        {
            object[] datas = new object[] {bone_t[0].position, bone_t[0].rotation, bone_t[1].rotation, bone_t[2].rotation, bone_t[4].rotation, bone_t[5].rotation, bone_t[7].rotation, bone_t[8].rotation, bone_t[11].rotation, bone_t[12].rotation, bone_t[14].rotation, bone_t[15].rotation, nickname};
            PhotonNetwork.RaiseEvent(POSE_CHANGE, datas, RaiseEventOptions.Default, SendOptions.SendUnreliable);
        }
        private void OnEnable()
        {
            PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
        }

        private void OnDisable()
        {
            PhotonNetwork.NetworkingClient.EventReceived -= OnEvent;
        }

        private void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code != 0)
            {
                cheak_end += 1;
            }
            else if (photonEvent.Code == 0)
            {
                object[] datas = (object[])photonEvent.CustomData;
                Debug.Log("nickname is " + nickname);
                Debug.Log("datas[12] is " + (string)datas[12]);
                
                // rpcì—ì„œ ìž…ë ¥ëœ nicknameê³¼ raiseeventì—ì„œ ìž…ë ¥ëœ datas[12]ì˜ ê°’ì´ ê°™ì€ì§€ í™•ì¸. ì¦‰, ìžì‹ ì˜ í´ë¡ ì´ ë§žëŠ”ì§€ í™•ì¸.
                if(nickname == (string)datas[12])
                {
                    Debug.Log("ok");
                    gameObject.GetComponent<Animator>().enabled = false;
                    bone_t[0].position = (Vector3)datas[0];
                    bone_t[0].rotation = (Quaternion)datas[1];
                    bone_t[1].rotation = (Quaternion)datas[2];
                    bone_t[2].rotation = (Quaternion)datas[3];
                    bone_t[4].rotation = (Quaternion)datas[4];
                    bone_t[5].rotation = (Quaternion)datas[5];
                    bone_t[7].rotation = (Quaternion)datas[6];
                    bone_t[8].rotation = (Quaternion)datas[7];
                    bone_t[11].rotation = (Quaternion)datas[8];
                    bone_t[12].rotation = (Quaternion)datas[9];
                    bone_t[14].rotation = (Quaternion)datas[10];
                    bone_t[15].rotation = (Quaternion)datas[11];
                }
            }
            else if (cheak_end > 10 && !PV.IsMine)
            {
                gameObject.GetComponent<Animator>().enabled = true;
            }
        }
	
		// ä»¥ä¸‹ã€ãƒ¡ã‚¤ãƒ³å‡¦ç†.ãƒªã‚¸ãƒƒãƒ‰ãƒœãƒ‡ã‚£ã¨çµ¡ã‚ã‚‹ã®ã§ã€FixedUpdateå†…ã§å‡¦ç†ã‚’è¡Œã†.
		void FixedUpdate ()
		{
			if(!PV.IsMine)
			{
				return;
			}
            if(pressed == false){
                float h = Input.GetAxis ("Horizontal");				// å…¥åŠ›ãƒ‡ãƒã‚¤ã‚¹ã®æ°´å¹³è»¸ã‚’hã§å®šç¾©
                float v = Input.GetAxis ("Vertical");				// å…¥åŠ›ãƒ‡ãƒã‚¤ã‚¹ã®åž‚ç›´è»¸ã‚’vã§å®šç¾©
                anim.SetFloat ("Speed", v);							// Animatorå´ã§è¨­å®šã—ã¦ã„ã‚‹"Speed"ãƒ‘ãƒ©ãƒ¡ã‚¿ã«vã‚’æ¸¡ã™
                anim.SetFloat ("Direction", h); 						// Animatorå´ã§è¨­å®šã—ã¦ã„ã‚‹"Direction"ãƒ‘ãƒ©ãƒ¡ã‚¿ã«hã‚’æ¸¡ã™
                anim.speed = animSpeed;								// Animatorã®ãƒ¢ãƒ¼ã‚·ãƒ§ãƒ³å†ç”Ÿé€Ÿåº¦ã« animSpeedã‚’è¨­å®šã™ã‚‹
                currentBaseState = anim.GetCurrentAnimatorStateInfo (0);	// å‚ç…§ç”¨ã®ã‚¹ãƒ†ãƒ¼ãƒˆå¤‰æ•°ã«Base Layer (0)ã®ç¾åœ¨ã®ã‚¹ãƒ†ãƒ¼ãƒˆã‚’è¨­å®šã™ã‚‹
                rb.useGravity = true;//ã‚¸ãƒ£ãƒ³ãƒ—ä¸­ã«é‡åŠ›ã‚’åˆ‡ã‚‹ã®ã§ã€ãã‚Œä»¥å¤–ã¯é‡åŠ›ã®å½±éŸ¿ã‚’å—ã‘ã‚‹ã‚ˆã†ã«ã™ã‚‹
            
            
            
                // ä»¥ä¸‹ã€ã‚­ãƒ£ãƒ©ã‚¯ã‚¿ãƒ¼ã®ç§»å‹•å‡¦ç†
                velocity = new Vector3 (0, 0, v);		// ä¸Šä¸‹ã®ã‚­ãƒ¼å…¥åŠ›ã‹ã‚‰Zè»¸æ–¹å‘ã®ç§»å‹•é‡ã‚’å–å¾—
                // ã‚­ãƒ£ãƒ©ã‚¯ã‚¿ãƒ¼ã®ãƒ­ãƒ¼ã‚«ãƒ«ç©ºé–“ã§ã®æ–¹å‘ã«å¤‰æ›
                velocity = transform.TransformDirection (velocity);
                //ä»¥ä¸‹ã®vã®é–¾å€¤ã¯ã€Mecanimå´ã®ãƒˆãƒ©ãƒ³ã‚¸ã‚·ãƒ§ãƒ³ã¨ä¸€ç·’ã«èª¿æ•´ã™ã‚‹
                if (v > 0.1) {
                    velocity *= forwardSpeed;		// ç§»å‹•é€Ÿåº¦ã‚’æŽ›ã‘ã‚‹
                } else if (v < -0.1) {
                    velocity *= backwardSpeed;	// ç§»å‹•é€Ÿåº¦ã‚’æŽ›ã‘ã‚‹
                }
            
                if (Input.GetButtonDown ("Jump")) {	// ã‚¹ãƒšãƒ¼ã‚¹ã‚­ãƒ¼ã‚’å…¥åŠ›ã—ãŸã‚‰
                    //ã‚¢ãƒ‹ãƒ¡ãƒ¼ã‚·ãƒ§ãƒ³ã®ã‚¹ãƒ†ãƒ¼ãƒˆãŒLocomotionã®æœ€ä¸­ã®ã¿ã‚¸ãƒ£ãƒ³ãƒ—ã§ãã‚‹
                    if (currentBaseState.nameHash == locoState) {
                        //ã‚¹ãƒ†ãƒ¼ãƒˆé·ç§»ä¸­ã§ãªã‹ã£ãŸã‚‰ã‚¸ãƒ£ãƒ³ãƒ—ã§ãã‚‹
                        if (!anim.IsInTransition (0)) {
                            rb.AddForce (Vector3.up * jumpPower, ForceMode.VelocityChange);
                            anim.SetBool ("Jump", true);		// Animatorã«ã‚¸ãƒ£ãƒ³ãƒ—ã«åˆ‡ã‚Šæ›¿ãˆã‚‹ãƒ•ãƒ©ã‚°ã‚’é€ã‚‹
                        }
                    }
                }
            

                // ä¸Šä¸‹ã®ã‚­ãƒ¼å…¥åŠ›ã§ã‚­ãƒ£ãƒ©ã‚¯ã‚¿ãƒ¼ã‚’ç§»å‹•ã•ã›ã‚‹
                transform.localPosition += velocity * Time.fixedDeltaTime;

                // å·¦å³ã®ã‚­ãƒ¼å…¥åŠ›ã§ã‚­ãƒ£ãƒ©ã‚¯ã‚¿ã‚’Yè»¸ã§æ—‹å›žã•ã›ã‚‹
                transform.Rotate (0, h * rotateSpeed, 0);	
        

                // ä»¥ä¸‹ã€Animatorã®å„ã‚¹ãƒ†ãƒ¼ãƒˆä¸­ã§ã®å‡¦ç†
                // Locomotionä¸­
                // ç¾åœ¨ã®ãƒ™ãƒ¼ã‚¹ãƒ¬ã‚¤ãƒ¤ãƒ¼ãŒlocoStateã®æ™‚
                if (currentBaseState.nameHash == locoState) {
                    //ã‚«ãƒ¼ãƒ–ã§ã‚³ãƒ©ã‚¤ãƒ€èª¿æ•´ã‚’ã—ã¦ã„ã‚‹æ™‚ã¯ã€å¿µã®ãŸã‚ã«ãƒªã‚»ãƒƒãƒˆã™ã‚‹
                    if (useCurves) {
                        resetCollider ();
                    }
                }
                
                // JUMPä¸­ã®å‡¦ç†
                // ç¾åœ¨ã®ãƒ™ãƒ¼ã‚¹ãƒ¬ã‚¤ãƒ¤ãƒ¼ãŒjumpStateã®æ™‚
                else if (currentBaseState.nameHash == jumpState) {
                    cameraObject.SendMessage ("setCameraPositionJumpView");	// ã‚¸ãƒ£ãƒ³ãƒ—ä¸­ã®ã‚«ãƒ¡ãƒ©ã«å¤‰æ›´
                    // ã‚¹ãƒ†ãƒ¼ãƒˆãŒãƒˆãƒ©ãƒ³ã‚¸ã‚·ãƒ§ãƒ³ä¸­ã§ãªã„å ´åˆ
                    if (!anim.IsInTransition (0)) {
                    
                        // ä»¥ä¸‹ã€ã‚«ãƒ¼ãƒ–èª¿æ•´ã‚’ã™ã‚‹å ´åˆã®å‡¦ç†
                        if (useCurves) {
                            // ä»¥ä¸‹JUMP00ã‚¢ãƒ‹ãƒ¡ãƒ¼ã‚·ãƒ§ãƒ³ã«ã¤ã„ã¦ã„ã‚‹ã‚«ãƒ¼ãƒ–JumpHeightã¨GravityControl
                            // JumpHeight:JUMP00ã§ã®ã‚¸ãƒ£ãƒ³ãƒ—ã®é«˜ã•ï¼ˆ0ã€œ1ï¼‰
                            // GravityControl:1â‡’ã‚¸ãƒ£ãƒ³ãƒ—ä¸­ï¼ˆé‡åŠ›ç„¡åŠ¹ï¼‰ã€0â‡’é‡åŠ›æœ‰åŠ¹
                            float jumpHeight = anim.GetFloat ("JumpHeight");
                            float gravityControl = anim.GetFloat ("GravityControl"); 
                            if (gravityControl > 0)
                                rb.useGravity = false;	//ã‚¸ãƒ£ãƒ³ãƒ—ä¸­ã®é‡åŠ›ã®å½±éŸ¿ã‚’åˆ‡ã‚‹
                                            
                            // ãƒ¬ã‚¤ã‚­ãƒ£ã‚¹ãƒˆã‚’ã‚­ãƒ£ãƒ©ã‚¯ã‚¿ãƒ¼ã®ã‚»ãƒ³ã‚¿ãƒ¼ã‹ã‚‰è½ã¨ã™
                            Ray ray = new Ray (transform.position + Vector3.up, -Vector3.up);
                            RaycastHit hitInfo = new RaycastHit ();
                            // é«˜ã•ãŒ useCurvesHeight ä»¥ä¸Šã‚ã‚‹æ™‚ã®ã¿ã€ã‚³ãƒ©ã‚¤ãƒ€ãƒ¼ã®é«˜ã•ã¨ä¸­å¿ƒã‚’JUMP00ã‚¢ãƒ‹ãƒ¡ãƒ¼ã‚·ãƒ§ãƒ³ã«ã¤ã„ã¦ã„ã‚‹ã‚«ãƒ¼ãƒ–ã§èª¿æ•´ã™ã‚‹
                            if (Physics.Raycast (ray, out hitInfo)) {
                                if (hitInfo.distance > useCurvesHeight) {
                                    col.height = orgColHight - jumpHeight;			// èª¿æ•´ã•ã‚ŒãŸã‚³ãƒ©ã‚¤ãƒ€ãƒ¼ã®é«˜ã•
                                    float adjCenterY = orgVectColCenter.y + jumpHeight;
                                    col.center = new Vector3 (0, adjCenterY, 0);	// èª¿æ•´ã•ã‚ŒãŸã‚³ãƒ©ã‚¤ãƒ€ãƒ¼ã®ã‚»ãƒ³ã‚¿ãƒ¼
                                } else {
                                    // é–¾å€¤ã‚ˆã‚Šã‚‚ä½Žã„æ™‚ã«ã¯åˆæœŸå€¤ã«æˆ»ã™ï¼ˆå¿µã®ãŸã‚ï¼‰					
                                    resetCollider ();
                                }
                            }
                        }
                        // Jump boolå€¤ã‚’ãƒªã‚»ãƒƒãƒˆã™ã‚‹ï¼ˆãƒ«ãƒ¼ãƒ—ã—ãªã„ã‚ˆã†ã«ã™ã‚‹ï¼‰				
                        anim.SetBool ("Jump", false);
                    }
                }
                // IDLEä¸­ã®å‡¦ç†
                // ç¾åœ¨ã®ãƒ™ãƒ¼ã‚¹ãƒ¬ã‚¤ãƒ¤ãƒ¼ãŒidleStateã®æ™‚
                else if (currentBaseState.nameHash == idleState) {
                    //ã‚«ãƒ¼ãƒ–ã§ã‚³ãƒ©ã‚¤ãƒ€èª¿æ•´ã‚’ã—ã¦ã„ã‚‹æ™‚ã¯ã€å¿µã®ãŸã‚ã«ãƒªã‚»ãƒƒãƒˆã™ã‚‹
                    if (useCurves) {
                        resetCollider ();
                    }
                    // ã‚¹ãƒšãƒ¼ã‚¹ã‚­ãƒ¼ã‚’å…¥åŠ›ã—ãŸã‚‰RestçŠ¶æ…‹ã«ãªã‚‹
                    if (Input.GetButtonDown ("Jump")) {
                        anim.SetBool ("Rest", true);
                    }
                }
                // RESTä¸­ã®å‡¦ç†
                // ç¾åœ¨ã®ãƒ™ãƒ¼ã‚¹ãƒ¬ã‚¤ãƒ¤ãƒ¼ãŒrestStateã®æ™‚
                else if (currentBaseState.nameHash == restState) {
                    //cameraObject.SendMessage("setCameraPositionFrontView");		// ã‚«ãƒ¡ãƒ©ã‚’æ­£é¢ã«åˆ‡ã‚Šæ›¿ãˆã‚‹
                    // ã‚¹ãƒ†ãƒ¼ãƒˆãŒé·ç§»ä¸­ã§ãªã„å ´åˆã€Rest boolå€¤ã‚’ãƒªã‚»ãƒƒãƒˆã™ã‚‹ï¼ˆãƒ«ãƒ¼ãƒ—ã—ãªã„ã‚ˆã†ã«ã™ã‚‹ï¼‰
                    if (!anim.IsInTransition (0)) {
                        anim.SetBool ("Rest", false);
                    }
                }
            }
		}

		void OnGUI ()
		{
			//GUI.Box (new Rect (Screen.width - 260, 10, 250, 150), "Interaction");
			//GUI.Label (new Rect (Screen.width - 245, 30, 250, 30), "Up/Down Arrow : Go Forwald/Go Back");
			//GUI.Label (new Rect (Screen.width - 245, 50, 250, 30), "Left/Right Arrow : Turn Left/Turn Right");
			//GUI.Label (new Rect (Screen.width - 245, 70, 250, 30), "Hit Space key while Running : Jump");
			//GUI.Label (new Rect (Screen.width - 245, 90, 250, 30), "Hit Spase key while Stopping : Rest");
			//GUI.Label (new Rect (Screen.width - 245, 110, 250, 30), "Left Control : Front Camera");
			//GUI.Label (new Rect (Screen.width - 245, 130, 250, 30), "Alt : LookAt Camera");
		}


		// ã‚­ãƒ£ãƒ©ã‚¯ã‚¿ãƒ¼ã®ã‚³ãƒ©ã‚¤ãƒ€ãƒ¼ã‚µã‚¤ã‚ºã®ãƒªã‚»ãƒƒãƒˆé–¢æ•°
		void resetCollider ()
		{
			// ã‚³ãƒ³ãƒãƒ¼ãƒãƒ³ãƒˆã®Heightã€Centerã®åˆæœŸå€¤ã‚’æˆ»ã™
			col.height = orgColHight;
			col.center = orgVectColCenter;
		}
	}
}
