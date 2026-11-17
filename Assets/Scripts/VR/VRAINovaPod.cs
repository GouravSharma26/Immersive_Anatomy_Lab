using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ImmersiveAnatomy.Learning;

namespace ImmersiveAnatomy.VR
{
    public class VRAINovaPod : MonoBehaviour
    {
        public static VRAINovaPod Instance { get; private set; }

        [Header("Backend")]
        public LLMAnatomyClient llmClient;
        public AIAnatomyTutor tutor;

        [Header("Header Elements")]
        public Text connectionStatusText;
        public Text currentOrganContextText;
        public Button modeToggleBtn;
        public Text modeToggleBtnText;

        [Header("Response Elements")]
        public Text responseBodyText;
        public Text sourcesText;
        public Text mnemonicText;
        public Button listenTTSBtn;
        public Text listenTTSBtnText;

        [Header("Quick Clinical Inquiries")]
        public Button btnAngina;
        public Button btnPortal;
        public Button btnCarina;
        public Button btnBaroreceptor;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip clickClip;
        public AudioClip receiveClip;

        private bool isClinicalMode = false;
        private string currentOrganName = "Lungs & Trachea";
        private string currentSystemName = "Respiratory";
        private string latestAudioUrl = "";

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);

            if (audioSource == null) audioSource = GetComponent<AudioSource>();

            // Ensure inward-facing rotation so world canvas is never seen from behind
            if (transform.position.x > 3f)
            {
                transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            }
        }

        private void Start()
        {
            if (llmClient == null) llmClient = FindFirstObjectByType<LLMAnatomyClient>();
            if (tutor == null) tutor = FindFirstObjectByType<AIAnatomyTutor>();

            InitButtons();
            UpdateStatusUI();
            SetDefaultIntroText();
        }

        private void InitButtons()
        {
            if (modeToggleBtn != null)
            {
                modeToggleBtn.onClick.AddListener(ToggleMode);
            }

            if (listenTTSBtn != null)
            {
                listenTTSBtn.onClick.AddListener(OnListenTTSClicked);
            }

            if (btnAngina != null)
            {
                btnAngina.onClick.AddListener(() => AskQuestion(
                    "Why does myocardial ischemia radiate to the left arm and jaw?",
                    "Heart",
                    "Cardiovascular"
                ));
            }

            if (btnPortal != null)
            {
                btnPortal.onClick.AddListener(() => AskQuestion(
                    "Explain hepatic portal vein hemodynamics, sinusoidal resistance, and the pathophysiology of ascites.",
                    "Liver",
                    "Digestive"
                ));
            }

            if (btnCarina != null)
            {
                btnCarina.onClick.AddListener(() => AskQuestion(
                    "What is the sensory innervation of the tracheal carina and why does it evoke such a violent cough reflex?",
                    "Lungs",
                    "Respiratory"
                ));
            }

            if (btnBaroreceptor != null)
            {
                btnBaroreceptor.onClick.AddListener(() => AskQuestion(
                    "How do carotid sinus baroreceptors modulate sympathetic and parasympathetic outflow during acute hypotension?",
                    "Heart",
                    "Cardiovascular"
                ));
            }
        }

        public void SetOrganContext(OrganInteractable organ)
        {
            if (organ == null) return;
            currentOrganName = organ.organName;
            currentSystemName = organ.bodySystem;

            if (currentOrganContextText != null)
            {
                currentOrganContextText.text = $"Context: <color=#00FFAA>{currentOrganName}</color> ({currentSystemName})";
            }
        }

        public void ToggleMode()
        {
            isClinicalMode = !isClinicalMode;
            if (modeToggleBtnText != null)
            {
                modeToggleBtnText.text = isClinicalMode ? "Mode: Specialist / MD" : "Mode: Student";
            }
            PlaySound(clickClip, 0.8f);
        }

        private void UpdateStatusUI()
        {
            bool online = (llmClient != null && llmClient.isServerOnline);
            if (connectionStatusText != null)
            {
                connectionStatusText.text = online
                    ? "<color=#00FF88>● Live RAG & LLM Online (127.0.0.1:8000)</color>"
                    : "<color=#FFCC00>● Standalone Medical Knowledge Base Active</color>";
            }

            if (currentOrganContextText != null)
            {
                currentOrganContextText.text = $"Context: <color=#00FFAA>{currentOrganName}</color> ({currentSystemName})";
            }
        }

        private void SetDefaultIntroText()
        {
            if (responseBodyText != null)
            {
                responseBodyText.text = "<b>Welcome to Dr. Nova AI Clinical Consultation Pod.</b>\n\nClick any high-yield clinical inquiry below or grab an organ on the central dissection table to review anatomical mechanisms, vascular architecture, and surgical correlates.\n\nAll responses are grounded in <i>Guyton & Hall Textbook of Medical Physiology</i> and <i>Gray's Anatomy</i>.";
            }

            if (sourcesText != null)
            {
                sourcesText.text = "Knowledge Base: Guyton & Hall (14th Ed), Moore's Clinically Oriented Anatomy (8th Ed)";
            }

            if (mnemonicText != null)
            {
                mnemonicText.text = "Tip: Laser-click [Listen to Dr. Nova] to stream spoken medical explanations directly into your VR headset.";
            }

            if (listenTTSBtn != null) listenTTSBtn.gameObject.SetActive(false);
        }

        public void AskQuestion(string query, string organ, string system)
        {
            currentOrganName = organ;
            currentSystemName = system;
            UpdateStatusUI();
            PlaySound(clickClip, 0.8f);

            if (responseBodyText != null)
            {
                responseBodyText.text = $"<color=#00E5FF>Consulting Dr. Nova knowledge base regarding {organ}...</color>";
            }

            if (llmClient != null && llmClient.isServerOnline)
            {
                llmClient.AskAIAssistant(query, organ, system, isClinicalMode ? "clinical" : "student", (res) =>
                {
                    OnResponseReceived(res, query);
                });
            }
            else
            {
                // Fallback to high-yield local medical knowledge
                StartCoroutine(SimulateLocalConsultation(query, organ, system));
            }
        }

        private IEnumerator SimulateLocalConsultation(string query, string organ, string system)
        {
            yield return new WaitForSeconds(0.6f);
            ChatResponseData fakeRes = new ChatResponseData();

            if (organ == "Heart")
            {
                fakeRes.reply = "Coronary blood flow is unique because resting myocardial extraction of oxygen is already near-maximal (~70-80%). The Left Anterior Descending (LAD) artery courses along the anterior interventricular groove, supplying 45-55% of the left ventricle including the apex and anterior septum. When myocardial perfusion is impaired by atherothrombosis, ischemic hypoxia stimulates unmyelinated sympathetic C-fiber cardiac afferents. These visceral sensory fibers enter spinal cord levels T1 to T4/T5, synapsing on secondary neurons that also receive somatic afferents from the left arm, neck, and jaw, producing the classic phenomenon of referred anginal pain.";
                fakeRes.citations = new string[] { "Guyton and Hall Textbook of Medical Physiology (14th Ed), Ch. 21", "Moore's Clinically Oriented Anatomy (8th Ed)" };
                fakeRes.clinical_pearl = "Pathological Correlate: Dermatomal convergence at the dorsal horn of spinal cord levels T1-T4.";
                fakeRes.mnemonic = "Mnemonic: 'LAD = Life After Delivery / Widow Maker' - Supplies the anterior 2/3 of the interventricular septum.";
            }
            else if (organ == "Liver")
            {
                fakeRes.reply = "The hepatic portal vein is formed by the union of the splenic vein and superior mesenteric vein behind the neck of the pancreas. It delivers ~75% of blood flow and ~50% of oxygen to the liver under low resting pressures (5-10 mmHg). In chronic liver disease, diffuse fibrosis and regenerative nodules dramatically increase sinusoidal resistance. As portal pressure exceeds 10-12 mmHg, backpressure produces splenomegaly, esophageal varices via portosystemic shunts, and splanchnic nitric oxide release causing vasodilation and transudative ascites.";
                fakeRes.citations = new string[] { "Sleisenger and Fordtran's Gastrointestinal and Liver Disease (11th Ed)", "Guyton & Hall Ch. 71" };
                fakeRes.clinical_pearl = "Clinical Threshold: Ascites and variceal hemorrhage rarely occur until the Hepatic Venous Pressure Gradient (HVPG) exceeds 10 mmHg.";
                fakeRes.mnemonic = "Mnemonic: 'Portal Triad = Proper Hepatic Artery + Portal Vein + Common Bile Duct' enclosed in hepatoduodenal ligament.";
            }
            else
            {
                fakeRes.reply = "The tracheal carina is the cartilaginous ridge at the tracheal bifurcation at vertebral level T4/T5. It represents the most sensitive sensory area of the entire respiratory tract for evoking the cough reflex. Mechanical or chemical stimulation of carina mucosal receptors triggers sensory impulses transmitted via the vagus nerve (CN X) to the medulla oblongata, firing efferent impulses via spinal nerves (intercostal and abdominal muscles) and phrenic nerve to produce explosive expulsion of air at velocities exceeding 100 mph.";
                fakeRes.citations = new string[] { "West's Respiratory Physiology (11th Ed)", "Gray's Anatomy Ch. 54" };
                fakeRes.clinical_pearl = "Surgical Landmark: Distortion or widening of the carinal angle on bronchoscopy suggests subcarinal lymphadenopathy (metastatic spread).";
                fakeRes.mnemonic = "Key Landmark: Carina aligns with the Sternal Angle of Louis (T4-T5 disc plane).";
            }

            OnResponseReceived(fakeRes, query);
        }

        private void OnResponseReceived(ChatResponseData res, string query)
        {
            PlaySound(receiveClip, 0.9f);

            if (res == null)
            {
                if (responseBodyText != null) responseBodyText.text = "Dr. Nova consultation timed out. Please try again.";
                return;
            }

            if (responseBodyText != null)
            {
                string text = $"<b>Inquiry:</b> {query}\n\n<b>Dr. Nova:</b> {res.reply}";
                if (!string.IsNullOrEmpty(res.clinical_pearl))
                {
                    text += $"\n\n<color=#00FFAA><b>Clinical Pearl:</b> {res.clinical_pearl}</color>";
                }
                responseBodyText.text = text;
            }

            if (sourcesText != null)
            {
                sourcesText.text = (res.citations != null && res.citations.Length > 0)
                    ? $"<b>Citations:</b> {string.Join(" | ", res.citations)}"
                    : "<b>Citations:</b> Guyton & Hall Medical Physiology";
            }

            if (mnemonicText != null)
            {
                mnemonicText.text = !string.IsNullOrEmpty(res.mnemonic) ? res.mnemonic : "";
            }

            latestAudioUrl = res.audio_url;
            if (listenTTSBtn != null)
            {
                listenTTSBtn.gameObject.SetActive(true);
                if (listenTTSBtnText != null) listenTTSBtnText.text = "🔊 Listen to Spoken Explanation";
            }
        }

        private void OnListenTTSClicked()
        {
            if (llmClient != null && !string.IsNullOrEmpty(latestAudioUrl))
            {
                if (listenTTSBtnText != null) listenTTSBtnText.text = "🔊 Playing Spoken Voice...";
                llmClient.PlayLatestAudio();
            }
            else
            {
                PlaySound(receiveClip, 1f);
                if (listenTTSBtnText != null) listenTTSBtnText.text = "Voice Played";
            }
        }

        private void PlaySound(AudioClip clip, float volume = 1f)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip, volume);
            }
        }
    }
}
