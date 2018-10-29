using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using BestHTTP;
using BestHTTP.SocketIO;
using Newtonsoft.Json;

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

	public class SocketCommunication : MonoBehaviour
	{
		public UnityEvent AttnGanUpdateResponded = new UnityEvent();

		public enum ServerType { Pix2Pix, AttnGan }

		public ServerType serverType = ServerType.AttnGan;

		public string attnGanUrl; //http://api.marrow.raycaster.studio:3333/socket.io/
		public int attnGanImageWidth = 256;
		public int attnGanImageHeight = 256;
		public float typingTimerLength = 0.5f;
		public Material attnGanMaterial;

		private SocketManager attnGanSocketManager;
        private Socket attnGanSocket;
		private Texture2D attnGanTexture;
        private float lastTypingTimecode;

        [Space(10)]
		public string pix2pixUrl; //http://pix2pix.api.marrow.raycaster.studio:3333/socket.io/
		public int pix2pixImageWidth = 256;
        public int pix2pixImageHeight = 256;
		public Material pix2pixMaterial;
		public Material webcamMaterial;
        
		private SocketManager pix2pixSocketManager;
		private Socket pix2pixSocket;
		private Texture2D pix2pixTexture;
		private bool pix2pixIsConnected;
        //private float lastTypingTimecode;

		[Space(10)]
        public string serverUrl; //http://pix2pix.api.marrow.raycaster.studio:3333/socket.io/
        public int imageWidth = 256;
        public int imageHeight = 256;
        public Material imageMaterial;
        
		private SocketManager genSocketManager;
        private Socket genSocket;
        private Texture2D genTexture;

		private void Start()
		{
			SocketOptions options = new SocketOptions();
			options.AutoConnect = false;
			//options.ConnectWith = BestHTTP.SocketIO.Transports.TransportTypes.WebSocket;

			if (serverType==ServerType.AttnGan)
			{
				attnGanSocketManager = new SocketManager(new Uri(attnGanUrl), options);
                attnGanSocketManager.Encoder = new BestHTTP.SocketIO.JsonEncoders.LitJsonEncoder();
                //attnGanSocket = attnGanSocketManager.Socket;
                attnGanSocket = attnGanSocketManager["/query"];

                attnGanSocket.On("connecting", (socket, packet, args) => Debug.Log("Connecting AttnGan"));
                attnGanSocket.On(SocketIOEventTypes.Connect, OnAttnGanConnect);
                attnGanSocket.On("update_response", OnAttnGanUpdateResponse);
                attnGanSocket.On(SocketIOEventTypes.Error, OnAttnGanError);

                attnGanSocketManager.Open();

                // Image convert related
                attnGanTexture = new Texture2D(attnGanImageWidth, attnGanImageHeight);
                attnGanMaterial.mainTexture = attnGanTexture;
			}
			else
			{
				pix2pixSocketManager = new SocketManager(new Uri(pix2pixUrl), options);
				pix2pixSocketManager.Encoder = new BestHTTP.SocketIO.JsonEncoders.LitJsonEncoder();
				pix2pixSocket = pix2pixSocketManager["/generate"];

				pix2pixSocket.On("connecting", (socket, packet, args) => Debug.Log("Connecting Pix2Pix"));
				pix2pixSocket.On(SocketIOEventTypes.Connect, OnPix2PixConnect);
				pix2pixSocket.On("update_response", OnPix2PixUpdateResponse);
				pix2pixSocket.On(SocketIOEventTypes.Error, OnPix2PixError);

				pix2pixSocketManager.Open();
				pix2pixTexture = new Texture2D(pix2pixImageWidth, pix2pixImageHeight);
				pix2pixMaterial.SetTexture("_ShadowTex", pix2pixTexture);
			}
		}

		private void Update()
		{
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
				attnGanSocket.Emit("update_request", attnGanRequestData);
				Debug.Log("AttnGanSocket emit update_request");
			}
		}

		private void OnDestroy()
		{
			if (serverType == ServerType.AttnGan)
			{
				Debug.Log("Close AttnGAN socket");
                attnGanSocketManager.Close();
			}
			else
			{
				Debug.Log("Close pix2pix socket");
				pix2pixSocketManager.Close();
			}
		}

		/////////////////////////////////////////////////
        /////////////////////////////////////////////////
        /////////////////////////////////////////////////

		void OnAttnGanConnect(Socket socket, Packet packet, params object[] args)
		{
			Debug.Log("Connected to AttnGAN.");
			Debug.Log(socket.Id);
			pix2pixIsConnected = true;
                        
			//EmitPix2PixRequest();
		}

		void OnAttnGanUpdateResponse(Socket socket, Packet packet, params object[] args)
		{
			Dictionary<string, object> data = args[0] as Dictionary<string, object>;
			string base64Image = data["image"] as string;
			byte[] receivedBase64Img = Convert.FromBase64String(base64Image);
			attnGanTexture.LoadImage(receivedBase64Img);
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
			attnGanSocket.Emit("update_request", attnGanRequestData);

			lastTypingTimecode = Time.time;
			Debug.LogFormat("socket emit request: {0}", stringToSend);
		}

		public void EmitAttnGanRequest(string inputText)
		{
			AttnGanRequestData attnGanRequestData = new AttnGanRequestData();
            string stringToSend = inputText.ToLower();
            stringToSend += " ";
            attnGanRequestData.caption = inputText.ToLower();
            attnGanSocket.Emit("update_request", attnGanRequestData);
			Debug.LogFormat("socket emit request: {0}", stringToSend);
		}

        /////////////////////////////////////////////////
		/////////////////////////////////////////////////
		///////////////////////////////////////////////// 

		void OnPix2PixConnect(Socket socket, Packet packet, params object[] args)
        {
            Debug.Log("Connected to pix2pix.");
            Debug.Log(socket.Id);
        }

        void OnPix2PixUpdateResponse(Socket socket, Packet packet, params object[] args)
        {
            Dictionary<string, object> data = args[0] as Dictionary<string, object>;
            string base64Image = data["results"] as string;
            byte[] receivedBase64Img = Convert.FromBase64String(base64Image);
            pix2pixTexture.LoadImage(receivedBase64Img);

			//
			EmitPix2PixRequest();
        }

        void OnPix2PixError(Socket socket, Packet packet, params object[] args)
        {
			Debug.LogError(string.Format("--- pix2pix Error --- {0}", args[0].ToString()));
        }
        
		public void EmitPix2PixRequest()
        {
			Pix2PixRequestData pix2PixRequestData = new Pix2PixRequestData();
			Texture2D webTex = (Texture2D) webcamMaterial.mainTexture;
            
			byte[] imageData = webTex.EncodeToJPG();
			string base64Image = Convert.ToBase64String(imageData);
			pix2pixSocket.Emit("update_request", base64Image);
			Debug.LogFormat("socket emit pix2pix request");
        }
	}
}
