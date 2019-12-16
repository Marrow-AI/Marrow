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
                LeanTween.scale(styleGAN, new Vector3(0.5f, 0.5f, 0.5f), 60.0f);
            } else if (state == 2) {
                LeanTween.move(styleGAN, new Vector3(-45.11f, 1.0f, styleGAN.transform.position.z), 35.0f);
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
                gauGAN.GetComponent<Renderer>().enabled = true;
                deepLab.GetComponent<Renderer>().enabled = true;
                Material deeplabMaterial = deepLab.GetComponent<Renderer>().material;
                LeanTween.value(deepLab, deeplabMaterial.GetFloat("_Transparency"), 1.0f, 30.0f)
                .setOnUpdate((float val) => {
                    deeplabMaterial.SetFloat("_Transparency", val);
                });
            } else if (state == 2) {
                deepLab.GetComponent<Renderer>().enabled = false;
                Material gauganMaterial = gauGAN.GetComponent<Renderer>().material;
                gauganMaterial.SetFloat("_Blend", 0.0f);
            } else if (state == 3) {
                gauGAN.GetComponent<Renderer>().enabled = true;
                deepLab.GetComponent<Renderer>().enabled = true;
                Material deeplabMaterial = deepLab.GetComponent<Renderer>().material;
                deeplabMaterial.SetFloat("_Transparency", 1.0f);              
            }
        }
    }
}
