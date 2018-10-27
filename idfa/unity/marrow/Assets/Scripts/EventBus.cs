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
        public static StringEvent SceneSetupStarted = new StringEvent();
        public static StringEvent SceneSetupEnded = new StringEvent();
		public static UnityEvent SceneStarted = new UnityEvent();
        public static UnityEvent SceneEnded = new UnityEvent();
		public static UnityEvent SceneRestarted = new UnityEvent();
        
		// OSC related
		public static StringEvent SpeechToTextReceived = new StringEvent();
		public static StringEvent SpeechEmotionReceived = new StringEvent();
    }
}
