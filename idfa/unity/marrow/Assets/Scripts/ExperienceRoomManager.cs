using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using extOSC;

namespace Marrow
{
	public class ExperienceRoomManager : Manager<ExperienceTableManager>
    {
		public SocketCommunication socketCommunication;
		public bool startPix2Pix;

		private bool socketIsConnected;

		[Header("Pix2Pix related")]
		public Material projectorMaterial;
		public int pix2pixImageWidth = 1280;   // HD
		public int pix2pixImageHeight = 720;
		public WebcamAccess webcamAccess;
		private float gotPix2PixTimecode;
		private Texture2D pix2pixTextureA;
		private Texture2D pix2pixTextureB;
		private int textureSwapCount;
		private int blobTweenId;
		private bool emitFirstPix2PixRequest;

		[Header("Blob")]
		public Animator blobAnimator;
		private float pix2pixStartTimecode;
		private bool[] passBlobStages = { false, false, false };    // b, c, move slow
		private float[] blobStageTimecode = { 90f, 180f, 300f };
		private bool blobFullyGrow;

		[Header("Env")]
		public Light spotLight;
		private float originalSpotLightIntensity;

		private void OnEnable()
		{
			EventBus.TableSequenceEnded.AddListener(OnTableSequenceEnded);
			EventBus.DiningRoomEnded.AddListener(OnDiningRoomEnded);
			EventBus.ExperienceRestarted.AddListener(DisablePix2Pix);
            
			EventBus.WebsocketConnected.AddListener(OnWebsocketConnected);
			EventBus.WebsocketDisconnected.AddListener(OnWebsocketDisconnected);

			socketCommunication.Pix2PixUpdateResponded.AddListener(OnPix2PixUpdateResponse);
		}

		private void OnDisable()
		{
			EventBus.TableSequenceEnded.RemoveListener(OnTableSequenceEnded);
			EventBus.DiningRoomEnded.RemoveListener(OnDiningRoomEnded);
			EventBus.ExperienceRestarted.RemoveListener(DisablePix2Pix);

			EventBus.WebsocketConnected.RemoveListener(OnWebsocketConnected);
			EventBus.WebsocketDisconnected.RemoveListener(OnWebsocketDisconnected);

			socketCommunication.Pix2PixUpdateResponded.RemoveListener(OnPix2PixUpdateResponse);
		}

		private void Start()
        {
			// Image convert related
			pix2pixTextureA = new Texture2D(pix2pixImageWidth, pix2pixImageHeight);
			projectorMaterial.SetTexture("_ShadowTex", pix2pixTextureA);
			pix2pixTextureB = new Texture2D(pix2pixImageWidth, pix2pixImageHeight);
			projectorMaterial.SetTexture("_SecondShadowTex", pix2pixTextureB);

			originalSpotLightIntensity = spotLight.intensity;
        }

        void Setup()
		{
			spotLight.intensity = 0;
			projectorMaterial.color = Color.black;
		}

		private void Update()
		{
			if (socketIsConnected && startPix2Pix && !emitFirstPix2PixRequest && webcamAccess.webcamTexture.didUpdateThisFrame)
            {
				socketCommunication.EmitPix2PixRequest();
                emitFirstPix2PixRequest = true;
            }

			if (startPix2Pix && !blobFullyGrow)
			{
				if (Time.time - pix2pixStartTimecode >= blobStageTimecode[0] && !passBlobStages[0])
				{
					passBlobStages[0] = true;
					blobAnimator.SetTrigger("GrowB");
				}
				else if (Time.time - pix2pixStartTimecode >= blobStageTimecode[1] && !passBlobStages[1])
				{
					passBlobStages[1] = true;
					blobAnimator.SetTrigger("GrowC");
				}
				else if (Time.time - pix2pixStartTimecode >= blobStageTimecode[2] && !passBlobStages[2])
                {
                    passBlobStages[2] = true;
                    blobAnimator.SetTrigger("SlowMove");
					blobFullyGrow = true;
                }
			}
		}

		void OnTableSequenceEnded()
		{
			// prepare to start pix2pix, e.g. room spot light on
			LeanTween.value(0f, 1f, 3f)
			         .setOnUpdate((float val) => {
                         spotLight.intensity = val;
                     });
			LeanTween.value(gameObject, Color.black, Color.white, 3f)
					 .setOnUpdate((Color col) =>
					 {
						 projectorMaterial.SetColor("_Color", col);
					 });

			// webcam on
			webcamAccess.StartWebCam();

			// start pix2pix
			EnablePix2Pix();
			pix2pixStartTimecode = Time.time;
		}

		void EnablePix2Pix()
		{
			startPix2Pix = true;
		}

        void OnDiningRoomEnded()
		{
			DisablePix2Pix();
			webcamAccess.StopWebCam();
		}

		void DisablePix2Pix()
        {
			startPix2Pix = false;
        }

		void OnWebsocketConnected()
		{
			socketIsConnected = true;
		}

		void OnWebsocketDisconnected()
		{
			socketIsConnected = false;
		}

		public void OnPix2PixInputUpdate()
        {
			if (!startPix2Pix || !socketIsConnected)
				return;

			socketCommunication.EmitPix2PixRequest();
        }

		public void OnPix2PixUpdateResponse(byte[] receivedBase64Img)
        {
			if (!startPix2Pix)
                return;
			if (LeanTween.isTweening(blobTweenId))
				return;
			
			int targetBlend;
            textureSwapCount++;
            if (textureSwapCount % 2 == 1)
            {
				pix2pixTextureB.LoadImage(receivedBase64Img);
                targetBlend = 1;
				//projectorMaterial.SetTexture("_SecondShadowTex", pix2pixTextureB);    // ???
            }
            else
            {
				pix2pixTextureA.LoadImage(receivedBase64Img);
                targetBlend = 0;
				//projectorMaterial.SetTexture("_ShadowTex", pix2pixTextureA);    // ???
            }


			blobTweenId = LeanTween.value(gameObject, projectorMaterial.GetFloat("_Blend"), targetBlend, .5f)
			                        .setOnUpdate((float val) =>
                                     {
				                        projectorMaterial.SetFloat("_Blend", val);
                                     })
			                        .id;

			gotPix2PixTimecode = Time.time;

			socketCommunication.EmitPix2PixRequest();
        }

		///////////////////////////
        ///     OSC related     ///
        ///////////////////////////
        
		public void ReceivedOscGanSpeaks(int status)
        {
			Debug.Log("Gan speaks status: " + status);
			if (status==1)
			{
				// Gan speaks
				blobAnimator.enabled = false;
			}
			else
			{
				// Gan stops
				blobAnimator.enabled = true;
			}
        }

		public void ReceivedOscIntroEnd(OSCMessage message)
        {
            Debug.Log(message);
			EventBus.TableSequenceEnded.Invoke();
        }
    }
}
