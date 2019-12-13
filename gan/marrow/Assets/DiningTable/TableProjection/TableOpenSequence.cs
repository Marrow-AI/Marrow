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
		public Animator nameTagAnimator;
		private MoveSpotLight moveSpotLight;
       private TextMeshPro textMeshProTitle;
		private TextMeshPro textMeshProSpeechDetection;
		private TextMeshPro textMeshProScript;

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

        //animated plates color
        public float speed = 0.5f;
        public Color startColor;
        public Color secondColor;
        public Color endColor;
        float startTime;

        private GameObject openLineAnima;

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
          mainLight.ResetColor();
          spotLight.RestartSoftly();
          platesOnlySpotlight.Restart();

            tableNormalMaterial = tableRenderer.sharedMaterial;
			plateNormalMaterial = ExperienceTableManager.Instance.plateMaterial;
			plateTransparentMaterial = ExperienceTableManager.Instance.plateTransparentMaterial;

          stayWait = new WaitForSeconds(1f);

			platesOriginalPosition = new Vector3[plates.Length];
			for (int i = 0; i < platesOriginalPosition.Length; i++)
			platesOriginalPosition[i] = plates[i].transform.position;

            openLineAnima = GameObject.Find("openLineAnima");
            enableOpenline(false);

            Setup();          
        }

        private void enableOpenline(bool value) {
           Renderer[] spriteRenderers = openLineAnima.GetComponentsInChildren<Renderer>();
           foreach (Renderer renderer in spriteRenderers) {
               renderer.enabled = value;
           }
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
				ReceivedOscShowPlates(1);
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
            plateNormalMaterial.SetFloat("_Blend", 0);
            //plateNormalMaterial.SetFloat("_Fade", 1);
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

		//public void LightUpWithChosenDinner()
		//{
		//	mainLight.ToggleOn(true, 1.7f, 1f, 0f);
  //          mainLight.ChangeLightColor(Color.white);
		//}
        
		public void StartTableDinner()
        {
            mainLight.ToggleOn(true, 2.9f, 1f, 0f);
            mainLight.ChangeLightColor(Color.white);

            // Show speech detection texts
            speechDetection.SetActive(true);
            LeanTween.value(speechDetection, Color.clear, Color.white, 1f)
                     .setOnUpdate((Color col) => { textMeshProSpeechDetection.color = col; });

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
			//nameTagAnimator.enabled = false;

			// Fade out everything
			mainLight.RestartSoftly();
			spotLight.RestartSoftly();
			platesOnlySpotlight.RestartSoftly();
			spotLight.GetComponent<MoveSpotLight>().ResetNameTags(.1f);

			if (title.activeSelf)
    			FadeOutTMPTexts(3f);
			else
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

		public void ReceivedOscShowPlates(int fast)
        {
            LogCurrentTimecode("Show Plates Ani");

            StartCoroutine(ShowPlateSequence(fast));
        }

        public void ReceivedOscSpotlight(string role)   
        {
            LogCurrentTimecode("Spotlight On " + role);

            StartCoroutine(SpotlightOnRoleSequence(role));
        }   

        public void ReceivedOscOpenLine(string role)
        {
            LogCurrentTimecode("Open line On " + role);
            if (role == "clear") {
                enableOpenline(false);
            } else {
                enableOpenline(true);
            }


          //  StartCoroutine(SpotlightOnRoleSequence(role));
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
			FadeOutTMPTexts(0.1f);
		}

		void FadeOutTMPTexts(float speed)
		{
			if (title.activeSelf)
            {
				LeanTween.value(title, UpdateMainTitleColor, Color.white, Color.clear, speed)
                     .setOnComplete(() => { title.SetActive(false); });
            }
            if (speechDetection.activeSelf)
            {
				LeanTween.value(speechDetection, Color.white, Color.clear, speed)
                     .setOnUpdate((Color col) => { textMeshProSpeechDetection.color = col; })
                     .setOnComplete(() => {
                         speechDetection.SetActive(false);
                         textMeshProSpeechDetection.text = "";
                     });
            }
			if (scriptText.GetComponent<Renderer>().enabled)
            {
				LeanTween.value(scriptText, Color.white, Color.clear, speed)
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
		IEnumerator ShowPlateSequence(int fastInt)
        {
            tableRenderer.material = tableNormalMaterial;
            mainLight.ResetColor();
            spotLight.RestartSoftly();
            platesOnlySpotlight.Restart();
            spotLight.ToggleOn(false, 0f, 0.5f, 0);

            bool fast = (fastInt == 1);

            Debug.Log("Start plate sequence (Fast:" + fast + ")" );

            // Table fade to color
            tableRenderer.material = plateTransparentMaterial;
            LeanTween.value(tableRenderer.gameObject, 0f, 1f, 1f)
                     .setOnUpdate((float val) => { plateTransparentMaterial.SetFloat("_Fade", val); });


            platesOnlySpotlight.ToggleOn(true, 2f, 1f, 0f);
            mainLight.RestartSoftly();


            yield return new WaitForSeconds(fast ? 0 : 2f);


            Debug.Log("Change table material back");
            tableRenderer.material = tableNormalMaterial;
            //plateTransparentMaterial.SetFloat("_Fade", 0);

            // v1 - Play plates in animation
            for (int i = 0; i < plates.Length; i++)
            {
                plates[i].GetComponent<Renderer>().material = plateTransparentMaterial;
                plateTransparentMaterial.color = startColor;
                plates[i].SetActive(true);
            }

            LeanTween.value(plates[0], startColor, secondColor, 15f)
                .setEaseOutBack()
                .setOnUpdate((Color col) => { startColor = col; });

            scriptText.GetComponent<Renderer>().enabled = false;

            yield return new WaitForSeconds(fast ? 0 : 10f);

            for (int i = 0; i < nameTags.Length; i++)
            {
                nameTags[i].SetActive(true);
                nameTagAnimator = nameTags[i].transform.parent.GetComponent<Animator>();
                nameTagAnimator.enabled = true;
            }
            //nameTagAnimator.SetTrigger("Show");
            //nameTagAnimator.enabled = true;
           // nameTagAnimator.SetTrigger("Show");

            for (int i = 0; i < plates.Length; i++)
            {
                plates[i].GetComponent<Renderer>().material = plateTransparentMaterial;
                plateTransparentMaterial.color = secondColor;
                plates[i].SetActive(true);
            }

            LeanTween.value(plates[0], secondColor, Color.white, 15f).setEaseOutElastic();

             //open text 2 image texture
             for (int i = 0; i < plates.Length; i++)
               plates[i].GetComponent<Renderer>().material = plateNormalMaterial;

            yield return new WaitForSeconds(fast ? 0 : 1f);

            // Hide name tags
            //nameTagAnimator.SetTrigger("Hide");

            yield return new WaitForSeconds(fast ? 0 : 10f);

            scriptText.GetComponent<Renderer>().enabled = true;
        }

       
        IEnumerator SpotlightOnRoleSequence(string role)
        {
			if (!tableSequenceIsEnded && role=="mom")
			{
				LogCurrentTimecode("Spotlight on mom");
                //spotLight.GetComponent<MoveSpotLight>().UpdateSpotlightPosition("mom");
				//scriptText.GetComponent<Renderer>().enabled = true;

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
            Debug.Log("Fade out table sequence");
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
            title.SetActive(false);
            LeanTween.value(title, UpdateMainTitleColor, Color.clear, Color.white, 2f);

			// V1- fade out titles
			/*
			yield return new WaitForSeconds(10f);
            
			// fade out main titles
			LeanTween.value(title, UpdateMainTitleColor, Color.white, Color.clear, 3f)
			         .setOnComplete(()=>{ title.SetActive(false); });

			yield return new WaitForSeconds(3f);

			LogCurrentTimecode("Table end end! Should be pitch black");
			*/

			yield return null;
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
			//	scriptText.GetComponent<Renderer>().enabled = true;
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

        void TurnOnFlicker(bool turnOn)
        {
            for (int i = 0; i < lightFlickers.Length; i++)
                lightFlickers[i].enabled = turnOn;
        }      

    }
}
