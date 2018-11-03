using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Marrow
{
	// doc: https://docs.unity3d.com/ScriptReference/WebCamTexture.html
	public class WebcamAccess : MonoBehaviour
    {
		public Material webCamMaterial;
		public int requestWidth = 1280; // 1280, 640
		public int requestHeight = 720; // 720, 360
		public int requestFPS = 30;
        
		public WebCamTexture webcamTexture;
		private Color32[] pix2pixWebcamData;
        
        private void Start()
		{
			// Gets the list of devices
			WebCamDevice[] devices = WebCamTexture.devices;
            for (int i = 0; i < devices.Length; i++)
                Debug.Log(devices[i].name);

			// (string deviceName, int requestedWidth, int requestedHeight)
            if(devices.Length==1)
    			webcamTexture = new WebCamTexture(requestWidth, requestHeight);
            else
                webcamTexture = new WebCamTexture(devices[1].name, requestWidth, requestHeight);

            webcamTexture.requestedWidth = requestWidth;
            webcamTexture.requestedHeight = requestHeight;
			//webCamMaterial.mainTexture = webcamTexture;
			webcamTexture.Play();
		}

		private void Update()
		{
			if (webcamTexture.didUpdateThisFrame && pix2pixWebcamData==null)
			{
                webCamMaterial.mainTexture = webcamTexture;
                pix2pixWebcamData = new Color32[webcamTexture.width * webcamTexture.height];
				Debug.Log(webcamTexture.width + ", " + webcamTexture.height);
			}
		}

		private void OnDestroy()
		{
			webcamTexture.Stop();
		}

		public void StartWebCam()
		{
			webcamTexture.Play();
		}

		public void StopWebCam()
        {
			webcamTexture.Stop();
        }
	}
}