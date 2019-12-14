using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using extOSC;

namespace Marrow
{
	public class ExperienceTableManager : Manager<ExperienceTableManager>
    {
		public SocketCommunication socketCommunication;
		public float speechTimerLength = 0.1f;
		private bool startText2Image;
		public TableOpenSequence tableOpenSequence;

		private bool socketIsConnected;
		private bool tableSceneStarted;
		private bool tableOpeningEnded;

		[Header("T2I related")]
		public Material plateMaterial;
		public Material plateTransparentMaterial;
		public int attnGanImageWidth = 256;
       public int attnGanImageHeight = 256;
		private float lastSpeechTimecode;
		private Texture2D attnGanTextureA;
		private Texture2D attnGanTextureB;
		private int textureSwapCount;
		private int plateTweenId;

		private int currentPlateMaterialTargetBlend = 0;

		private bool m_reactToGanSpeak;
		public bool ReactToGanSpeak
		{
			get { return m_reactToGanSpeak; }
			set { m_reactToGanSpeak = value; }
		}

		private void OnEnable()
		{
			EventBus.T2IEnable.AddListener(EnableText2Image);
			EventBus.T2IDisable.AddListener(DisableText2Image);
			EventBus.TableOpeningEnded.AddListener(OnTableOpeningEnded);
                        
			EventBus.WebsocketConnected.AddListener(OnWebsocketConnected);
			EventBus.WebsocketDisconnected.AddListener(OnWebsocketDisconnected);

			socketCommunication.AttnGanUpdateResponded.AddListener(OnAttnGanUpdateResponse);
		}

		private void OnDisable()
		{
			EventBus.T2IEnable.RemoveListener(EnableText2Image);
			EventBus.T2IDisable.RemoveListener(DisableText2Image);
			EventBus.TableOpeningEnded.RemoveListener(OnTableOpeningEnded);
            
			EventBus.WebsocketConnected.RemoveListener(OnWebsocketConnected);
			EventBus.WebsocketDisconnected.RemoveListener(OnWebsocketDisconnected);

			socketCommunication.AttnGanUpdateResponded.RemoveListener(OnAttnGanUpdateResponse);
		}

		private void Start()
        {
			// Image convert related
			attnGanTextureA = new Texture2D(attnGanImageWidth, attnGanImageHeight);
			plateMaterial.SetTexture("_MainTex", attnGanTextureA);
			plateTransparentMaterial.SetTexture("_MainTex", attnGanTextureA);
			attnGanTextureB = new Texture2D(attnGanImageWidth, attnGanImageHeight);
			plateMaterial.SetTexture("_SecondTex", attnGanTextureB);
			plateTransparentMaterial.SetTexture("_SecondTex", attnGanTextureB);
        }

		/////////////////////////
		///  Events callback  ///
		/////////////////////////

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

		void OnTableOpeningEnded()
		{
			tableOpeningEnded = true;
		}

		public void OnAttnGanInputUpdate(string inputText)
        {
			Debug.Log("OnAttnGanInputUpdate, startText2Image: " + startText2Image 
			          + ", socketIsConnected: " + socketIsConnected
			          + ", tableOpeningEnded: " + tableOpeningEnded);
			if (!startText2Image || !socketIsConnected || !tableOpeningEnded)
				return;
			if ((Time.time - lastSpeechTimecode) <= speechTimerLength)
                return;

            Debug.Log("Requesting");
			socketCommunication.EmitAttnGanRequest(inputText);
            
			lastSpeechTimecode = Time.time;
        }

		public void OnAttnGanUpdateResponse(byte[] receivedBase64Img)
        {
			if (!startText2Image)
                return;            
			if (LeanTween.isTweening(plateTweenId))
				return;
			
            textureSwapCount++;
            if (textureSwapCount % 2 == 1)
            {
				attnGanTextureB.LoadImage(receivedBase64Img);
				currentPlateMaterialTargetBlend = 1;
            }
            else
            {
				attnGanTextureA.LoadImage(receivedBase64Img);
				currentPlateMaterialTargetBlend = 0;
            }

			plateTweenId = LeanTween.value(gameObject, plateMaterial.GetFloat("_Blend"), currentPlateMaterialTargetBlend, .5f)
			                        .setOnUpdate((float val) =>
                                     {
                                         plateMaterial.SetFloat("_Blend", val);
										 plateTransparentMaterial.SetFloat("_Blend", val);
                                     })
			                        .id;
            
			if(!tableOpeningEnded)
			{
				LTDescr descr = LeanTween.description(plateTweenId);
				descr.setOnComplete(()=>{
					//FadeColorToTexture();
					tableOpenSequence.StartTableDinner();
                    DisableText2Image();

                });
			}
        }

		/////////////////////////
        ///  ---------------  ///
        /////////////////////////
        
		public void FadeTextureToColor()
		{         
            // V2 - fade in shader
			LeanTween.value(gameObject, plateMaterial.GetFloat("_Fade"), 1f, 1f)
                                    .setOnUpdate((float val) =>
                                    {
                                		plateMaterial.SetFloat("_Fade", val);
                                		plateTransparentMaterial.SetFloat("_Fade", val);
                                    });
		}

		void FadeColorToTexture()
		{
			LeanTween.value(gameObject, plateMaterial.GetFloat("_Fade"), 0f, 1f)
                                    .setOnUpdate((float val) =>
                                    {
                                        plateMaterial.SetFloat("_Fade", val);
                                        plateTransparentMaterial.SetFloat("_Fade", val);
                                    });
		}

        ///////////////////////////
        ///     OSC related     ///
		///////////////////////////

		public void ReceivedOscControlStart(OSCMessage message)
		{
			Debug.Log("osc:control/start - 4 people triggered, starting the expereince");
			EventBus.ExperienceStarted.Invoke();
			tableOpeningEnded = false;
		}

		public void ReceivedOscTableFadeIn(OSCMessage message)
        {
			Debug.Log("osc:table/fadein");
            tableSceneStarted = true;
            EventBus.TableStarted.Invoke();
        }

		public void ReceivedOscShowChosenDinner(string text)
        {
			Debug.Log("Received Osc - Show the chosen dinner on the table");
			//Debug.Log(text);
			EnableText2Image();
			EventBus.T2IEnable.Invoke();
		//	EventBus.DinnerQuestionStart.Invoke();

			//tableOpenSequence.UpdateSpeechDetectionText(text);
			//socketCommunication.EmitAttnGanRequest(text);
        }

		public void ReceivedOscControlStop(OSCMessage message)
        {
			Debug.Log("Received Osc - hard stop!");
			EventBus.ExperienceEnded.Invoke();
        }

		public void ReceivedOscGanSpeak(int doSpeak)
		{
			if (!ReactToGanSpeak)
				return;
			
			if (doSpeak==1)
			{
				// hide plates, disable name tag
				tableOpenSequence.HideSpotLightAndTexts(true);
				// dim main light
				tableOpenSequence.mainLight.ToggleOn(true, 1f, 1f, 0);
				tableOpenSequence.platesOnlySpotlight.ToggleOn(false, 0f, 1f, 0);

				FadeTextureToColor();
			}
			else
			{
				// show plates, enable name tag
				tableOpenSequence.HideSpotLightAndTexts(false);
				// reset main light
				tableOpenSequence.mainLight.ToggleOn(true, 1.7f, 1f, 0);
				tableOpenSequence.platesOnlySpotlight.ToggleOn(true, 2f, 1f, 0);

				FadeColorToTexture();
			}
		}

		public void ReceivedOscTableFadeOut(OSCMessage message)
        {
            Debug.Log(message);
            tableSceneStarted = false;
			EventBus.TableEnded.Invoke();			        
        }
    }
}
