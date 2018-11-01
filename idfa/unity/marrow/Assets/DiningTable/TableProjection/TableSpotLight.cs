using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Marrow
{
	[RequireComponent(typeof(Light))]
    public class TableSpotLight : MonoBehaviour
    {
		public Transform targetPlateLocation;

		private Light m_light;
		private Vector3 initialPosition;
		private float initialIntensity;
		private float initialSpotAngle;
		private Color initialColor;

    	void Awake()
    	{
			m_light = GetComponent<Light>();
			initialPosition = transform.position;
			initialIntensity = m_light.intensity;
			initialSpotAngle = m_light.spotAngle;
			initialColor = m_light.color;

            // off at start
			m_light.intensity = 0;
    	}

		public void Restart()
		{
			transform.position = initialPosition;
			m_light.intensity = initialIntensity;
			m_light.spotAngle = initialSpotAngle;
			m_light.color = initialColor;
			m_light.cookie = null;

			// off at start
            m_light.intensity = 0;
		}

    	public void ToggleOn(bool turnOn, float intensity, float time, float delay)
    	{
			if (turnOn)
			{
				LeanTween.value(gameObject, m_light.intensity, 1f * intensity, time)
				         .setDelay(delay)
				         .setOnUpdate(CallOnIntensityUpdate);
			}
			else
			{
				LeanTween.value(gameObject, m_light.intensity, 0, time)
				         .setDelay(delay)
				         .setOnUpdate(CallOnIntensityUpdate);
			}
    	}

		public void BlinkOnce()
		{
			LeanTween.value(gameObject, m_light.intensity, m_light.intensity*0.8f, 0.25f)
					 .setLoopPingPong(1)
					 .setOnUpdate(CallOnIntensityUpdate);
		}

		void CallOnIntensityUpdate(float val)
		{
			m_light.intensity = val;
		}

		public void SetSpotAngle(float angle)
		{
			SetSpotAngle(angle, 1f);
		}

		public void SetSpotAngle(float angle, float time)
        {
			LeanTween.value(gameObject, m_light.spotAngle, angle, time)
                     .setOnUpdate((float val) => {
                         m_light.spotAngle = val;
                     });
        }

		public void TargetOnPlate(float delay)
		{
			TargetOnPlate(delay, 1.5f);
		}

		public void TargetOnPlate(float delay, float intensity)
        {
			TargetOnPlate(delay, intensity, 0.5f);
        }

		public void TargetOnPlate(float delay, float intensity, float time)
        {
			transform.position = targetPlateLocation.position;
            m_light.spotAngle = 30f;
            ToggleOn(true, intensity, time, delay);
        }

		public void TargetOnPlateBlink(float intensity, float time)
        {
            transform.position = targetPlateLocation.position;
            m_light.spotAngle = 30f;
			LeanTween.value(gameObject, CallOnIntensityUpdate, 0, intensity, time);
			LeanTween.value(gameObject, CallOnIntensityUpdate, intensity, 0, time).setDelay(time+2f);
        }

		public void SetLightColor(string hex)
        {
            Color newLightColor;
            if (ColorUtility.TryParseHtmlString(hex, out newLightColor))
            {
				m_light.color = newLightColor;
            }
        }

		public void ChangeLightColor(string hex)
		{
			Color newLightColor;
			if (ColorUtility.TryParseHtmlString(hex, out newLightColor))
            {
				LeanTween.value(gameObject, m_light.color, newLightColor, 1f)
                         .setOnUpdate((Color col) =>
                         {
                             m_light.color = col;
                         });
            }
		}

		public void BecomeGeneralMainLight(Texture cookie)
		{
			// position
			LeanTween.moveLocal(gameObject, new Vector3(0, 5.5f, 0), 1f);
			//transform.localPosition = new Vector3(0, 5.5f, 0);
			//m_light.spotAngle = 10f;
			//m_light.cookie = cookie;

			// spot angle
			SetSpotAngle(130f);

			// intensity
			ToggleOn(true, 1.5f, 1f, 0);
            
			// color
			Color generalLightColor;
			if(ColorUtility.TryParseHtmlString("#AC94BE", out generalLightColor))
			{
				LeanTween.value(gameObject, m_light.color, generalLightColor, 1f)
				         .setOnUpdate((Color col) =>
                         {
                             m_light.color = col;
                         });

			}
		}
    }
}
