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



            Setup();


			if (devMode)
			{
				OnTableSequenceEnded();
			}            
        }

        void Setup()
		{
            styleGANCamera.enabled = false;
            gauGAN.GetComponent<Renderer>().enabled = false;
            deepLab.GetComponent<Renderer>().enabled = false;
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
            styleGANCamera.enabled = (active  == 1);
        }

        public void ReceivedOscStyleGANScale(int state)
        {
            if (state == 1) {
                LeanTween.moveX(styleGAN, -44.0f, 60.0f);
                LeanTween.scale(styleGAN, new Vector3(0.68f, 0.68f, 0.68f), 60.0f);
            } else if (state == 2) {
                LeanTween.move(styleGAN, new Vector3(-45.11f, -0.55f, styleGAN.transform.position.z), 35.0f);
                LeanTween.scale(styleGAN, new Vector3(0.75f, 0.75f, 0.75f), 35.0f);
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


                gauGAN.transform.position.Set(-43.78f, -0.65f, 17.49f);

                deepLab.GetComponent<Renderer>().enabled = true;
                rawImage.GetComponent<Renderer>().enabled = true;
                Material rawImageMaterial = rawImage.GetComponent<Renderer>().material;
                Material deeplabMaterial = deepLab.GetComponent<Renderer>().material;

                LeanTween.value(
                    deepLab,
                    0.5f, 1.0f, 15f
                )
                .setOnUpdate((float val) => {
                    deeplabMaterial.SetFloat("_Tran sparency", val);
                });

                LeanTween.value(
                    rawImage,
                    1.0f, 0.0f, 15f
                )
                .setOnUpdate((float val) => {
                    deeplabMaterial.SetFloat("_Transparency", val);
                });
            }
        }
    }
}
