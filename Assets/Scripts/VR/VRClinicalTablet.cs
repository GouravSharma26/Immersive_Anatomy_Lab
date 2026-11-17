using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ImmersiveAnatomy.Learning;

namespace ImmersiveAnatomy.VR
{
    public class VRClinicalTablet : MonoBehaviour
    {
        public static VRClinicalTablet Instance { get; private set; }

        public enum TabletTab
        {
            AITutor,
            OrganMetrics,
            Quiz,
            Cases,
            LabControls
        }

        [Header("State")]
        public TabletTab currentTab = TabletTab.OrganMetrics;
        public OrganInteractable currentOrgan;
        public VRGrabbableOrgan currentGrabbable;

        [Header("UI Panels")]
        public GameObject aiTutorPanel;
        public GameObject organMetricsPanel;
        public GameObject quizPanel;
        public GameObject casesPanel;
        public GameObject labControlsPanel;

        [Header("Organ Metrics Fields")]
        public Text organNameText;
        public Text organSystemText;
        public Text organMassText;
        public Text organPhysiologyText;
        public Text organVascularText;
        public Text organInnervationText;
        public Text organPathologyText;
        public Button returnOrganButton;

        [Header("AI Tutor Fields")]
        public Text tutorStatusText;
        public Text tutorResponseText;
        public Text tutorSourcesText;
        public Button askAnginaButton;
        public Button askPortalButton;
        public Button askCarinaButton;
        public Button listenTTSButton;
        public AudioSource ttsAudioSource;

        [Header("Quiz Fields")]
        public Text quizQuestionText;
        public Text quizPromptText;
        public Text quizScoreText;
        public Button[] quizOptionButtons;
        public Text[] quizOptionTexts;

        [Header("Lab Control Fields")]
        public Button explodeButton;
        public Button xrayButton;
        public Button vascularButton;
        public Button thoracicButton;
        public Button normalModeButton;
        public Button resetAllButton;
        public Text xrayButtonText;
        public Text vascularButtonText;
        public Text thoracicButtonText;
        public Button[] systemToggleButtons;
        public Text[] systemToggleTexts;

        [Header("Audio")]
        public AudioSource tabletAudio;
        public AudioClip tabSwitchSound;
        public AudioClip actionSound;
        public AudioClip correctSound;
        public AudioClip wrongSound;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != null && Instance != this) Destroy(gameObject);
        }

        private void Start()
        {
            InitButtonListeners();
            SetTab(TabletTab.OrganMetrics);
            UpdateMetricsUI();

            XRayViewController xray = FindFirstObjectByType<XRayViewController>();
            if (xray != null)
            {
                xray.OnModeChanged += SyncModeStates;
                SyncModeStates(xray.CurrentMode);
            }
        }

        private void OnDestroy()
        {
            XRayViewController xray = FindFirstObjectByType<XRayViewController>();
            if (xray != null)
            {
                xray.OnModeChanged -= SyncModeStates;
            }
        }

        private void InitButtonListeners()
        {
            if (returnOrganButton != null)
            {
                returnOrganButton.onClick.AddListener(() =>
                {
                    if (currentGrabbable != null)
                    {
                        currentGrabbable.SmoothReturnToSocket();
                        PlaySound(actionSound);
                    }
                });
            }

            if (askAnginaButton != null)
            {
                askAnginaButton.onClick.AddListener(() => AskDrNova("Why does myocardial ischemia radiate to the left arm?"));
            }

            if (askPortalButton != null)
            {
                askPortalButton.onClick.AddListener(() => AskDrNova("Explain the dual blood supply of the human liver."));
            }

            if (askCarinaButton != null)
            {
                askCarinaButton.onClick.AddListener(() => AskDrNova("What is the clinical significance of the tracheal carina?"));
            }

            if (listenTTSButton != null)
            {
                listenTTSButton.onClick.AddListener(PlayLastTTS);
            }

            if (resetAllButton != null)
            {
                resetAllButton.onClick.AddListener(ResetAllAnatomy);
            }

            if (explodeButton != null)
            {
                explodeButton.onClick.AddListener(ToggleExplodedView);
            }

            if (xrayButton != null)
            {
                xrayButton.onClick.AddListener(ToggleXRayView);
            }

            if (vascularButton != null)
            {
                vascularButton.onClick.AddListener(ToggleVascularView);
            }

            if (thoracicButton != null)
            {
                thoracicButton.onClick.AddListener(ToggleThoracicView);
            }

            if (normalModeButton != null)
            {
                normalModeButton.onClick.AddListener(SetNormalView);
            }
        }

        public void SetTab(TabletTab tab)
        {
            currentTab = tab;
            if (aiTutorPanel != null) aiTutorPanel.SetActive(tab == TabletTab.AITutor);
            if (organMetricsPanel != null) organMetricsPanel.SetActive(tab == TabletTab.OrganMetrics);
            if (quizPanel != null) quizPanel.SetActive(tab == TabletTab.Quiz);
            if (casesPanel != null) casesPanel.SetActive(tab == TabletTab.Cases);
            if (labControlsPanel != null) labControlsPanel.SetActive(tab == TabletTab.LabControls);

            PlaySound(tabSwitchSound);
        }

        public void OnOrganGrabbed(OrganInteractable organ, VRGrabbableOrgan grabbable)
        {
            currentOrgan = organ;
            currentGrabbable = grabbable;

            UpdateMetricsUI();

            // Auto-switch to metrics if not in active quiz
            if (currentTab != TabletTab.Quiz)
            {
                SetTab(TabletTab.OrganMetrics);
            }
        }

        public void OnOrganSnapped(OrganInteractable organ, VRGrabbableOrgan grabbable)
        {
            if (currentGrabbable == grabbable)
            {
                UpdateMetricsUI();
            }
        }

        public void UpdateMetricsUI()
        {
            if (currentOrgan != null)
            {
                if (organNameText != null) organNameText.text = currentOrgan.organName;
                if (organSystemText != null) organSystemText.text = $"System: {currentOrgan.bodySystem}";
                if (organMassText != null) organMassText.text = $"Normal Mass: {currentOrgan.normalWeight:F0} g";
                if (organPhysiologyText != null) organPhysiologyText.text = $"Physiology: {currentOrgan.clinicalFunction}";
                if (organVascularText != null) organVascularText.text = $"Vascular: {currentOrgan.arterialSupply}";
                if (organInnervationText != null) organInnervationText.text = $"Innervation: {currentOrgan.innervation}";
                if (organPathologyText != null) organPathologyText.text = $"Pathology: {currentOrgan.commonPathology}";
                if (returnOrganButton != null) returnOrganButton.interactable = true;
            }
            else
            {
                if (organNameText != null) organNameText.text = "Human Organ Specimen";
                if (organSystemText != null) organSystemText.text = "System: Grasp any organ with controller to inspect";
                if (organMassText != null) organMassText.text = "Mass: --";
                if (organPhysiologyText != null) organPhysiologyText.text = "Point controller ray or reach with VR hand to grab heart, lungs, brain, or liver.";
                if (organVascularText != null) organVascularText.text = "Vascular: --";
                if (organInnervationText != null) organInnervationText.text = "Innervation: --";
                if (organPathologyText != null) organPathologyText.text = "Pathology: --";
                if (returnOrganButton != null) returnOrganButton.interactable = false;
            }
        }

        public void AskDrNova(string question)
        {
            SetTab(TabletTab.AITutor);
            if (tutorStatusText != null) tutorStatusText.text = "Dr. Nova (Consulting RAG Medical Knowledge Base...)";
            if (tutorResponseText != null) tutorResponseText.text = "Retrieving peer-reviewed citations and formulating clinical response...";
            if (tutorSourcesText != null) tutorSourcesText.text = "";

            PlaySound(actionSound);

            LLMAnatomyClient client = FindFirstObjectByType<LLMAnatomyClient>();
            if (client != null)
            {
                string organContext = currentOrgan != null ? currentOrgan.organName : "General Human Anatomy";
                string systemContext = currentOrgan != null ? currentOrgan.bodySystem : "Cardiovascular";
                client.AskAIAssistant(question, organContext, systemContext, "Student", OnDrNovaAnswerReceived);
            }
            else
            {
                // Fallback direct clinical response if LLM client is initializing
                StartCoroutine(FallbackAIResponse(question));
            }
        }

        private void OnDrNovaAnswerReceived(ChatResponseData response)
        {
            if (response != null && !string.IsNullOrEmpty(response.reply))
            {
                if (tutorStatusText != null) tutorStatusText.text = "Dr. Nova (AI Clinical Assistant - Live RAG)";
                if (tutorResponseText != null) tutorResponseText.text = response.reply;
                if (tutorSourcesText != null) tutorSourcesText.text = response.citations != null && response.citations.Length > 0 ? "Sources: " + string.Join("; ", response.citations) : "";

                // Stream TTS if audio available
                if (!string.IsNullOrEmpty(response.audio_url))
                {
                    LLMAnatomyClient client = FindFirstObjectByType<LLMAnatomyClient>();
                    if (client != null) client.PlayLatestAudio();
                }
            }
            else
            {
                if (tutorStatusText != null) tutorStatusText.text = "Dr. Nova (Offline Clinical Knowledge)";
                if (tutorResponseText != null) tutorResponseText.text = "Clinical reference: Coronary artery perfusion supplies myocardium. Referred pain travels via T1-T5 spinal sympathetics.";
            }
        }

        private IEnumerator FallbackAIResponse(string question)
        {
            yield return new WaitForSeconds(0.4f);
            if (tutorStatusText != null) tutorStatusText.text = "Dr. Nova (AI Clinical Specialist)";
            if (tutorResponseText != null) tutorResponseText.text = "Clinical Analysis: Visceral afferent fibers from cardiac ischemia travel retrograde through cardiac nerves into spinal cord levels T1-T5. Somatic sensory fibers from the medial left arm enter these same segments, producing referred chest/arm pain.";
            if (tutorSourcesText != null) tutorSourcesText.text = "Sources: Guyton & Hall Textbook of Medical Physiology (14th Ed), Moore's Clinically Oriented Anatomy (8th Ed)";
        }

        public void PlayLastTTS()
        {
            LLMAnatomyClient client = FindFirstObjectByType<LLMAnatomyClient>();
            if (client != null)
            {
                client.PlayLatestAudio();
            }
            PlaySound(actionSound);
        }

        public void ResetAllAnatomy()
        {
            VRGrabbableOrgan[] allOrgans = FindObjectsByType<VRGrabbableOrgan>(FindObjectsSortMode.None);
            foreach (var organ in allOrgans)
            {
                organ.SmoothReturnToSocket();
            }
            ExplodedViewController exploded = FindFirstObjectByType<ExplodedViewController>();
            if (exploded != null && exploded.IsExploded) exploded.ToggleExplodedView();

            XRayViewController xray = FindFirstObjectByType<XRayViewController>();
            if (xray != null)
            {
                xray.ResetToNormal();
                SyncModeStates(XRayViewController.MedicalVisualizationMode.Normal);
            }

            PlaySound(actionSound);
        }

        public void ToggleExplodedView()
        {
            ExplodedViewController exploded = FindFirstObjectByType<ExplodedViewController>();
            if (exploded != null)
            {
                exploded.ToggleExplodedView();
                PlaySound(actionSound);
            }
        }

        public void ToggleXRayView()
        {
            XRayViewController xray = FindFirstObjectByType<XRayViewController>();
            if (xray != null)
            {
                xray.ToggleXRay();
                SyncModeStates(xray.CurrentMode);
                PlaySound(actionSound);
            }
        }

        public void ToggleVascularView()
        {
            XRayViewController xray = FindFirstObjectByType<XRayViewController>();
            if (xray != null)
            {
                xray.ToggleVascularMode();
                SyncModeStates(xray.CurrentMode);
                PlaySound(actionSound);
            }
        }

        public void ToggleThoracicView()
        {
            XRayViewController xray = FindFirstObjectByType<XRayViewController>();
            if (xray != null)
            {
                xray.ToggleThoracicMode();
                SyncModeStates(xray.CurrentMode);
                PlaySound(actionSound);
            }
        }

        public void SetNormalView()
        {
            XRayViewController xray = FindFirstObjectByType<XRayViewController>();
            if (xray != null)
            {
                xray.ResetToNormal();
                SyncModeStates(XRayViewController.MedicalVisualizationMode.Normal);
                PlaySound(actionSound);
            }
        }

        public void SyncModeStates(XRayViewController.MedicalVisualizationMode mode)
        {
            if (xrayButtonText != null)
            {
                xrayButtonText.text = (mode == XRayViewController.MedicalVisualizationMode.XRay) ? "Normal View" : "X-Ray Mode";
            }
            if (vascularButtonText != null)
            {
                vascularButtonText.text = (mode == XRayViewController.MedicalVisualizationMode.Vascular) ? "Exit Vascular" : "Vascular Angiogram";
            }
            if (thoracicButtonText != null)
            {
                thoracicButtonText.text = (mode == XRayViewController.MedicalVisualizationMode.ThoracicTranslucent) ? "Exit Thoracic" : "Thoracic Silhouette";
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (tabletAudio != null && clip != null)
            {
                tabletAudio.PlayOneShot(clip);
            }
        }
    }
}
