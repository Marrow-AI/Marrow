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

			Setup();


			if (devMode)
			{
				OnTableSequenceEnded();
			}            
        }

        void Setup()
		{
            styleGANCamera.enabled = false;
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
    }
}
