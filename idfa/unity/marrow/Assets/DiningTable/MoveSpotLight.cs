using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveSpotLight : MonoBehaviour {

	public Transform[] lightPositions;
	private Light m_light;

	private void Start()
	{
		m_light = GetComponent<Light>();
	}

	public void UpdateSpotlightPosition(string role)
	{
		LeanTween.value(gameObject, m_light.intensity, 2.4f, 1f)
                     .setLoopPingPong(1)
                     .setOnUpdate(CallOnIntensityUpdate);
		
		switch(role)
		{
			case "dad":
				transform.position = lightPositions[0].position;
				break;
			case "sister":
				transform.position = lightPositions[1].position;
                break;
			case "brother":
				transform.position = lightPositions[2].position;
                break;
			case "mom":
				transform.position = lightPositions[3].position;
                break;
		}
	}

	private void CallOnIntensityUpdate(float val)
    {
        m_light.intensity = val;
    }
}
