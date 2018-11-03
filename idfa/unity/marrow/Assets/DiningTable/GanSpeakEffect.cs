using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Marrow
{
	public class GanSpeakEffect : MonoBehaviour
	{
		public float speed = 1f;
		private PostProcessVolume postProcessVolume;
		private Grain grainLayer;
		private bool ganIsSpeaking;

		void Start()
		{
			postProcessVolume = GetComponent<PostProcessVolume>();
			postProcessVolume.profile.TryGetSettings(out grainLayer);
		}
        
		void Update()
		{
			//if (Input.GetKeyDown("g"))
			//{
			//	GanSpeaks(1);
			//}
			//else if (Input.GetKeyDown("h"))
			//{
            //    GanSpeaks(0);
            //}

			//if (ganIsSpeaking)
			//{
			//	grainLayer.intensity.value = 0.7f + (Mathf.Sin(Time.time) + 1f) / 2f * 0.3f;
			//}
		}

		public void GanSpeaks(int status)
		{
			if (status==1)
			{
				ganIsSpeaking = true;
				postProcessVolume.weight = 1;
			}
			else
			{
				ganIsSpeaking = false;
				postProcessVolume.weight = 0;
			}
		}
	}
}
