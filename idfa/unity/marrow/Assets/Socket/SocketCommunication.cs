using System;
using System.Collections.Generic;
using UnityEngine;
using BestHTTP;
using BestHTTP.SocketIO;
using Newtonsoft.Json;

[Serializable]
public class Text2ImageRequestInfo
{
	public string caption;
}

public class SocketCommunication : MonoBehaviour {

	public string t2iURL;
	private SocketManager t2iSocketManager;
	private Socket t2iSocket;

	private void Start ()
	{

		SocketOptions options = new SocketOptions();
		options.AutoConnect = false;
		//options.ConnectWith = BestHTTP.SocketIO.Transports.TransportTypes.WebSocket;

		t2iSocketManager = new SocketManager(new Uri(t2iURL), options);
		//t2iSocket = t2iSocketManager.Socket;
		t2iSocket = t2iSocketManager["/query"];

		t2iSocket.On("connecting",
		                 (socket, packet, args) => Debug.Log("connecting")
		                );
		t2iSocket.On("event",
                         (socket, packet, args) => Debug.Log("event")
                        );
		t2iSocket.On("connect", OnT2IConnect);
		t2iSocket.On("update_response", OnT2IUpdateResponse);
		t2iSocket.On(SocketIOEventTypes.Error, OnError);
        
		t2iSocketManager.Open();
	}

	private void Update()
	{
		if (Input.GetKeyDown("a"))
		{
			Text2ImageRequestInfo text2ImageRequestInfo = new Text2ImageRequestInfo();
			text2ImageRequestInfo.caption = "apple test";
			//string json = JsonConvert.SerializeObject(text2ImageRequestInfo);
			string json = JsonUtility.ToJson(text2ImageRequestInfo);
			t2iSocket.Emit("update_request", json);
			//t2iSocket.Emit("update_request", "caption", "apple");
			Debug.Log("emit update_request");
		}
	}

	private void OnDestroy()
	{
		Debug.Log("Close pix2pix socket");
		t2iSocketManager.Close();
	}

	void OnT2IConnect(Socket socket, Packet packet, params object[] args)
	{
		Debug.Log("Connect to t2i socket server.");
	}

	void OnT2IUpdateResponse(Socket socket, Packet packet, params object[] args)
	{
		//Dictionary<string, object> data = args[0] as Dictionary<string, object>;
		//string base64Image = data["image"] as string;
		Debug.Log("Get t2i: update_response");
	}

	void OnError(Socket socket, Packet packet, params object[] args)
	{
		Debug.LogError(string.Format("--- Error --- {0}", args[0].ToString()));
	}
}
