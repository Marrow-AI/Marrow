using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using extOSC;

namespace Marrow
{
	public class OSCCommunication : MonoBehaviour
	{
		public string address = "/speech";
		[Header("OSC Settings")]
		public OSCReceiver oscReceiver;

		void Start()
		{
			oscReceiver.Bind(address, ReceivedMessage);
		}

		void ReceivedMessage (OSCMessage message)
		{
			Debug.LogFormat("Received: {0}", message);
		}
	}
}
