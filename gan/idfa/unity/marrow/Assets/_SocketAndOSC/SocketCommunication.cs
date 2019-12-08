using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using BestHTTP;
using BestHTTP.SocketIO;
using Newtonsoft.Json;
using System.Linq;

namespace Marrow
{
	[Serializable]
	public class AttnGanRequestData
	{
		public string caption;
	}

	[Serializable]
    public class Pix2PixRequestData
    {
        public string data;
    }

	[System.Serializable]
	public class ByteArrayEvent : UnityEvent<byte[]> { }

	public class SocketCommunication : MonoBehaviour
	{
		public ByteArrayEvent AttnGanUpdateResponded = new ByteArrayEvent();
		public ByteArrayEvent Pix2PixUpdateResponded = new ByteArrayEvent();

		public enum ServerType { Pix2Pix, AttnGan }

		public ServerType serverType = ServerType.AttnGan;
		public bool devMode;

		[Header("AttnGan")]
		//public string attnGanUrl; //http://t2i.api.marrow.raycaster.studio/socket.io/
		//public int attnGanImageWidth = 256;
		//public int attnGanImageHeight = 256;
		public float typingTimerLength = 0.4f;

		//private SocketManager attnGanSocketManager;
        //private Socket attnGanSocket;
		private Texture2D attnGanTexture;
        private float lastTypingTimecode;
		private bool attnGanIsConnected;

		[Header("Pix2Pix")]
		//public string pix2pixUrl; //https://pix2pix.api.marrow.raycaster.studio/socket.io/
		//public int pix2pixImageWidth = 1280;
		//public int pix2pixImageHeight = 720;
		//public Material pix2pixMaterial;
		public WebcamAccess webcamAccess;
        
		//private SocketManager pix2pixSocketManager;
		//private Socket pix2pixSocket;
		//private Texture2D pix2pixTexture;
		private bool pix2pixIsConnected;
		private Color32[] pix2pixWebcamData;
		private bool emitFirstPix2PixRequest;

		[Space(10)]
        public string serverUrl; //http://pix2pix.api.marrow.raycaster.studio:3333/socket.io/
		[Tooltip("t2i: 256x256; pix2pix: 1280*720")]
        public int imageWidth = 256;
        public int imageHeight = 256;
        public Material genImageMaterial;
        
		private SocketManager genSocketManager;
        private Socket genSocket;
        private Texture2D genTexture;

		private float getPix2PixTimecode;
		private bool socketIsClosing;

		private void Start()
		{
			SocketOptions options = new SocketOptions();
			options.AutoConnect = false;
			//options.ConnectWith = BestHTTP.SocketIO.Transports.TransportTypes.WebSocket;

			genSocketManager = new SocketManager(new Uri(serverUrl), options);
			genSocketManager.Encoder = new BestHTTP.SocketIO.JsonEncoders.LitJsonEncoder();
            
			if (serverType==ServerType.AttnGan)
			{
				genSocket = genSocketManager["/query"];

				genSocket.On("connecting", (socket, packet, args) => Debug.Log("Connecting AttnGan"));
				genSocket.On(SocketIOEventTypes.Connect, OnAttnGanConnect);
				genSocket.On("update_response", OnAttnGanUpdateResponse);
				genSocket.On(SocketIOEventTypes.Disconnect, OnAttnGanDisconnect);
				genSocket.On(SocketIOEventTypes.Error, OnAttnGanError);

				genSocketManager.Open();
			}
			else
			{
				genSocket = genSocketManager["/generate"];

				genSocket.On("connecting", (socket, packet, args) => Debug.Log("Connecting Pix2Pix"));
				genSocket.On(SocketIOEventTypes.Connect, OnPix2PixConnect);
				genSocket.On("update_response", OnPix2PixUpdateResponse);
				genSocket.On(SocketIOEventTypes.Disconnect, OnPix2PixDisconnect);
				genSocket.On(SocketIOEventTypes.Error, OnPix2PixError);

				genSocketManager.Open();
				genTexture = new Texture2D(imageWidth, imageHeight);
				genImageMaterial.SetTexture("_ShadowTex", genTexture);

				pix2pixWebcamData = new Color32[imageWidth * imageHeight];

				Debug.Log("pix2pixSocketManager setup");
			}
		}

		private void Update()
		{
			if (Input.GetKeyDown("escape") && !socketIsClosing)
			{
				StartCoroutine(CloseSocketBeforeEnd());
				socketIsClosing = true;
				return;
			}

			if (!devMode)
				return;
			
			if (serverType == ServerType.AttnGan && Input.GetKeyDown("a"))
			{
				AttnGanRequestData attnGanRequestData = new AttnGanRequestData();
				attnGanRequestData.caption = "apple test";

				// v.1 - send as dictionary
				/*
				Dictionary<string, object> arg = new Dictionary<string, object>();
				arg.Add("caption", "apple");
				t2iSocket.Emit("update_request", arg);
				*/

				// v.2 - send as object
				//string json = JsonConvert.SerializeObject(attnGanRequestData);
				//string json = JsonUtility.ToJson(attnGanRequestData);
				genSocket.Emit("update_request", attnGanRequestData);
				Debug.Log("AttnGanSocket emit update_request");
			}

			if (serverType == ServerType.Pix2Pix && pix2pixIsConnected && !emitFirstPix2PixRequest && webcamAccess.webcamTexture.didUpdateThisFrame)
			{
				EmitPix2PixRequest();
				emitFirstPix2PixRequest = true;
			}
		}

		IEnumerator CloseSocketBeforeEnd()
		{
            genSocketManager.Close();
			Debug.Log("Close socket!!");
			yield return new WaitForSeconds(1f);

            Application.Quit();
		}

		private void OnDestroy()
		{
			if (serverType == ServerType.AttnGan && genSocketManager != null)
			{
				Debug.Log("Close AttnGAN socket");
                genSocketManager.Close();
			}
			if (serverType == ServerType.AttnGan && genSocketManager != null)
			{
				Debug.Log("Close pix2pix socket");
				genSocketManager.Close();
			}
		}

		/////////////////////////////////////////////////
        /////////////////////////////////////////////////
        /////////////////////////////////////////////////

		void OnAttnGanConnect(Socket socket, Packet packet, params object[] args)
		{
			Debug.Log("Connected to AttnGAN.");
			Debug.Log(socket.Id);
			attnGanIsConnected = true;
			EventBus.WebsocketConnected.Invoke();
		}

		void OnAttnGanUpdateResponse(Socket socket, Packet packet, params object[] args)
		{
            Debug.Log("AttnGAN response");
			Dictionary<string, object> data = args[0] as Dictionary<string, object>;
			string base64Image = data["image"] as string;
			byte[] receivedBase64Img = Convert.FromBase64String(base64Image);
			//attnGanTexture.LoadImage(receivedBase64Img);
			AttnGanUpdateResponded.Invoke(receivedBase64Img);
		}

		void OnAttnGanError(Socket socket, Packet packet, params object[] args)
		{
			Debug.LogError(string.Format("--- Error --- {0}", args[0].ToString()));
		}

		public void OnAttnGanInputUpdate(string inputText)
		{
			if ((Time.time - lastTypingTimecode) <= typingTimerLength)
				return;

			AttnGanRequestData attnGanRequestData = new AttnGanRequestData();
			string stringToSend = inputText.ToLower();
			stringToSend += " ";
			attnGanRequestData.caption = inputText.ToLower();
			genSocket.Emit("update_request", attnGanRequestData);

			lastTypingTimecode = Time.time;
			//Debug.LogFormat("socket emit request: {0}", stringToSend);
		}

		public void EmitAttnGanRequest(string inputText)
		{
			AttnGanRequestData attnGanRequestData = new AttnGanRequestData();
            string stringToSend = inputText.ToLower();
            stringToSend += " ";
            attnGanRequestData.caption = inputText.ToLower();
			genSocket.Emit("update_request", attnGanRequestData);
			//Debug.LogFormat("socket emit request: {0}", stringToSend);
		}

		void OnAttnGanDisconnect(Socket socket, Packet packet, params object[] args)
		{
			Debug.Log("AttnGan disonnected!!!");
			EventBus.WebsocketDisconnected.Invoke();
		}

        /////////////////////////////////////////////////
		/////////////////////////////////////////////////
		///////////////////////////////////////////////// 

		void OnPix2PixConnect(Socket socket, Packet packet, params object[] args)
        {
			Debug.LogWarning("Connected to pix2pix.");
            Debug.Log(socket.Id);
			pix2pixIsConnected = true;

            EventBus.WebsocketConnected.Invoke();
			//EmitPix2PixRequest();
        }

        void OnPix2PixUpdateResponse(Socket socket, Packet packet, params object[] args)
        {
			float p2pDuration = Time.time - getPix2PixTimecode; 
			Debug.Log("got pix2pix response!: " + p2pDuration);
			getPix2PixTimecode = Time.time;

            Dictionary<string, object> data = args[0] as Dictionary<string, object>;
            string base64Image = data["results"] as string;

            // https://stackoverflow.com/questions/28015442/converting-base64-string-to-gif-image
            //HashSet<char> whiteSpace = new HashSet<char> { '\t', '\n', '\r', ' ' };
            //int length = base64Image.Count(c => !whiteSpace.Contains(c));
            //if (length % 4 != 0)
            //    base64Image += new string('=', 4 - length % 4); // Pad length to multiple of 4.

            Debug.Log(base64Image.Length);
            if (base64Image.Length < 10000)
                return;
            byte[] receivedBase64Img = Convert.FromBase64String(base64Image);
			//genTexture.LoadImage(receivedBase64Img);

			//genImageMaterial.SetTexture("_ShadowTex", genTexture);
			Pix2PixUpdateResponded.Invoke(receivedBase64Img);
            
			//EmitPix2PixRequest();
        }

        void OnPix2PixError(Socket socket, Packet packet, params object[] args)
        {
			Debug.LogError(string.Format("--- pix2pix Error --- {0}", args[0].ToString()));
        }
        
		public void EmitPix2PixRequest()
        {
			// Send webcam image
			Pix2PixRequestData pix2PixRequestData = new Pix2PixRequestData();
			webcamAccess.webcamTexture.GetPixels32(pix2pixWebcamData);

			// TODO: try resize to reset, instead of create new texture
			Destroy(genTexture);
			genTexture = null;
			genTexture = new Texture2D(imageWidth, imageHeight);
			//pix2pixTexture.Resize(pix2pixImageWidth, pix2pixImageHeight);
			genTexture.SetPixels32(pix2pixWebcamData);
			//pix2pixTexture.Apply();

            // Scale down seems to crash the server???

			TextureScale.Bilinear(genTexture, imageWidth/2, imageHeight/2);
			// Debug.Log(genTexture.width + ", " + genTexture.height);
			     
			byte[] imageData = genTexture.EncodeToJPG();
			string base64Image = Convert.ToBase64String(imageData);
			pix2PixRequestData.data = base64Image;
			genSocket.Emit("update_request", pix2PixRequestData);
			//Debug.LogFormat("socket emit pix2pix request: {0}", base64Image);
			Debug.Log("socket emit pix2pix request");
        }

		void OnPix2PixDisconnect(Socket socket, Packet packet, params object[] args)
        {
			Debug.LogWarning("Pix2Pix websocket disonnected!!!");
            EventBus.WebsocketDisconnected.Invoke();
        }
	}
}
