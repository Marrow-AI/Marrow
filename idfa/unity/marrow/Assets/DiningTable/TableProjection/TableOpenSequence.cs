using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RenderHeads.Media.AVProVideo;
using TMPro;

namespace Marrow
{
    public class TableOpenSequence : MonoBehaviour
    {
		/*
		 * 1. Gan talk + music
		 * 2. Lights on * 4
		 * 3. Title in & out
		 * 3-1. black
		 * 3.2. swap table's ordinary material to video material
		 * 4. play table video
		 * 4-1. table video end
		 * 4-2. swap back to ordinary material
		 * 4-3. spot lights on one by one
		 * 5. swirl a bit
		 * 5-1.change to one spot light
		 * 5-2.pop up knives & forks
		 * 6. name tags
		 * 7. title of time & day
		 * 7-1.focus spot light on mom
		 * 8. name tags fade out
		 * 9. time to display texts & t2i images
		 */
		public TableSpotLight mainLight;
		public TableSpotLight spotLightA;
		public TableSpotLight spotLightB;
		public TableSpotLight spotLightC;

		public GameObject title;
		public GameObject timeTitle;

		public MediaPlayer videoPlayer;
		public Material videoMaterial;
		public Renderer tableRenderer;
		public Material plateTextMaterial;

        public GameObject[] knivesAndForks;
		public GameObject[] nameTags;

		public GameObject[] plates;
		public GameObject[] plateTexts;

		private TextMeshPro textMeshProTitle;
		private TextMeshPro textMeshProTimeTitle;
		private IEnumerator titleSequencePart1Coroutine;
		private IEnumerator titleSequencePart2Coroutine;
		private Material tableNormalMaterial;
		private Material plateNormalMaterial;

		private float startTimecode;

		[Header("Dev")]
		public bool devMode;
        
        void Start()
        {
			textMeshProTitle = title.GetComponent<TextMeshPro>();
			textMeshProTimeTitle = timeTitle.GetComponent<TextMeshPro>();
			tableNormalMaterial = tableRenderer.sharedMaterial;
			plateNormalMaterial = plates[0].GetComponent<Renderer>().sharedMaterial;
			videoPlayer.Events.AddListener(OnVideoEvent);

			Setup();
			if (devMode)
				StartTitle();
        }

		private void OnDestroy()
		{
			
		}

		public void Setup()
		{
			// reset
			textMeshProTitle.color = Color.clear;
			textMeshProTimeTitle.color = Color.clear;
			title.SetActive(false);
			timeTitle.SetActive(false);

			mainLight.Restart();
            spotLightA.Restart();
            spotLightB.Restart();
            spotLightC.Restart();

			// cancel all coroutine

			// toggle off stuff
			for (int i = 0; i < plates.Length; i++)
			{
				// set plates same as table normal
				plates[i].GetComponent<Renderer>().sharedMaterial = plateNormalMaterial;
				plates[i].SetActive(false);
			}
			for (int i = 0; i < nameTags.Length; i++)
            {
				nameTags[i].SetActive(false);
            }

            // re-position stuff

            // clean stuff
		}

		public void StartTitle()
		{
			startTimecode = Time.time;

			// TODO: cancel if it's running
			titleSequencePart1Coroutine = TitleSequence();
			StartCoroutine(titleSequencePart1Coroutine);
		}
        
		IEnumerator TitleSequence()
        {
			// Temp
			yield return new WaitForSeconds(3f);

			Debug.Log("Lights on * 4");
			mainLight.ToggleOn(true, 1f, 2f, 0);
			spotLightA.ToggleOn(true, 1f, 1.5f, 2.5f);
			spotLightB.ToggleOn(true, 1f, 1f, 4f);
			spotLightC.ToggleOn(true, 1f, 1f, 4.5f);
            
            yield return new WaitForSeconds(6.5f);

			Debug.Log("Title fade in");
			title.SetActive(true);
			LeanTween.value(title, Color.clear, Color.white, 2f)
			         .setOnUpdate((Color col) => {
                         textMeshProTitle.color = col;
                     });

			yield return new WaitForSeconds(6f);

			Debug.Log("Lights off one by one");
			spotLightB.ToggleOn(false, 0f, 0.2f, 0f);
			spotLightA.ToggleOn(false, 0f, 0.2f, 0.5f);
			spotLightC.ToggleOn(false, 0f, 0.2f, 1f);
			mainLight.ToggleOn(false, 0f, 0.2f, 1.5f);

			yield return new WaitForSeconds(3f);

			Debug.Log("Title off");
			textMeshProTitle.color = Color.clear;
			title.SetActive(false);
            
			Debug.Log("Table change to video materia; play video");
			tableRenderer.material = videoMaterial;
			videoPlayer.Play();

			// Wait for video be done (approx 13~15s)
        }

		IEnumerator TitleSequencePart2()
		{
			Debug.Log("Table material change back");
			tableRenderer.material = tableNormalMaterial;
			for (int i = 0; i < plates.Length; i++)
            {
				//plates[i].GetComponent<Renderer>().sharedMaterial = plateTextMaterial;
				plates[i].SetActive(true);
            }

			Debug.Log("Lights-on onto plates");
			// TODO: turn to unlit texture material
			mainLight.SetLightColor("#FEFFDD");
			spotLightA.SetLightColor("#FEFFDD");
			spotLightB.SetLightColor("#FEFFDD");
			spotLightC.SetLightColor("#FEFFDD");
			mainLight.TargetOnPlate(0);
			spotLightA.TargetOnPlate(1f);
			spotLightB.TargetOnPlate(2f);
			spotLightC.TargetOnPlate(3f);

			yield return new WaitForSeconds(5f);

			// Table be well lit by main light
			Debug.Log("Table be well lit by main light");
			mainLight.BecomeGeneralMainLight();
			// Others turn down
			spotLightA.ToggleOn(false, 0, 2f, 0);
			spotLightB.ToggleOn(false, 0, 2f, 0);
			spotLightC.ToggleOn(false, 0, 2f, 0);

			yield return new WaitForSeconds(3f);

            // Show name tags/titles
			Debug.Log("Show name tags");
			for (int i = 0; i < nameTags.Length; i++)
            {
				nameTags[i].SetActive(true);
            }

			yield return new WaitForSeconds(3f);

			// Show title & time
			Debug.Log("Show title & time");
			timeTitle.SetActive(true);
			LeanTween.value(timeTitle, Color.clear, Color.white, 2f)
			         .setOnUpdate((Color col) => {
                         textMeshProTimeTitle.color = col;
                     });

			yield return new WaitForSeconds(2f);
            
			Debug.Log("Spotlight on mom");
			spotLightC.TargetOnPlate(0);

			yield return new WaitForSeconds(3f);

			Debug.Log("Table sequence ends! Wait for talking.");
			float totalTime = Time.time - startTimecode;
			Debug.Log("Total time: " + totalTime);
		}

		void OnVideoEvent(MediaPlayer mp, MediaPlayerEvent.EventType et, ErrorCode errorCode)
        {
            switch (et)
            {
                case MediaPlayerEvent.EventType.ReadyToPlay:
					//
                    break;
                case MediaPlayerEvent.EventType.FinishedPlaying:
					// TODO: cancel if it's running
					Debug.Log("Video ends! Start title Sequence Part2");
					titleSequencePart2Coroutine = TitleSequencePart2();
					StartCoroutine(titleSequencePart2Coroutine);
                    break;
            }
        }
    }
}
