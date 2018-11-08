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
		public static UnityEvent TableSequenceStarted = new UnityEvent();
		public static UnityEvent TableSequenceEnded = new UnityEvent();

		public static UnityEvent DiningRoomStarted = new UnityEvent();
		public static UnityEvent DiningRoomEnded = new UnityEvent();

		public static UnityEvent ExperienceEnded = new UnityEvent();
		public static UnityEvent ExperienceRestarted = new UnityEvent();    // ???
        
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
		public static UnityEvent DinnerQuestionEnd = new UnityEvent();
    }
}
