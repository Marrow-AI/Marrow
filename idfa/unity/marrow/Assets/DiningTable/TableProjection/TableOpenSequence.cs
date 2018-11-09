using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RenderHeads.Media.AVProVideo;
using TMPro;
using extOSC;

namespace Marrow
{
    public class TableOpenSequence : MonoBehaviour
    {
        [Header("OSC related")]
        public OSCTransmitter oSCTransmitter;
        public string showPlateOscAddress = "/intro/plate";

        private int[] showPlateNotes = { 62, 64, 65, 69 };

        /*
         * 1. Gan talk + music
         * 2. table lights on
         * 3. show detected texts + full t2i underneath
         * 3-1. full t2i fade out
         * 4. plates come in
         * 4-1. name tags come in one by one
         * 4-2. name tags hide, except Mom
         * 5. spotlight on Mom
         * 6. display script texts & t2i images
         */

        [Space(10)]
		public TableSpotLight mainLight;
        //public TableSpotLight[] spotLights;
        public TableSpotLight platesOnlySpotlight;
		public TableSpotLight spotLight;

        public GameObject title;
        public string mainTitleTexts;
		public GameObject speechDetection;

        public Renderer tableRenderer;
        
        public GameObject[] nameTags;
        public GameObject[] plates;
        public GameObject[] plateTexts;
		public GameObject scriptText;

        public Texture tableCookie;
        //public Shader textMeshProSurfaceShader;
        //public Shader textMeshProOverlayShader;

		private Vector3[] platesOriginalPosition;
		private Animator nameTagAnimator;

        private TextMeshPro textMeshProTitle;
		private TextMeshPro textMeshProSpeechDetection;
		private TextMeshPro textMeshProScript;
        
        private IEnumerator titleSequencePart1Coroutine;
        private IEnumerator titleSequencePart2Coroutine;
		private IEnumerator creditsSequenceCoroutine;
		private IEnumerator tableSequenceCoroutine;

        private Material tableNormalMaterial;
		private Material plateNormalMaterial;
		private Material plateTransparentMaterial;
        private IEnumerator platesDissovleCoroutine;

        private float startTimecode;
        private LightFlicker[] lightFlickers;

        [Header("Dev")]
        public AudioSource bgAudio;
        public bool devMode;
        public bool skipGanTalk;
        public Texture[] plateDevTextures;
        private int textureSwapCount;
        private WaitForSeconds stayWait;

		private bool tableSceneStart;

        private void OnEnable()
        {
			EventBus.TableSequenceStarted.AddListener(FadeInTable);
			EventBus.DinnerQuestionStart.AddListener(StartTableDinner);
            
			EventBus.DiningRoomEnded.AddListener(StartCredits);
        }

        private void OnDisable()
        {
			EventBus.TableSequenceStarted.RemoveListener(FadeInTable);
			EventBus.DinnerQuestionStart.RemoveListener(StartTableDinner);
            
			EventBus.DiningRoomEnded.RemoveListener(StartCredits);
        }

        void Start()
        {
            textMeshProTitle = title.GetComponent<TextMeshPro>();
			textMeshProSpeechDetection = speechDetection.GetComponent<TextMeshPro>();
			textMeshProScript = scriptText.GetComponent<TextMeshPro>();

 			nameTagAnimator = nameTags[0].transform.parent.GetComponent<Animator>();

            tableNormalMaterial = tableRenderer.sharedMaterial;
			plateNormalMaterial = ExperienceTableManager.Instance.plateMaterial;
			plateTransparentMaterial = ExperienceTableManager.Instance.plateTransparentMaterial;

            //lightFlickers = new LightFlicker[spotLights.Length];
            //for (int i = 0; i < lightFlickers.Length; i++)
                //lightFlickers[i] = spotLights[i].GetComponent<LightFlicker>();

            stayWait = new WaitForSeconds(1f);

			platesOriginalPosition = new Vector3[plates.Length];
			for (int i = 0; i < platesOriginalPosition.Length; i++)
				platesOriginalPosition[i] = plates[i].transform.position;
                
            Setup();

            //if (devMode)
            //{
            //    if (skipGanTalk)
            //        bgAudio.time = 31.7f;

            //    bgAudio.Play();
            //    StartTitle();
            //}

        }

        private void Update()
        {
			// To Avner
            if (Input.GetKeyDown("s"))
            {
                SendOSCofShowingPlate();
            }

			// From Avner
			if (Input.GetKeyDown("1"))
				FadeInTable();
			else if (Input.GetKeyDown("2"))
				ExperienceTableManager.Instance.ReceivedOscShowChosenDinner(null);
			//else if (Input.GetKeyDown("3"))
			//EndTableDinner();
			else if (Input.GetKeyDown("4"))
				ShowPlates();
			else if (Input.GetKeyDown("l"))
				SpotlightOnMom();
			else if (Input.GetKeyDown("f"))
				FadeOutTableForEnding();
			else if (Input.GetKeyDown("e"))
				ShowEndTitles();
        }

        public void Setup()
        {
            // reset
            textMeshProTitle.color = Color.clear;
			textMeshProSpeechDetection.color = Color.clear;
            UpdateNameTagsColor(Color.clear);
			nameTagAnimator.enabled = true;
			ExperienceTableManager.Instance.ReactToGanSpeak = false;

			// Change table material back
            tableRenderer.material = tableNormalMaterial;

            // Lights
			spotLight.Restart();
            platesOnlySpotlight.Restart();

            // toggle off stuff
			title.SetActive(false);
			speechDetection.SetActive(false);
            scriptText.SetActive(false);

            plateNormalMaterial.SetTexture("_MainTex", null);
            plateNormalMaterial.SetTexture("_SecondTex", null);
            plateNormalMaterial.SetFloat("_Blend", 0);
			plateTransparentMaterial.SetTexture("_MainTex", null);
			plateTransparentMaterial.SetTexture("_SecondTex", null);
			plateTransparentMaterial.SetFloat("_Blend", 0);

			for (int i = 0; i < plates.Length; i++)
            {
				//plates[i].transform.position = new Vector3(
				//platesOriginalPosition[i].x,
				//platesOriginalPosition[i].y,
				//platesOriginalPosition[i].z + 7.15f);
				plates[i].GetComponent<Renderer>().material = plateNormalMaterial;
                plates[i].SetActive(false);
            }
            for (int i = 0; i < nameTags.Length; i++)
                nameTags[i].SetActive(false);

            for (int i = 0; i < plateTexts.Length; i++)
                plateTexts[i].SetActive(false);
			
            // re-position stuff

            // clean stuff
        }
        
		///////////////////////////////
        ///     Events Sequences     //
		///////////////////////////////

        /// ======== OLD =========
        public void StartTitle()
        {
            startTimecode = Time.time;

            // TODO: cancel if it's running
            titleSequencePart1Coroutine = TitleSequence();
            StartCoroutine(titleSequencePart1Coroutine);
        }

		public void StartCredits()
		{
			Setup();
			creditsSequenceCoroutine = CreditsSequence();
			StartCoroutine(creditsSequenceCoroutine);
		}
		/// ======================

		/// ======== NEW =========
		public void FadeInTable()
        {
            startTimecode = Time.time;
			tableSceneStart = true;

			// Change table material
            tableRenderer.material = plateNormalMaterial;

			// Main light on
            mainLight.ToggleOn(true, 1f, 3f, 0f);

			LogCurrentTimecode("Fade In Table");
        }

        void StartReactToGanSpeak()
		{
			ExperienceTableManager.Instance.ReactToGanSpeak = true;
		}

		public void StartT2i()
		{
			// ?
		}

		public void UpdateSpeechDetectionText(string _text)
		{
			textMeshProSpeechDetection.text = _text;
		}

		public void StartTableDinner()
        {
			mainLight.ToggleOn(true, 1.7f, 1f, 0f);
			mainLight.ChangeLightColor(Color.white);

			// Show speech detection texts
			speechDetection.SetActive(true);
			LeanTween.value(speechDetection, Color.clear, Color.white, 1f)
			         .setOnUpdate((Color col)=>{ textMeshProSpeechDetection.color = col; });
            
			//if (devMode)
			//{
			//	textMeshProSpeechDetection.text = "roasted chcken";
   //             ExperienceTableManager.Instance.OnAttnGanInputUpdate("roasted chcken");
			//}

			LogCurrentTimecode("Start Table Dinner Qs");

			EndTableDinner();
        }

		public void EndTableDinner()
        {			
			LogCurrentTimecode("End Table Dinner Qs");
			StartCoroutine(EndTableDinnerSeqeunce());
        }

		IEnumerator EndTableDinnerSeqeunce()
		{
			yield return new WaitForSeconds(5f);

			// hide speech detection texts
            LeanTween.value(speechDetection, Color.white, Color.clear, 1f)
                     .setOnUpdate((Color col) => { textMeshProSpeechDetection.color = col; })
                     .setOnComplete(() => {
                         speechDetection.SetActive(false);
                         textMeshProSpeechDetection.text = "";
                     });

            LogCurrentTimecode("End Table Dinner Qs");
		}

		public void ShowPlates()
        {
			LogCurrentTimecode("Show Plates Ani");

			StartCoroutine(ShowPlateSequence());
        }

		public void SpotlightOnMom()
        {
			LogCurrentTimecode("Spotlight On Mom");

			StartCoroutine(SpotlightOnMomSequence());
        }

		public void SpotlightOnMom(string role)
        {
            LogCurrentTimecode("Spotlight On Mom");

            StartCoroutine(SpotlightOnMomSequence());
        }

		public void FadeOutTableForEnding()
        {
			LogCurrentTimecode("Fade Out Table For Ending");

			StartCoroutine(FadeOutTableSequence());
        }

		public void ShowEndTitles()
		{
			LogCurrentTimecode("Show End Titles");

			StartCoroutine(ShowEndTitlesSequence());
		}
        
		/// ======================
		IEnumerator ShowPlateSequence()
		{
			platesOnlySpotlight.ToggleOn(true, .5f, 2f, 1f);

			// v1 - Play plates in animation
            /*
            for (int i = 0; i < plates.Length; i++)
            {
				plates[i].SetActive(true);
                LeanTween.moveZ(plates[i], platesOriginalPosition[i].z, Random.Range(1f, 1.5f))
				         .setDelay(i*Random.Range(0.5f, 1f))
				         .setOnComplete(()=>{  })
                         .setEaseOutBack();
            }
            */

            // V2 - Fade in plates
			for (int i = 0; i < plates.Length; i++)
            {
				plates[i].GetComponent<Renderer>().material = plateTransparentMaterial;
				plateTransparentMaterial.color = Color.clear;
                plates[i].SetActive(true);
            }
			LeanTween.value(plates[0], Color.clear, Color.white, 2f)
					 .setOnUpdate((Color col) => { plateTransparentMaterial.color = col; });

			yield return new WaitForSeconds(2.1f);

			for (int i = 0; i < plates.Length; i++)
				plates[i].GetComponent<Renderer>().material = plateNormalMaterial;

			yield return new WaitForSeconds(2.4f);

			for (int i = 0; i < nameTags.Length; i++)
				nameTags[i].SetActive(true);
			nameTagAnimator.SetTrigger("Show");

			yield return new WaitForSeconds(4f);

			// Hide name tags
			nameTagAnimator.SetTrigger("Hide");
            
			mainLight.ResetColor();
			platesOnlySpotlight.ToggleOn(true, 2f, 2f, 0f);
            
			yield return new WaitForSeconds(3f);

			nameTagAnimator.enabled = false;

			// Texture fade out
			ExperienceTableManager.Instance.FadeTextureToColor();

			yield return new WaitForSeconds(2f);

			Debug.Log("Change table material back");
            tableRenderer.material = tableNormalMaterial;
		}

		IEnumerator SpotlightOnMomSequence()
        {
            LogCurrentTimecode("Spotlight on mom");
			//spotLight.TargetOnPlate(0, 2.4f, 1f);

			//spotLight.GetComponent<MoveSpotLight>().UpdateSpotlightPosition("mom");

			scriptText.SetActive(true);

            yield return null;

			StartReactToGanSpeak();

            EventBus.TableSequenceEnded.Invoke();
        }

		IEnumerator FadeOutTableSequence()
		{
			mainLight.ToggleOn(false, 0f, 3f, 0f);
            platesOnlySpotlight.ToggleOn(false, 0f, 3f, 0);
            spotLight.ToggleOn(false, 0f, 3f, 0);

            yield return new WaitForSeconds(3.1f);

            HideSpotLightAndTexts(true);

            for (int i = 0; i < plates.Length; i++)
                plates[i].SetActive(false);
		}

		IEnumerator ShowEndTitlesSequence()
		{
            // fade in main titles
            title.SetActive(true);
            LeanTween.value(title, UpdateMainTitleColor, Color.clear, Color.white, 2f);

			yield return new WaitForSeconds(5f);
            
			// fade out main titles
			LeanTween.value(title, UpdateMainTitleColor, Color.white, Color.clear, 3f)
			         .setOnComplete(()=>{ title.SetActive(false); });

			yield return new WaitForSeconds(3f);

			LogCurrentTimecode("Table end end!");
		}

		public void HideSpotLightAndTexts(bool toHide)
		{
			if (toHide)
			{
				spotLight.gameObject.SetActive(false);
				scriptText.SetActive(false);
				for (int i = 0; i < plates.Length; i++)
                {
                    nameTags[i].SetActive(false);
                }
			}
			else
			{
				spotLight.gameObject.SetActive(true);
				scriptText.SetActive(true);
				for (int i = 0; i < plates.Length; i++)
                {
					nameTags[i].SetActive(true);
                }
			}
		}

		/// ======================

        IEnumerator TitleSequence()
        {
            // Temp
            if (!skipGanTalk)
                yield return new WaitForSeconds(32f);

            LogCurrentTimecode("Lights on * 4");
            //spotLights[0].ToggleOn(true, 1.5f, 2f, 0);
            //spotLights[1].ToggleOn(true, 1.5f, 1f, 2.5f);
            //spotLights[2].ToggleOn(true, 1.5f, 1.5f, 3.5f);
            //spotLights[3].ToggleOn(true, 1.5f, 1f, 5f);

            yield return new WaitForSeconds(6f);

            LogCurrentTimecode("Main title");
            textMeshProTitle.text = mainTitleTexts.Replace("\\n", "\n");
            title.SetActive(true);
            LeanTween.value(title, UpdateMainTitleColor, Color.clear, Color.white, 2f);

            yield return new WaitForSeconds(5.5f);

            LogCurrentTimecode("Lights off one by one");
            LeanTween.value(title, UpdateMainTitleColor, Color.white, Color.clear, 2f);
            //spotLights[0].ToggleOn(false, 0f, 0.2f, 1.5f);
            //spotLights[1].ToggleOn(false, 0f, 0.2f, 1.75f);
            //spotLights[2].ToggleOn(false, 0f, 0.2f, 2f);
            //spotLights[3].ToggleOn(false, 0f, 0.2f, 2.25f);

            yield return new WaitForSeconds(3.5f);

            LogCurrentTimecode("Staring title");
            //textMeshProTitle.GetComponent<Renderer>().sharedMaterial.shader = textMeshProOverlayShader;
            UpdateMainTitleColor(Color.clear);

            yield return new WaitForSeconds(3f);
            //textMeshProTitle.GetComponent<Renderer>().sharedMaterial.shader = textMeshProSurfaceShader;

            LogCurrentTimecode("Title off");
            title.SetActive(false);
            
			titleSequencePart2Coroutine = TitleSequencePart2();
            StartCoroutine(titleSequencePart2Coroutine);
        }

        IEnumerator TitleSequencePart2()
        {
            LogCurrentTimecode("Table material change back");
            tableRenderer.material = tableNormalMaterial;
            for (int i = 0; i < plates.Length; i++)
            {
                //plates[i].GetComponent<Renderer>().sharedMaterial = plateTextMaterial;
                plates[i].SetActive(true);
            }

            //yield return new WaitForSeconds(2f);

            LogCurrentTimecode("Lights-on onto plates");
            //for (int i = 0; i < spotLights.Length; i++)
            //{
            //    spotLights[i].SetLightColor("#FEFFDD");
            //    spotLights[i].TargetOnPlate(i);
            //}

			SendOSCofShowingPlate();
			yield return stayWait;
			SendOSCofShowingPlate();
            yield return stayWait;
			SendOSCofShowingPlate();
            yield return stayWait;
			SendOSCofShowingPlate();

            yield return new WaitForSeconds(3f);

            // Table be well lit by main light
            LogCurrentTimecode("Table be well lit by main light");
            //spotLights[0].BecomeGeneralMainLight(tableCookie);

            // Others turn down
            //for (int i = 1; i < spotLights.Length; i++)
                //spotLights[i].ToggleOn(false, 0, 2f, 0);

            //yield return new WaitForSeconds(4f);

            platesOnlySpotlight.ToggleOn(true, .5f, 2f, 1f);

            // Show name tags/titles
            LogCurrentTimecode("Show name tags");
            for (int i = 0; i < nameTags.Length; i++)
                nameTags[i].SetActive(true);
            LeanTween.value(nameTags[0], UpdateNameTagsColor, Color.clear, Color.white, 2f);

            yield return new WaitForSeconds(3f);
            
            LogCurrentTimecode("Spotlight on mom");
            //spotLights[3].TargetOnPlateBlink(2.4f, 1f);

            yield return new WaitForSeconds(3f);

            LeanTween.value(nameTags[0], UpdateNameTagsColor, Color.white, Color.clear, 2f);

            yield return new WaitForSeconds(2f);

            // TODO: nameTags => change to speech2text

            //for (int i = 0; i < nameTags.Length; i++)
            //nameTags[i].SetActive(false);

            platesOnlySpotlight.ToggleOn(true, 2f, 2f, 0f);

            LogCurrentTimecode("Table sequence ends! Wait for talking.");
            int totalTime = Mathf.FloorToInt(Time.time - startTimecode);
            Debug.Log("Total time: " + totalTime);

            //if (devMode)
            //{
            //  platesDissovleCoroutine = AutoDissolvePlates();
            //  StartCoroutine(platesDissovleCoroutine);
            //}

            for (int i = 0; i < plateTexts.Length; i++)
                plateTexts[i].SetActive(true);

            EventBus.TableSequenceEnded.Invoke();
        }

		IEnumerator CreditsSequence()
        {
            LogCurrentTimecode("Lights on * 4");
            //spotLights[0].ToggleOn(true, 1.5f, 2f, 0);
            //spotLights[1].ToggleOn(true, 1.5f, 1f, 2.5f);
            //spotLights[2].ToggleOn(true, 1.5f, 1.5f, 3.5f);
            //spotLights[3].ToggleOn(true, 1.5f, 1f, 5f);

            yield return new WaitForSeconds(6f);

            LogCurrentTimecode("Credits title");
            textMeshProTitle.text = "Credits time!!";
            title.SetActive(true);
            LeanTween.value(title, UpdateMainTitleColor, Color.clear, Color.white, 2f);

			// TODO: credits!
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

        IEnumerator AutoDissolvePlates()
        {
            while (true)
            {
                yield return stayWait;

                int targetBlend;
                textureSwapCount++;
                if (textureSwapCount % 2 == 1)
                {
                    plateNormalMaterial.SetTexture("_SecondTex", plateDevTextures[textureSwapCount % plateDevTextures.Length]);
                    targetBlend = 1;
                }
                else
                {
                    plateNormalMaterial.SetTexture("_MainTex", plateDevTextures[textureSwapCount % plateDevTextures.Length]);
                    targetBlend = 0;
                }

                LeanTween.value(plates[0], plateNormalMaterial.GetFloat("_Blend"), targetBlend, 1f)
                         .setOnUpdate((float val) =>
                         {
                             plateNormalMaterial.SetFloat("_Blend", val);
                         });
            }
        }

        void LogCurrentTimecode(string info)
        {
            int currTime = Mathf.FloorToInt(Time.time - startTimecode);
            Debug.Log(currTime + ": " + info);
        }

        void UpdateMainTitleColor(Color col)
        {
            textMeshProTitle.color = col;
        }

        void UpdateNameTagsColor(Color col)
        {
            //for (int i = 0; i < textMeshProNameTags.Length; i++)
                //textMeshProNameTags[i].color = col;
        }

        void TurnOnFlicker(bool turnOn)
        {
            for (int i = 0; i < lightFlickers.Length; i++)
                lightFlickers[i].enabled = turnOn;
        }

		///////////////////////////
        ///         OSC         ///
		///////////////////////////

		void SendOSCofShowingPlate()
        {
            var message = new OSCMessage(showPlateOscAddress);
            message.AddValue(OSCValue.Int(showPlateNotes[Random.Range(0, 3)]));
            message.AddValue(OSCValue.Int(120));
            message.AddValue(OSCValue.Int(1));

            oSCTransmitter.Send(message);
        }
    }
}
