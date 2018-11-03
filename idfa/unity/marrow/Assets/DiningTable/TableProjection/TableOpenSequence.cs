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
         * 2. Lights on * 4
         * 3. Main title in & out
         * 3-1. Starting titles
         * 3-2. black
         * 3.3. swap table's ordinary material to video material
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
        [Space(10)]
        public TableSpotLight[] spotLights;
        public TableSpotLight platesOnlySpotlight;

        public GameObject title;
        public GameObject starringTitle;
        public GameObject timeTitle;
        public string mainTitleTexts;
        public string[] staringTitleTexts;

        public MediaPlayer videoPlayer;
        public Material videoMaterial;
        public Renderer tableRenderer;
        public Material plateTextMaterial;

        public GameObject[] knivesAndForks;
        public GameObject[] nameTags;

        public GameObject[] plates;
        public GameObject[] plateTexts;

        public Texture tableCookie;
        //public Shader textMeshProSurfaceShader;
        //public Shader textMeshProOverlayShader;

        private TextMeshPro textMeshProTitle;
        private TextMeshPro textMeshProStarringTitle;
        private TextMeshPro textMeshProTimeTitle;
        private TextMeshPro[] textMeshProNameTags;
        private IEnumerator titleSequencePart1Coroutine;
        private IEnumerator titleSequencePart2Coroutine;
		private IEnumerator creditsSequenceCoroutine;
        private Material tableNormalMaterial;
        private Material plateNormalMaterial;
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

        private void OnEnable()
        {
            EventBus.TableSequenceStarted.AddListener(StartTitle);
			EventBus.DiningRoomEnded.AddListener(StartCredits);
        }

        private void OnDisable()
        {
            EventBus.TableSequenceStarted.RemoveListener(StartTitle);
			EventBus.DiningRoomEnded.RemoveListener(StartCredits);
        }

        void Start()
        {
            textMeshProTitle = title.GetComponent<TextMeshPro>();
            textMeshProStarringTitle = starringTitle.GetComponent<TextMeshPro>();
            textMeshProTimeTitle = timeTitle.GetComponent<TextMeshPro>();
            textMeshProNameTags = new TextMeshPro[nameTags.Length];
            for (int i = 0; i < textMeshProNameTags.Length; i++)
                textMeshProNameTags[i] = nameTags[i].GetComponent<TextMeshPro>();

            tableNormalMaterial = tableRenderer.sharedMaterial;
            plateNormalMaterial = plates[0].GetComponent<Renderer>().sharedMaterial;
            videoPlayer.Events.AddListener(OnVideoEvent);

            lightFlickers = new LightFlicker[spotLights.Length];
            for (int i = 0; i < lightFlickers.Length; i++)
                lightFlickers[i] = spotLights[i].GetComponent<LightFlicker>();

            stayWait = new WaitForSeconds(1f);

            Setup();

            if (devMode)
            {
                if (skipGanTalk)
                    bgAudio.time = 31.7f;

                bgAudio.Play();
                StartTitle();
            }

        }

        private void Update()
        {
            if (Input.GetKeyDown("s"))
            {
                SendOSCofShowingPlate();
            }
        }

        public void Setup()
        {
            // reset
            //textMeshProTitle.GetComponent<Renderer>().sharedMaterial.shader = textMeshProSurfaceShader;
            textMeshProTitle.color = Color.clear;
            textMeshProStarringTitle.color = Color.clear;
            textMeshProTimeTitle.color = Color.clear;
            UpdateNameTagsColor(Color.clear);

            title.SetActive(false);
            starringTitle.SetActive(false);
            timeTitle.SetActive(false);

            for (int i = 0; i < spotLights.Length; i++)
                spotLights[i].Restart();
            platesOnlySpotlight.Restart();
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

            plateNormalMaterial.SetTexture("_MainTex", null);
            plateNormalMaterial.SetTexture("_SecondTex", null);
            plateNormalMaterial.SetFloat("_Blend", 0);

            for (int i = 0; i < plateTexts.Length; i++)
                plateTexts[i].SetActive(false);

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

		public void StartCredits()
		{
			Setup();
			creditsSequenceCoroutine = CreditsSequence();
			StartCoroutine(creditsSequenceCoroutine);
		}

        IEnumerator TitleSequence()
        {
            // Temp
            if (!skipGanTalk)
                yield return new WaitForSeconds(32f);

            LogCurrentTimecode("Lights on * 4");
            spotLights[0].ToggleOn(true, 1.5f, 2f, 0);
            spotLights[1].ToggleOn(true, 1.5f, 1f, 2.5f);
            spotLights[2].ToggleOn(true, 1.5f, 1.5f, 3.5f);
            spotLights[3].ToggleOn(true, 1.5f, 1f, 5f);

            yield return new WaitForSeconds(6f);

            LogCurrentTimecode("Main title");
            textMeshProTitle.text = mainTitleTexts.Replace("\\n", "\n");
            title.SetActive(true);
            LeanTween.value(title, UpdateMainTitleColor, Color.clear, Color.white, 2f);

            yield return new WaitForSeconds(5.5f);

            LogCurrentTimecode("Lights off one by one");
            LeanTween.value(title, UpdateMainTitleColor, Color.white, Color.clear, 2f);
            spotLights[0].ToggleOn(false, 0f, 0.2f, 1.5f);
            spotLights[1].ToggleOn(false, 0f, 0.2f, 1.75f);
            spotLights[2].ToggleOn(false, 0f, 0.2f, 2f);
            spotLights[3].ToggleOn(false, 0f, 0.2f, 2.25f);

            yield return new WaitForSeconds(3.5f);

            LogCurrentTimecode("Staring title");
            //textMeshProTitle.GetComponent<Renderer>().sharedMaterial.shader = textMeshProOverlayShader;
            UpdateMainTitleColor(Color.clear);
            starringTitle.SetActive(true);

            for (int i = 0; i < staringTitleTexts.Length; i++)
            {
                //for (int j = 0; j < spotLights.Length; j++)
                //spotLights[j].BlinkOnce();
                if (i != 0)
                    LeanTween.value(starringTitle, UpdateStarringTitleColor, Color.white, Color.clear, .5f);
                yield return new WaitForSeconds(.5f);
                textMeshProStarringTitle.text = staringTitleTexts[i].Replace("\\n", "\n");
                LeanTween.value(starringTitle, UpdateStarringTitleColor, Color.clear, Color.white, .5f);
                yield return new WaitForSeconds(2.5f);
            }

            LeanTween.value(title, UpdateStarringTitleColor, Color.white, Color.clear, 2f);

            yield return new WaitForSeconds(3f);
            //textMeshProTitle.GetComponent<Renderer>().sharedMaterial.shader = textMeshProSurfaceShader;

            LogCurrentTimecode("Title off");
            title.SetActive(false);
            starringTitle.SetActive(false);

            LogCurrentTimecode("Table change to video materia; play video");
            tableRenderer.material = videoMaterial;
            videoPlayer.Play();

            // Wait for video be done (approx 13~15s)
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
            for (int i = 0; i < spotLights.Length; i++)
            {
                spotLights[i].SetLightColor("#FEFFDD");
                spotLights[i].TargetOnPlate(i);
            }

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
            spotLights[0].BecomeGeneralMainLight(tableCookie);

            // Others turn down
            for (int i = 1; i < spotLights.Length; i++)
                spotLights[i].ToggleOn(false, 0, 2f, 0);

            //yield return new WaitForSeconds(4f);

            platesOnlySpotlight.ToggleOn(true, .5f, 2f, 1f);

            // Show name tags/titles
            LogCurrentTimecode("Show name tags");
            for (int i = 0; i < nameTags.Length; i++)
                nameTags[i].SetActive(true);
            LeanTween.value(nameTags[0], UpdateNameTagsColor, Color.clear, Color.white, 2f);

            yield return new WaitForSeconds(3f);

            // Show title & time
            LogCurrentTimecode("Show title & time");
            timeTitle.SetActive(true);
            LeanTween.value(timeTitle, Color.clear, Color.white, 2f)
                     .setOnUpdate((Color col) => {
                         textMeshProTimeTitle.color = col;
                     });

            yield return new WaitForSeconds(4f);

            LeanTween.value(timeTitle, Color.white, Color.clear, 2f)
                     .setOnUpdate((Color col) => {
                         textMeshProTimeTitle.color = col;
                     })
                     .setOnComplete(() => {
                         timeTitle.SetActive(false);
                     });

            yield return new WaitForSeconds(3f);

            LogCurrentTimecode("Spotlight on mom");
            spotLights[3].TargetOnPlateBlink(2.4f, 1f);

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
            spotLights[0].ToggleOn(true, 1.5f, 2f, 0);
            spotLights[1].ToggleOn(true, 1.5f, 1f, 2.5f);
            spotLights[2].ToggleOn(true, 1.5f, 1.5f, 3.5f);
            spotLights[3].ToggleOn(true, 1.5f, 1f, 5f);

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

        void UpdateStarringTitleColor(Color col)
        {
            textMeshProStarringTitle.color = col;
        }

        void UpdateNameTagsColor(Color col)
        {
            for (int i = 0; i < textMeshProNameTags.Length; i++)
                textMeshProNameTags[i].color = col;
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
