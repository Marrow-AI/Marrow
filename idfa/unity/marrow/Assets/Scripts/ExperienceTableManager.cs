using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Marrow
{
	public class ExperienceTableManager : Manager<ExperienceTableManager>
    {
		public SocketCommunication socketCommunication;
		public OSCCommunication oSCCommunication;
		public float speechTimerLength = 0.5f;
		public bool startText2Image;
		private float lastSpeechTimecode;

		[Header("T2I related")]
		public Material plateMaterial;
		public int attnGanImageWidth = 256;
        public int attnGanImageHeight = 256;
		private Texture2D attnGanTextureA;
		private Texture2D attnGanTextureB;


        private void Start()
        {
			// Image convert related
			attnGanTextureA = new Texture2D(attnGanImageWidth, attnGanImageHeight);
			plateMaterial.SetTexture("_MainTex", attnGanTextureA);
			attnGanTextureB = new Texture2D(attnGanImageWidth, attnGanImageHeight);
			plateMaterial.SetTexture("_SecondTex", attnGanTextureB);
        }

		public void OnAttnGanInputUpdate(string inputText)
        {
			if ((Time.time - lastSpeechTimecode) <= speechTimerLength)
                return;

			socketCommunication.EmitAttnGanRequest(inputText);

			lastSpeechTimecode = Time.time;
        }

		public void OnAttnGanUpdateResponse(byte[] receivedBase64Img)
        {
			attnGanTextureA.LoadImage(receivedBase64Img);
        }
    }
}
