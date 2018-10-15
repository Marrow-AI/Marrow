using System;
using System.Collections.Generic;
using UnityEngine;
using BestHTTP;
using BestHTTP.SocketIO;
using Newtonsoft.Json;

[Serializable]
public class AttnGanRequestData
{
	public string caption;
}

public class SocketCommunication : MonoBehaviour {

	public string attnGanUrl; //http://api.marrow.raycaster.studio:3333/socket.io/
	public int attnGanImageWidth = 256;
	public int attnGanImageHeight = 256;
	public float typingTimerLength = 0.5f;
	public Material attnGanMaterial;
	public Texture2D testTexture;

	private SocketManager attnGanSocketManager;
	private Socket attnGanSocket;

	public Texture2D attnGanTexture;
	private float lastTypingTimecode;

	private void Start ()
	{
		SocketOptions options = new SocketOptions();
		options.AutoConnect = false;
		//options.ConnectWith = BestHTTP.SocketIO.Transports.TransportTypes.WebSocket;

		attnGanSocketManager = new SocketManager(new Uri(attnGanUrl), options);
		attnGanSocketManager.Encoder = new BestHTTP.SocketIO.JsonEncoders.LitJsonEncoder();
        
		//attnGanSocket = attnGanSocketManager.Socket;
		//attnGanSocket = attnGanSocketManager.GetSocket("/query");
		attnGanSocket = attnGanSocketManager["/query"];
        
		attnGanSocket.On("connecting", (socket, packet, args) => Debug.Log("Connecting AttnGan"));
		attnGanSocket.On(SocketIOEventTypes.Connect, OnAttnGanConnect);
		attnGanSocket.On("update_response", OnAttnGanUpdateResponse);
		attnGanSocket.On(SocketIOEventTypes.Error, OnAttnGanError);
        
		attnGanSocketManager.Open();

        // Image convert related
		//byte[] testBase64Img = testTexture.GetRawTextureData();
		//Debug.Log("test image format: " + testTexture.format);
		attnGanTexture = new Texture2D(attnGanImageWidth, attnGanImageHeight, TextureFormat.RGB24, false, false);
		//attnGanTexture.LoadRawTextureData(testBase64Img);
		//attnGanTexture.Apply();

		attnGanMaterial.mainTexture = attnGanTexture;
		//byte[] base64Img = attnGanTexture.GetRawTextureData();
	}

	private void Update()
	{
		if (Input.GetKeyDown("a"))
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
		Debug.Log("Close AttnGAN socket");
		attnGanSocketManager.Close();
	}

	void OnAttnGanConnect(Socket socket, Packet packet, params object[] args)
	{
		Debug.Log("Connected to AttnGAN.");
		Debug.Log(socket.Id);
	}

	void OnAttnGanUpdateResponse(Socket socket, Packet packet, params object[] args)
	{
		Dictionary<string, object> data = args[0] as Dictionary<string, object>;
		string base64Image = data["image"] as string;
		Debug.Log("Got AttnGAN update_response - base64Image: " + base64Image);

		byte[] receivedBase64Img = Convert.FromBase64String(base64Image);
		//Debug.Log("received base64Img length: " + receivedBase64Img.Length);
		attnGanTexture.LoadRawTextureData(receivedBase64Img);
		attnGanTexture.Apply();
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
		attnGanRequestData.caption = inputText;
        attnGanSocket.Emit("update_request", attnGanRequestData);

		lastTypingTimecode = Time.time;
	}
}
