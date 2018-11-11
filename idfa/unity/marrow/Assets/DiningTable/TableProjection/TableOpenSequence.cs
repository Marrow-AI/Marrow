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
         * 4. plates fade in with t2i texture
         * 4-1. name tags come in one by one
         * 4-2. name tags hide, except Mom
         * 4-3. full t2i fade out
         * 5. spotlight on Mom
         * 6. display script texts & t2i images
         */

        [Space(10)]
		public TableSpotLight mainLight;
        public TableSpotLight platesOnlySpotlight;
		public TableSpotLight spotLight;

        public GameObject title;
        public string mainTitleTexts;
		public GameObject speechDetection;

        public Renderer tableRenderer;
        
        public GameObject[] nameTags;
        public GameObject[] plates;
		public GameObject scriptText;

		private Vector3[] platesOriginalPosition;
		private Animator nameTagAnimator;
		private MoveSpotLight moveSpotLight;
        private TextMeshPro textMeshProTitle;
		private TextMeshPro textMeshProSpeechDetection;
		private TextMeshPro textMeshProScript;
        
		//public Texture tableCookie;
        //private IEnumerator titleSequencePart1Coroutine;
        //private IEnumerator titleSequencePart2Coroutine;
        //private IEnumerator creditsSequenceCoroutine;
        //private IEnumerator tableSequenceCoroutine;
		//private IEnumerator platesDissovleCoroutine;
        
        private Material tableNormalMaterial;
		private Material plateNormalMaterial;
		private Material plateTransparentMaterial;

        private float startTimecode;
        private LightFlicker[] lightFlickers;
		private bool tableSceneStart;
		private bool tableSequenceIsEnded;
        
        [Header("Dev")]
        //public bool devMode;
        public Texture[] plateDevTextures;
        private int textureSwapCount;
        private WaitForSeconds stayWait;


        private void OnEnable()
        {            
			EventBus.ExperienceStarted.AddListener(OnControlStart);
			EventBus.ExperienceEnded.AddListener(OnControlStop);

			EventBus.TableStarted.AddListener(FadeInTable);
			EventBus.TableEnded.AddListener(FadeOutTable);
            EventBus.DinnerQuestionStart.AddListener(StartTableDinner);
        }

        private void OnDisable()
        {
			EventBus.ExperienceStarted.RemoveListener(OnControlStart);
			EventBus.ExperienceEnded.RemoveListener(OnControlStop);

			EventBus.TableStarted.RemoveListener(FadeInTable);
			EventBus.TableEnded.RemoveListener(FadeOutTable);
			EventBus.DinnerQuestionStart.RemoveListener(StartTableDinner);
        }

        void Start()
        {
            textMeshProTitle = title.GetComponent<TextMeshPro>();
			textMeshProSpeechDetection = speechDetection.GetComponent<TextMeshPro>();
			textMeshProScript = scriptText.GetComponent<TextMeshPro>();
			moveSpotLight = spotLight.GetComponent<MoveSpotLight>();
 			nameTagAnimator = nameTags[0].transform.parent.GetComponent<Animator>();

            tableNormalMaterial = tableRenderer.sharedMaterial;
			plateNormalMaterial = ExperienceTableManager.Instance.plateMaterial;
			plateTransparentMaterial = ExperienceTableManager.Instance.plateTransparentMaterial;
            
            stayWait = new WaitForSeconds(1f);

			platesOriginalPosition = new Vector3[plates.Length];
			for (int i = 0; i < platesOriginalPosition.Length; i++)
				platesOriginalPosition[i] = plates[i].transform.position;
                
            Setup();            
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
			else if (Input.GetKeyDown("4"))
				ReceivedOscShowPlates(null);
			else if (Input.GetKeyDown("l"))
				ReceivedOscSpotlight("mom");
			else if (Input.GetKeyDown("f"))
				FadeOutTable();
			else if (Input.GetKeyDown("e"))
				ReceivedOscTableTitles(null);
        }

        public void Setup()
        {
            // reset
            textMeshProTitle.color = Color.clear;
			textMeshProSpeechDetection.color = Color.clear;
            //UpdateNameTagsColor(Color.clear);
			nameTagAnimator.enabled = true;
			ExperienceTableManager.Instance.ReactToGanSpeak = false;
			tableSceneStart = false;
			tableSequenceIsEnded = false;

			// Table - change material back
            tableRenderer.material = tableNormalMaterial;

			// Lights
			mainLight.Restart();
			spotLight.Restart();
            platesOnlySpotlight.Restart();

            // Plates
            //plateNormalMaterial.SetTexture("_MainTex", null);
            //plateNormalMaterial.SetTexture("_SecondTex", null);
            plateNormalMaterial.SetFloat("_Blend", 0);
			plateNormalMaterial.SetFloat("_Fade", 1);
			//plateTransparentMaterial.SetTexture("_MainTex", null);
			//plateTransparentMaterial.SetTexture("_SecondTex", null);
			plateTransparentMaterial.SetFloat("_Blend", 0);
			plateTransparentMaterial.SetFloat("_Fade", 1);

			// toggle off stuff
            title.SetActive(false);
            speechDetection.SetActive(false);
			scriptText.GetComponent<Renderer>().enabled = false;

			for (int i = 0; i < plates.Length; i++)
            {
				plates[i].GetComponent<Renderer>().material = plateNormalMaterial;
                plates[i].SetActive(false);
            }
            for (int i = 0; i < nameTags.Length; i++)
                nameTags[i].SetActive(false);
        }
        
		///////////////////////////////
        ///     Events Callbacks     //
		///////////////////////////////
		public void OnControlStart()
		{
			Setup();
		}

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

		public void StartTableDinner()
        {
            mainLight.ToggleOn(true, 1.7f, 1f, 0f);
            mainLight.ChangeLightColor(Color.white);

            // Show speech detection texts
            speechDetection.SetActive(true);
            LeanTween.value(speechDetection, Color.clear, Color.white, 1f)
                     .setOnUpdate((Color col) => { textMeshProSpeechDetection.color = col; });

            //if (devMode)
            //{
            //  textMeshProSpeechDetection.text = "roasted chcken";
            //             ExperienceTableManager.Instance.OnAttnGanInputUpdate("roasted chcken");
            //}

            LogCurrentTimecode("Start Table Dinner Qs");

            EndTableDinner();
        }

		public void FadeOutTable()
        {
            LogCurrentTimecode("Fade Out Table For Ending");

            StartCoroutine(FadeOutTableSequence());
        }

		public void OnControlStop()
		{
			// End everything
			LeanTween.cancelAll(false);
			nameTagAnimator.enabled = false;

			// Fade out everything
			mainLight.RestartSoftly();
			spotLight.RestartSoftly();
			platesOnlySpotlight.RestartSoftly();
			spotLight.GetComponent<MoveSpotLight>().ResetNameTags(.1f);

			FadeOutTMPTexts();
		}

		//////////////////////////////
        ///       OSC related       //
        //////////////////////////////

		public void ReceivedOscTableTitles(OSCMessage message)
        {
            LogCurrentTimecode("Show End Titles");

            StartCoroutine(ShowEndTitlesSequence());
        }

		public void ReceivedOscShowPlates(OSCMessage message)
        {
            LogCurrentTimecode("Show Plates Ani");

            StartCoroutine(ShowPlateSequence());
        }

        public void ReceivedOscSpotlight(string role)
        {
            LogCurrentTimecode("Spotlight On " + role);

            StartCoroutine(SpotlightOnRoleSequence(role));
        }

        void SendOSCofShowingPlate()
        {
            var message = new OSCMessage(showPlateOscAddress);
            message.AddValue(OSCValue.Int(showPlateNotes[Random.Range(0, 3)]));
            message.AddValue(OSCValue.Int(120));
            message.AddValue(OSCValue.Int(1));

            oSCTransmitter.Send(message);
        }

		///////////////////////////////
		///////////////////////////////
        ///////////////////////////////

        void FadeOutTMPTexts()
		{
			if (title.activeSelf)
            {
                LeanTween.value(title, UpdateMainTitleColor, Color.white, Color.clear, .1f)
                     .setOnComplete(() => { title.SetActive(false); });
            }
            if (speechDetection.activeSelf)
            {
                LeanTween.value(speechDetection, Color.white, Color.clear, .1f)
                     .setOnUpdate((Color col) => { textMeshProSpeechDetection.color = col; })
                     .setOnComplete(() => {
                         speechDetection.SetActive(false);
                         textMeshProSpeechDetection.text = "";
                     });
            }
			if (scriptText.GetComponent<Renderer>().enabled)
            {
                LeanTween.value(scriptText, Color.white, Color.clear, .1f)
                         .setOnUpdate((Color col) => { textMeshProScript.color = col; })
                         .setOnComplete(() => {
					         scriptText.GetComponent<Renderer>().enabled = true;
                             textMeshProScript.text = "";
                             textMeshProScript.color = Color.white;
                         });
            }
		}

        void StartReactToGanSpeak()
		{
			ExperienceTableManager.Instance.ReactToGanSpeak = true;
		}

		public void UpdateSpeechDetectionText(string _text)
		{
			textMeshProSpeechDetection.text = _text;
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

			yield return new WaitForSeconds(2.5f);

			for (int i = 0; i < plates.Length; i++)
				plates[i].GetComponent<Renderer>().material = plateNormalMaterial;

			yield return new WaitForSeconds(1.5f);

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

			// Table fade to color
			tableRenderer.material = plateTransparentMaterial;
			LeanTween.value(tableRenderer.gameObject, 0f, 1f, 1f)
                     .setOnUpdate((float val) => { plateTransparentMaterial.SetFloat("_Fade", val); });

			yield return new WaitForSeconds(2f);

			Debug.Log("Change table material back");
            tableRenderer.material = tableNormalMaterial;
			plateTransparentMaterial.SetFloat("_Fade", 0);
		}

		IEnumerator SpotlightOnRoleSequence(string role)
        {
			if (!tableSequenceIsEnded && role=="mom")
			{
				LogCurrentTimecode("Spotlight on mom");
                //spotLight.GetComponent<MoveSpotLight>().UpdateSpotlightPosition("mom");
				scriptText.GetComponent<Renderer>().enabled = true;

                yield return null;

                StartReactToGanSpeak();
                EventBus.TableOpeningEnded.Invoke();
				tableSequenceIsEnded = true;
			}
			else
			{
				spotLight.GetComponent<MoveSpotLight>().UpdateSpotlightPosition("mom");
			}
        }

		IEnumerator FadeOutTableSequence()
		{
			EventBus.T2IDisable.Invoke();
			ExperienceTableManager.Instance.ReactToGanSpeak = false;
            
			spotLight.ToggleOn(false, 0f, .1f, 0);
			mainLight.ToggleOn(false, 0f, .1f, 0.1f);
            platesOnlySpotlight.ToggleOn(false, 0f, .1f, 0.1f);
            
			FadeOutTMPTexts();

			spotLight.GetComponent<MoveSpotLight>().ResetNameTags(.1f);

            yield return new WaitForSeconds(0.5f);

			for (int i = 0; i < nameTags.Length; i++)
                nameTags[i].SetActive(false);

            for (int i = 0; i < plates.Length; i++)
                plates[i].SetActive(false);
		}

		IEnumerator ShowEndTitlesSequence()
		{
            // fade in main titles
            title.SetActive(true);
            LeanTween.value(title, UpdateMainTitleColor, Color.clear, Color.white, 2f);

			yield return new WaitForSeconds(10f);
            
			// fade out main titles
			LeanTween.value(title, UpdateMainTitleColor, Color.white, Color.clear, 3f)
			         .setOnComplete(()=>{ title.SetActive(false); });

			yield return new WaitForSeconds(3f);

			LogCurrentTimecode("Table end end! Should be pitch black");
		}

		public void HideSpotLightAndTexts(bool toHide)
		{
			if (toHide)
			{
				moveSpotLight.ResetNameTags(0.5f);
				spotLight.ToggleOn(false, 0f, 0.5f, 0);
				scriptText.GetComponent<Renderer>().enabled = false;
				textMeshProScript.text = "";
			}
			else
			{
				scriptText.GetComponent<Renderer>().enabled = true;
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

		/// ======================
        /*
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
        */

       


    }
}
