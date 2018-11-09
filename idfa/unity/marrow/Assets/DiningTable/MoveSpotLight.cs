using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Marrow
{
	public class MoveSpotLight : MonoBehaviour
	{

		public Transform[] lightPositions;
		public NameTag[] nameTags;
		private Light m_light;

		private void Start()
		{
			m_light = GetComponent<Light>();
		}

		public void UpdateSpotlightPosition(string role)
		{
			float delay=0f;
			if (m_light.intensity > 0)
			{
				delay = 0.5f;
				LeanTween.value(gameObject, m_light.intensity, 0f, 0.5f)
				         .setOnUpdate(CallOnIntensityUpdate);
			}
			LeanTween.value(gameObject, m_light.intensity, 2.4f, 1f)
			         .setDelay(delay)
                     .setOnUpdate(CallOnIntensityUpdate);

			//LeanTween.value(gameObject, m_light.intensity, 2.4f, 1f)
						 //.setLoopPingPong(1)
						 //.setOnUpdate(CallOnIntensityUpdate);

			int roleIndex = 0;
			switch (role)
			{
				case "dad":
					roleIndex = 0;
					break;

				case "sister":
					roleIndex = 1;
					break;

				case "brother":
					roleIndex = 2;
					break;

				case "mom":
					roleIndex = 3;
					break;
			}

			transform.position = lightPositions[roleIndex].position;

			for (int i = 0; i < nameTags.Length; i++)
			{
				if (i==roleIndex)
				{
					if(!nameTags[i].IsOn)
					{
						nameTags[i].Show();
                        //Debug.Log("show name: " + role);
					}
				}
				else
				{
					if(nameTags[i].IsOn)
					{
						nameTags[i].Hide();
                        //Debug.Log("hide name: " + role);
					}
				}
			}

		}

		private void CallOnIntensityUpdate(float val)
		{
			m_light.intensity = val;
		}
	}
}
