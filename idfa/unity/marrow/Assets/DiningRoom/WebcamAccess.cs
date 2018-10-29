using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Marrow
{
	public class WebcamAccess : MonoBehaviour
    {
		public Material webCamMaterial;
		private WebCamTexture webcamTexture;

        private void Start()
		{
			webcamTexture = new WebCamTexture();
			webCamMaterial.mainTexture = webcamTexture;
			webcamTexture.Play();
		}
	}
}