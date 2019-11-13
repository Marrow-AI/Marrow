using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GanShowFace : MonoBehaviour {

	public GameObject gFace;
	public GameObject dFace;

	public void ShowFace()
	{
		gFace.SetActive(true);
		dFace.SetActive(true);
	}
}
