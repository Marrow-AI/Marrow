using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using extOSC;

namespace Marrow
{
	public class ExperienceRoomManager : Manager<ExperienceTableManager>
    {
		public SocketCommunication socketCommunication;
		public bool devMode;
		public bool simpleAnimation;

		private bool startPix2Pix;
		private bool socketIsConnected;
        private bool socketIsDisconnectedWillTryReconnect;

        private Camera styleGANCamera;
        private GameObject styleGAN;
        private GameObject gauGAN;
        private GameObject deepLab;
        private GameObject rawImage;
        private GameObject memory;

        private int bowlTweenId;



        private void OnEnable()
		{
			EventBus.TableOpeningEnded.AddListener(OnTableSequenceEnded);
			//EventBus.DiningRoomEnded.AddListener(OnDiningRoomEnded);
			//EventBus.ExperienceRestarted.AddListener(DisablePix2Pix);
            
			EventBus.WebsocketConnected.AddListener(OnWebsocketConnected);
			EventBus.WebsocketDisconnected.AddListener(OnWebsocketDisconnected);

		}

		private void OnDisable()
		{
			EventBus.TableOpeningEnded.RemoveListener(OnTableSequenceEnded);
			//EventBus.DiningRoomEnded.RemoveListener(OnDiningRoomEnded);
			//EventBus.ExperienceRestarted.RemoveListener(DisablePix2Pix);

			EventBus.WebsocketConnected.RemoveListener(OnWebsocketConnected);
			EventBus.WebsocketDisconnected.RemoveListener(OnWebsocketDisconnected);

		}

		private void Start()
        {

            Debug.Log("Starting experience manager");

            styleGANCamera = GameObject.Find("StyleGAN Camera").GetComponent<Camera>();
            styleGAN = GameObject.Find("StyleGAN");
            gauGAN = GameObject.Find("GauGAN");
            deepLab = GameObject.Find("Deeplab");
            rawImage = GameObject.Find("RawImage");
            memory = GameObject.Find("Memory");
            bowlTweenId = 0;



            Setup();


			if (devMode)
			{
				OnTableSequenceEnded();
			}            
        }

        void Setup()
		{
            gauGAN.GetComponent<Renderer>().enabled = false;
            deepLab.GetComponent<Renderer>().enabled = false;
            styleGAN.GetComponent<Renderer>().enabled = false;
        }

        private void Update()
		{

		}

		void OnTableSequenceEnded()
		{
		}

		void EnablePix2Pix()
		{
			startPix2Pix = true;
		}

        void OnDiningRoomEnded()
		{
			DisablePix2Pix();
		}

		void DisablePix2Pix()
        {
			startPix2Pix = false;
        }

		void OnWebsocketConnected()
		{
			socketIsConnected = true;
            if (socketIsDisconnectedWillTryReconnect)
            {
                RestartWebsocketConnection();
                socketIsDisconnectedWillTryReconnect = false;
            }
		}

		void OnWebsocketDisconnected()
		{
			socketIsConnected = false;
            socketIsDisconnectedWillTryReconnect = true;
        }

        void RestartWebsocketConnection()
        {
        }

		///////////////////////////
        ///     OSC related     ///
        ///////////////////////////
        
		public void ReceivedOscGanSpeaks(int status)
        {
        }

		public void ReceivedOscIntroEnd(OSCMessage message)
        {
            Debug.Log(message);
			EventBus.TableOpeningEnded.Invoke();
        }

        public void ReceivedOscCameraStyleGAN(int active)
        {
            styleGAN.GetComponent<Renderer>().enabled = (active  == 1);
        }

        public void ReceivedOscStyleGANScale(int state)
        {
            if (state == 1) {
                LeanTween.moveX(styleGAN, -44.0f, 60.0f);
                LeanTween.scale(styleGAN, new Vector3(0.68f, 0.68f, 0.68f), 60.0f);
            } else if (state == 2) {
                LeanTween.move(styleGAN, new Vector3(-45.11f, -0.55f, styleGAN.transform.position.z), 35.0f);
                // LeanTween.scale(styleGAN, new Vector3(0.75f, 0.75f, 0.75f), 35.0f);
                LeanTween.scale(styleGAN, new Vector3(0.6f, 0.6f, 0.6f), 35.0f);
            }
        }
        public void ReceivedOscStyleGANBlend(float value)
        {
            Debug.Log("Set StyleGAN Blend " + value);
            styleGAN.GetComponent<Renderer>().material.SetFloat("_Blend", value);
        }
        public void ReceivedOscGauGANState(int state)
        {
            Debug.Log("Set GauGAN state " + state);
            if (state == 1) {
                gauGAN.GetComponent<Renderer>().enabled = false;
                deepLab.GetComponent<Renderer>().enabled = false;
                rawImage.GetComponent<Renderer>().enabled = false;
            } else if (state == 2) {
                gauGAN.GetComponent<Renderer>().enabled = true;
            }  else if (state == 3) {

                gauGAN.GetComponent<Renderer>().enabled = true;

                gauGAN.transform.position = new Vector3(-43.78f, -0.65f, 15f);

                deepLab.GetComponent<Renderer>().enabled = true;
                //rawImage.GetComponent<Renderer>().enabled = true;
                Material rawImageMaterial = rawImage.GetComponent<Renderer>().material;
                Material deeplabMaterial = deepLab.GetComponent<Renderer>().material;

                LeanTween.value(
                    deepLab,
                    0.1f, 1.0f, 7.0f
                )
                .setOnUpdate((float val) => {
                    deeplabMaterial.SetFloat("_Transparency", val);
                });
                /*
                LeanTween.value(
                    rawImage,
                    0.3f, 0.0f, 7.0f
                )
                .setOnUpdate((float val) => {
                       rawImageMaterial.SetFloat("_TransparencyOrig", val);
                });*/
            } else if (state == 4) {
                Material rawImageMaterial = rawImage.GetComponent<Renderer>().material;
                Material deeplabMaterial = deepLab.GetComponent<Renderer>().material;

                rawImage.GetComponent<Renderer>().enabled = false;


                    LeanTween.value(
                        deepLab,
                        1.0f, 0.0f, 3.0f
                    )
                    .setOnUpdate((float val) => {
                        deeplabMaterial.SetFloat("_Transparency", val);
                    })
                .setOnComplete(() => {
                    deepLab.GetComponent<Renderer>().enabled = false;
                });


                deepLab.GetComponent<Renderer>().enabled = false;
            }
        }
        public void ReceivedStyleGANAnimationState(int state) {
            Debug.Log("Set StyleGAN Animation state " + state);
            Animator[] animators = styleGAN.GetComponentsInChildren<Animator>();
            SpriteRenderer[] renderers = styleGAN.GetComponentsInChildren<SpriteRenderer>();

            if (state == 1) {
                animators[1].enabled = false;
                renderers[1].enabled = false;
                animators[0].enabled = true;
                renderers[0].enabled = true;

            } else if (state == 2) {
                animators[0].enabled = false;
                animators[1].enabled = true;
                renderers[0].enabled = false;
                renderers[1].enabled = true;
            } else if (state == 3) {
                renderers[0].enabled = false;
                renderers[1].enabled = false;
            }
        }
        public void ReceivedDeeplabBowl(OSCMessage message) {

            int x = message.Values[0].IntValue;
            int y = message.Values[1].IntValue;

            Debug.Log("Received bowl position! (" + x + "," + y + ")");

            float memX = (x / 512.0f) * 14.0f - 7.0f;
            LeanTween.cancel(bowlTweenId);
            Vector3 worldPos = memory.transform.parent.TransformPoint(memX, memory.transform.localPosition.y, memory.transform.localPosition.z);
            Debug.Log("World pos: " + worldPos.x);
            bowlTweenId = LeanTween.moveX(memory, worldPos.x, 2.0f).id;
        }
        public void ReceivedMemoryState(int state) {
            Debug.Log("Set Memory Animation state " + state);
            VideoSelector videoSelector = memory.GetComponent<VideoSelector>();

            memory.GetComponent<Renderer>().enabled = false;
            if (state == 1) {
                videoSelector.playVideo(videoSelector.sister);  
            } else if (state == 2) {
                videoSelector.playVideo(videoSelector.father);
            } else if (state == 3) {
                videoSelector.playVideo(videoSelector.brother);
            } else if (state == 4) {
                videoSelector.playVideo(videoSelector.mother);
            }
            if (state > 0) {
                StartCoroutine(ShowMemoryAfter(0.5f));
            }
        }
        IEnumerator ShowMemoryAfter(float time) {
            yield return new WaitForSeconds(time);
            memory.GetComponent<Renderer>().enabled = true;
        }
    }
}
