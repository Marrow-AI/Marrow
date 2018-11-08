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
		public TableOpenSequence tableOpenSequence;

		private bool socketIsConnected;
		private bool tableSceneStarted;

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
			//EventBus.TableSequenceEnded.AddListener(EnableText2Image);
			EventBus.T2IEnable.AddListener(EnableText2Image);
			EventBus.T2IDisable.AddListener(DisableText2Image);

			EventBus.DiningRoomEnded.AddListener(OnDiningRoomEnded);
            
			EventBus.WebsocketConnected.AddListener(OnWebsocketConnected);
			EventBus.WebsocketDisconnected.AddListener(OnWebsocketDisconnected);

			socketCommunication.AttnGanUpdateResponded.AddListener(OnAttnGanUpdateResponse);
		}

		private void OnDisable()
		{
			//EventBus.TableSequenceEnded.RemoveListener(EnableText2Image);
			EventBus.T2IEnable.RemoveListener(EnableText2Image);
			EventBus.T2IDisable.RemoveListener(DisableText2Image);
            
			EventBus.DiningRoomEnded.RemoveListener(OnDiningRoomEnded);

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
			Debug.Log("OnAttnGanInputUpdate, startText2Image: " + startText2Image + ", socketIsConnected: " + socketIsConnected);
			if (!startText2Image || !socketIsConnected)
				return;
			if ((Time.time - lastSpeechTimecode) <= speechTimerLength)
                return;

			socketCommunication.EmitAttnGanRequest(inputText);
			Debug.Log("EmitAttnGanRequest");
            
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
			tableSceneStarted = true;
			EventBus.TableSequenceStarted.Invoke();
		}

		public void ReceivedOscTableDinnerQuestionStart(string text)
        {
			Debug.Log(text);
			EnableText2Image();
			EventBus.T2IEnable.Invoke();
			EventBus.DinnerQuestionStart.Invoke();

			tableOpenSequence.UpdateSpeechDetectionText(text);
			OnAttnGanInputUpdate(text);
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

		public void ReceivedOscGanSpeak(int doSpeak)
		{
			if (doSpeak==1)
			{
				// hide plates, disable name tag
				tableOpenSequence.HidePlatesAndNameTags(true);
				// dim main light
				tableOpenSequence.mainLight.ToggleOn(true, 0.5f, 1f, 0);
			}
			else
			{
				// show plates, enable name tag
				tableOpenSequence.HidePlatesAndNameTags(false);
				// reset main light
				tableOpenSequence.mainLight.ToggleOn(true, 1.7f, 1f, 0);
			}
		}

		public void ReceivedOscTableTitle(OSCMessage message)
		{
			Debug.Log(message);
			tableSceneStarted = false;
		}
    }
}
