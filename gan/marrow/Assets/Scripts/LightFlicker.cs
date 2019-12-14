using UnityEngine;
using System.Collections;

namespace Marrow
{
	[RequireComponent(typeof(Light))]
	public class LightFlicker : MonoBehaviour
	{
		public float range = 0.1f;
		public float maxIntensity = 1.5f;
		public float minIntensity = 0.5f;
		public float frequency = 20f;
		public bool randomColor;

		private Light targetLight;
		private float intensity;
		private float lastChangedColorTime;
		private float colorDuration = 0.5f;
		private float timeRandom;
        
		void Start()
		{
			targetLight = GetComponent<Light>();
			intensity = targetLight.intensity;
			lastChangedColorTime = Time.time;
			timeRandom = Random.value*2f;
		}

		void Update()
		{
			intensity = Mathf.Clamp(targetLight.intensity, minIntensity, maxIntensity);
			targetLight.intensity = intensity / 2f + Mathf.Lerp(intensity - range, intensity + range * (Mathf.Sin(Time.time+timeRandom)+1f)/2f, Mathf.Cos((Time.time+timeRandom) * frequency));

			if (randomColor)
			{
				if (Time.time - lastChangedColorTime>colorDuration)
				{
					targetLight.color = Random.ColorHSV();
					lastChangedColorTime = Time.time;
					colorDuration = Random.Range(0.2f, 0.6f);
				}
			}
		}
	}
}
