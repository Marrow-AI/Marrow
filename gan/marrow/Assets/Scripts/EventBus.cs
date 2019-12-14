using System;
using UnityEngine;
using UnityEngine.Events;

namespace Marrow
{
    [Serializable]
    public class StringEvent : UnityEvent<string> { }
	[Serializable]
	public class FloatEvent : UnityEvent<float> { }
	[Serializable]
	public class IntergerEvent : UnityEvent<int> { }
    [Serializable]
    public class Vector3Event : UnityEvent<Vector3> { }
    
    public class EventBus
    {
        // Scene related
		public static UnityEvent TableStarted = new UnityEvent();
		public static UnityEvent TableOpeningEnded = new UnityEvent();
		public static UnityEvent TableEnded = new UnityEvent();
        
		public static UnityEvent ExperienceStarted = new UnityEvent();
		public static UnityEvent ExperienceEnded = new UnityEvent();
        
		// Websocket related
		public static UnityEvent WebsocketConnected = new UnityEvent();
		public static UnityEvent WebsocketDisconnected = new UnityEvent();

		// OSC related
		public static StringEvent SpeechToTextReceived = new StringEvent();
		public static StringEvent SpeechToTextRoleReceived = new StringEvent();
		public static StringEvent SpeechEmotionReceived = new StringEvent();

        // T2I
		public static UnityEvent T2IEnable = new UnityEvent();
		public static UnityEvent T2IDisable = new UnityEvent();
		public static UnityEvent DinnerQuestionStart = new UnityEvent();
    }
}
