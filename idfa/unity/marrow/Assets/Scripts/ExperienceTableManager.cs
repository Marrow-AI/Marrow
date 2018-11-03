using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using extOSC;

namespace Marrow
{
	public class ExperienceTableManager : Manager<ExperienceTableManager>
    {
		public SocketCommunication socketCommunication;
		public OSCCommunication oSCCommunication;
		public float speechTimerLength = 0.1f;
		public bool startText2Image;

		private bool socketIsConnected;

		[Header("T2I related")]
		public Material plateMaterial;
		public int attnGanImageWidth = 256;
        public int attnGanImageHeight = 256;
		private float lastSpeechTimecode;
		private Texture2D attnGanTextureA;
		private Texture2D attnGanTextureB;
		private int textureSwapCount;
		private int plateTweenId;

		private void OnEnable()
		{
			EventBus.TableSequenceEnded.AddListener(EnableText2Image);

			EventBus.DiningRoomEnded.AddListener(OnDiningRoomEnded);
			EventBus.ExperienceEnded.AddListener(DisableText2Image);
            
			EventBus.WebsocketConnected.AddListener(OnWebsocketConnected);
			EventBus.WebsocketDisconnected.AddListener(OnWebsocketDisconnected);

			socketCommunication.AttnGanUpdateResponded.AddListener(OnAttnGanUpdateResponse);
		}

		private void OnDisable()
		{
			EventBus.TableSequenceEnded.RemoveListener(EnableText2Image);

			EventBus.DiningRoomEnded.RemoveListener(OnDiningRoomEnded);
			EventBus.ExperienceEnded.RemoveListener(DisableText2Image);

			EventBus.WebsocketConnected.RemoveListener(OnWebsocketConnected);
			EventBus.WebsocketDisconnected.RemoveListener(OnWebsocketDisconnected);

			socketCommunication.AttnGanUpdateResponded.RemoveListener(OnAttnGanUpdateResponse);
		}

		private void Start()
        {
			// Image convert related
			attnGanTextureA = new Texture2D(attnGanImageWidth, attnGanImageHeight);
			plateMaterial.SetTexture("_MainTex", attnGanTextureA);
			attnGanTextureB = new Texture2D(attnGanImageWidth, attnGanImageHeight);
			plateMaterial.SetTexture("_SecondTex", attnGanTextureB);
        }

		void EnableText2Image()
		{
			startText2Image = true;
		}

		void DisableText2Image()
        {
			startText2Image = false;
        }

		void OnWebsocketConnected()
		{
			socketIsConnected = true;
		}

		void OnWebsocketDisconnected()
		{
			socketIsConnected = false;
		}

		public void OnAttnGanInputUpdate(string inputText)
        {
			if (!startText2Image || !socketIsConnected)
				return;
			if ((Time.time - lastSpeechTimecode) <= speechTimerLength)
                return;

			socketCommunication.EmitAttnGanRequest(inputText);

			lastSpeechTimecode = Time.time;
        }

		public void OnAttnGanUpdateResponse(byte[] receivedBase64Img)
        {
			if (!startText2Image)
                return;
			if (LeanTween.isTweening(plateTweenId))
				return;
			
			int targetBlend;
            textureSwapCount++;
            if (textureSwapCount % 2 == 1)
            {
				attnGanTextureB.LoadImage(receivedBase64Img);
                targetBlend = 1;
            }
            else
            {
				attnGanTextureA.LoadImage(receivedBase64Img);
                targetBlend = 0;
            }

			plateTweenId = LeanTween.value(gameObject, plateMaterial.GetFloat("_Blend"), targetBlend, .5f)
			                        .setOnUpdate((float val) =>
                                     {
                                         plateMaterial.SetFloat("_Blend", val);
                                     })
			                        .id;
        }

        void OnDiningRoomEnded()
		{
			DisableText2Image();
            // Roll credits happen in TableOpenSequence script
		}

        ///////////////////////////
        ///     OSC related     ///
		///////////////////////////

		public void ReceivedOscControlStart(OSCMessage message)
		{
			Debug.Log(message);
			EventBus.TableSequenceStarted.Invoke();
		}

		public void ReceivedOscControlStop(OSCMessage message)
        {
			Debug.Log(message);
			EventBus.ExperienceEnded.Invoke();
        }

		public void ReceivedOscGanEnd(OSCMessage message)
        {
            Debug.Log(message);
			EventBus.DiningRoomEnded.Invoke();
        }
    }
}
