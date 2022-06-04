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


namespace UnityChan
{
	[RequireComponent(typeof(Animator))]
	[RequireComponent(typeof(CapsuleCollider))]
	[RequireComponent(typeof(Rigidbody))]

	public class ControlScript : MonoBehaviourPunCallbacks, IPunObservable
	{
		public float animSpeed = 1.5f;				
		public float lookSmoother = 3.0f;			
		public bool useCurves = true;				
		public float useCurvesHeight = 0.5f;		
		public float forwardSpeed = 7.0f;
		public float backwardSpeed = 2.0f;
		public float rotateSpeed = 2.0f;
		public float jumpPower = 3.0f; 
		private CapsuleCollider col;
		private Rigidbody rb;
		private Vector3 velocity;
		private float orgColHight;
		private Vector3 orgVectColCenter;
		public Animator anim;							
		private AnimatorStateInfo currentBaseState;	

		private GameObject cameraObject;	

		static int idleState = Animator.StringToHash ("Base Layer.Idle");
		static int locoState = Animator.StringToHash ("Base Layer.Locomotion");
		static int jumpState = Animator.StringToHash ("Base Layer.Jump");
		static int restState = Animator.StringToHash ("Base Layer.Rest");

		float scale_ratio = 0.001f;
		float heal_position = 0.00f;
		float head_angle = 25f;
		public string[] str = new string[52];
		Transform[] bone_t;
		Transform[] cube_t;
		Vector3 init_position;
		Quaternion[] init_rot;
		Quaternion[] init_inv;
		int[] bones = new int[10] { 1, 2, 4, 5, 7, 8, 11, 12, 14, 15 };
		int[] child_bones = new int[10] { 2, 3, 5, 6, 8, 10, 12, 13, 15, 16 };
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

		void Start ()
		{
            		BackMusic.SetActive(true);
			anim = GetComponent<Animator> ();
			col = GetComponent<CapsuleCollider> ();
			rb = GetComponent<Rigidbody> ();
			cameraObject = GameObject.FindWithTag ("MainCamera");
			orgColHight = col.height;
			orgVectColCenter = col.center;

		        GetInitInfo();

		        // localEndPoint = new IPEndPoint(IPAddress.Parse("192.168.0.17"), _port);
		        // listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		        // listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
		        // listener.Bind(localEndPoint);

		        nickname = NetworkManager.nickname.ToString();
		        Debug.Log(nickname);
		}

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

		    Vector3 init_forward = TriangleNormal(bone_t[7].position, bone_t[4].position, bone_t[1].position);
		    init_inv[0] = Quaternion.Inverse(Quaternion.LookRotation(init_forward));

		    init_position = bone_t[0].position;
		    init_rot[0] = bone_t[0].rotation;
		    for (int i = 0; i < bones.Length; i++)
		    {
			int b = bones[i];
			int cb = child_bones[i];
			init_rot[b] = bone_t[b].rotation;
			init_inv[b] = Quaternion.Inverse(Quaternion.LookRotation(bone_t[b].position - bone_t[cb].position, init_forward));
		    }
		}

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
			    StreamReader sr = new StreamReader("/home/ji/Desktop/pose_data.csv");
			    string data = sr.ReadLine();
			    //Debug.Log(data);
			    //StreamReader sr = new StreamReader("/home/ji/Desktop/ji/real-time-3d-pose-estimation-with-Unity3D/3d pose App/src/pose_data.csv");
			    //string data = sr.ReadLine();
			    //Debug.Log(data);
			    position_weight = transform.position*1000;
			    if (data != "" && data!=null)
			    {
				str = data.Split(' ');
					    count = str.Last();
				cnt.text = count.ToString();
				now_pos = new Vector3[bone_num];

				for (int i = 0; i < (str.Length-2); i += 3)
				{
				    now_pos[i / 3] = new Vector3(-float.Parse(str[i]), float.Parse(str[i + 2]), -float.Parse(str[i + 1])) + position_weight;
				}

				Vector3 pos_forward = TriangleNormal(now_pos[7], now_pos[4], now_pos[1]);
				bone_t[0].position = now_pos[0] * scale_ratio + new Vector3(init_position.x, heal_position, init_position.z);
				bone_t[0].rotation = Quaternion.LookRotation(pos_forward) * init_rot[0];

				for (int i = 0; i < bones.Length; i++)
				{
				    int b = bones[i];
				    int cb = child_bones[i];
				    bone_t[b].rotation = Quaternion.LookRotation(now_pos[b] - now_pos[cb], pos_forward) * init_inv[b] * init_rot[b];
				}

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
	
		void FixedUpdate ()
		{
		    if(!PV.IsMine)
		    {
			return;
		    }
		    if(pressed == false)
		    {
			float h = Input.GetAxis ("Horizontal");
			float v = Input.GetAxis ("Vertical");
			anim.SetFloat ("Speed", v);
			anim.SetFloat ("Direction", h);
			anim.speed = animSpeed;
			currentBaseState = anim.GetCurrentAnimatorStateInfo (0);
			rb.useGravity = true;

			velocity = new Vector3 (0, 0, v);
			velocity = transform.TransformDirection (velocity);

			if (v > 0.1) {
			    velocity *= forwardSpeed;
			} else if (v < -0.1) {
			    velocity *= backwardSpeed;
			}

			if (Input.GetButtonDown ("Jump")) {
			    if (currentBaseState.nameHash == locoState) {
				if (!anim.IsInTransition (0)) {
				    rb.AddForce (Vector3.up * jumpPower, ForceMode.VelocityChange);
				    anim.SetBool ("Jump", true);
				}
			    }
			}

			transform.localPosition += velocity * Time.fixedDeltaTime;
			transform.Rotate (0, h * rotateSpeed, 0);	
			if (currentBaseState.nameHash == locoState) {
			    if (useCurves) {
				resetCollider ();
			    }
                	}
                

			else if (currentBaseState.nameHash == jumpState) {
			    cameraObject.SendMessage ("setCameraPositionJumpView");
			    if (!anim.IsInTransition (0)) {
				if (useCurves) {

				    float jumpHeight = anim.GetFloat ("JumpHeight");
				    float gravityControl = anim.GetFloat ("GravityControl"); 
				    if (gravityControl > 0)
					rb.useGravity = false;

				    Ray ray = new Ray (transform.position + Vector3.up, -Vector3.up);
				    RaycastHit hitInfo = new RaycastHit ();
				    if (Physics.Raycast (ray, out hitInfo)) {
					if (hitInfo.distance > useCurvesHeight) {
					    col.height = orgColHight - jumpHeight;
					    float adjCenterY = orgVectColCenter.y + jumpHeight;
					    col.center = new Vector3 (0, adjCenterY, 0);
					} else {				
					    resetCollider ();
					}
				    }
				}			
				anim.SetBool ("Jump", false);
			    }
			}

			else if (currentBaseState.nameHash == idleState) {
			    if (useCurves) {
				resetCollider ();
			    }

			    if (Input.GetButtonDown ("Jump")) {
				anim.SetBool ("Rest", true);
			    }
			}

			else if (currentBaseState.nameHash == restState) {
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

		void resetCollider ()
		{
			col.height = orgColHight;
			col.center = orgVectColCenter;
		}
	}
}
