using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveSpotLight : MonoBehaviour {

	public Transform[] lightPositions;

	public void UpdateSpotlightPosition(string role)
	{
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
}
