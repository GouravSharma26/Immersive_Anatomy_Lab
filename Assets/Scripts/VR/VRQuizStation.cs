using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ImmersiveAnatomy.Learning;

namespace ImmersiveAnatomy.VR
{
    public class VRQuizStation : MonoBehaviour
    {
        public static VRQuizStation Instance { get; private set; }

        public enum StationTab
        {
            Quiz,
            Cases
        }

        [Header("State")]
        public StationTab activeTab = StationTab.Quiz;
        public QuizEngine quizEngine;

        [Header("Tabs Navigation")]
        public Button quizTabBtn;
        public Button casesTabBtn;
        public GameObject quizContainer;
        public GameObject casesContainer;

        [Header("Header Stats")]
        public Text statsScoreText;
        public Text statsStreakText;
        public Text statsDifficultyText;

        [Header("Quiz UI Elements")]
        public Text quizSystemText;
        public Text quizQuestionText;
        public Text quizHintText;
        public Button[] optionButtons; // 4 buttons
        public Text[] optionTexts;
        public Text feedbackText;
        public Button nextQuestionBtn;

        [Header("Clinical Case UI Elements")]
        public Text caseTitleText;
        public Text casePatientProfileText;
        public Text caseComplaintText;
        public Text caseVitalsText;
        public Text casePromptText;
        public Button[] differentialButtons; // 4 buttons
        public Text[] differentialTexts;
        public Text caseDebriefText;
        public Button nextCaseBtn;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip correctClip;
        public AudioClip wrongClip;
        public AudioClip nextClip;

        private int currentCaseIndex = 0;
        private List<ClinicalCase> casesList = new List<ClinicalCase>();
        private bool isQuestionAnswered = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);

            if (audioSource == null) audioSource = GetComponent<AudioSource>();

            // Ensure inward-facing rotation so world canvas is never seen from behind
            if (transform.position.x < -3f)
            {
                transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            }
        }

        private void Start()
        {
            if (quizEngine == null) quizEngine = FindFirstObjectByType<QuizEngine>();

            InitTabButtons();
            InitCasesBank();
            SetTab(StationTab.Quiz);

            if (quizEngine != null)
            {
                quizEngine.StartQuizSession(10);
                DisplayCurrentQuizQuestion();
            }
        }

        private void InitTabButtons()
        {
            if (quizTabBtn != null) quizTabBtn.onClick.AddListener(() => SetTab(StationTab.Quiz));
            if (casesTabBtn != null) casesTabBtn.onClick.AddListener(() => SetTab(StationTab.Cases));

            if (nextQuestionBtn != null)
            {
                nextQuestionBtn.onClick.AddListener(OnNextQuestionClicked);
            }

            if (nextCaseBtn != null)
            {
                nextCaseBtn.onClick.AddListener(OnNextCaseClicked);
            }

            // Bind 4 option buttons
            for (int i = 0; i < optionButtons.Length; i++)
            {
                int index = i;
                if (optionButtons[i] != null)
                {
                    optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
                }
            }

            // Bind 4 differential buttons
            for (int i = 0; i < differentialButtons.Length; i++)
            {
                int index = i;
                if (differentialButtons[i] != null)
                {
                    differentialButtons[i].onClick.AddListener(() => OnDifferentialSelected(index));
                }
            }
        }

        public void SetTab(StationTab tab)
        {
            activeTab = tab;
            if (quizContainer != null) quizContainer.SetActive(tab == StationTab.Quiz);
            if (casesContainer != null) casesContainer.SetActive(tab == StationTab.Cases);

            if (tab == StationTab.Quiz)
            {
                DisplayCurrentQuizQuestion();
            }
            else
            {
                DisplayCurrentClinicalCase();
            }
            UpdateHeaderStats();
        }

        private void UpdateHeaderStats()
        {
            int score = (quizEngine != null && quizEngine.analytics != null) ? quizEngine.analytics.data.totalScore : 0;
            int streak = (quizEngine != null && quizEngine.analytics != null) ? quizEngine.analytics.currentStreak : 0;

            if (statsScoreText != null) statsScoreText.text = $"Score: <color=#00FFAA>{score}</color> pts";
            if (statsStreakText != null) statsStreakText.text = $"Streak: <color=#FFDD44>{streak}x</color>";
            if (statsDifficultyText != null) statsDifficultyText.text = "Level: <color=#33CCFF>Intermediate</color>";
        }

        public void DisplayCurrentQuizQuestion()
        {
            isQuestionAnswered = false;
            if (nextQuestionBtn != null) nextQuestionBtn.gameObject.SetActive(false);
            if (feedbackText != null) feedbackText.text = "";

            // Reset button colors
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] != null)
                {
                    optionButtons[i].interactable = true;
                    var colors = optionButtons[i].colors;
                    colors.normalColor = new Color(0.12f, 0.22f, 0.35f, 1f);
                    optionButtons[i].colors = colors;
                }
            }

            if (quizEngine == null || quizEngine.CurrentQuestion == null)
            {
                if (quizQuestionText != null) quizQuestionText.text = "All quiz questions completed! Great work.";
                return;
            }

            QuizQuestion q = quizEngine.CurrentQuestion;
            if (quizSystemText != null) quizSystemText.text = $"System: <color=#00E5FF>{q.bodySystem}</color>  |  Difficulty: <color=#FFCC00>{q.difficulty}</color>  |  +{q.points} pts";
            if (quizQuestionText != null) quizQuestionText.text = q.questionText;
            if (quizHintText != null) quizHintText.text = $"<b>Clinical Clue:</b> {q.hint}";

            if (q.questionType == QuestionType.MultipleChoice && q.options != null && q.options.Length > 0)
            {
                for (int i = 0; i < optionButtons.Length; i++)
                {
                    if (i < q.options.Length)
                    {
                        optionButtons[i].gameObject.SetActive(true);
                        if (optionTexts[i] != null) optionTexts[i].text = $"<b>{(char)('A' + i)}.</b> {q.options[i]}";
                    }
                    else
                    {
                        optionButtons[i].gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                // 3D identification question
                for (int i = 0; i < optionButtons.Length; i++)
                {
                    optionButtons[i].gameObject.SetActive(false);
                }
                if (feedbackText != null)
                {
                    feedbackText.text = "<color=#FFCC00>⚡ 3D IDENTIFICATION CHALLENGE:</color> Walk up to the central dissection table and grab or laser-click the target organ!";
                }
            }

            UpdateHeaderStats();
        }

        private void OnOptionSelected(int index)
        {
            if (isQuestionAnswered || quizEngine == null || quizEngine.CurrentQuestion == null) return;
            isQuestionAnswered = true;

            QuizQuestion q = quizEngine.CurrentQuestion;
            bool isCorrect = (index == q.correctOptionIndex);

            // Audio & Visual feedback
            if (isCorrect)
            {
                PlaySound(correctClip, 1f);
                SetButtonColor(optionButtons[index], new Color(0.1f, 0.65f, 0.25f, 1f));
                if (feedbackText != null)
                {
                    feedbackText.text = $"<color=#00FF88><b>✓ CORRECT! (+{q.points} pts)</b></color>\n{q.clinicalExplanation}";
                }
                quizEngine.SubmitMultipleChoiceAnswer(index);
            }
            else
            {
                PlaySound(wrongClip, 1f);
                SetButtonColor(optionButtons[index], new Color(0.75f, 0.15f, 0.15f, 1f));
                if (q.correctOptionIndex < optionButtons.Length)
                {
                    SetButtonColor(optionButtons[q.correctOptionIndex], new Color(0.1f, 0.65f, 0.25f, 1f));
                }
                if (feedbackText != null)
                {
                    feedbackText.text = $"<color=#FF4444><b>✗ INCORRECT</b></color>\n{q.clinicalExplanation}";
                }
                quizEngine.SubmitMultipleChoiceAnswer(index);
            }

            // Disable further clicks
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] != null) optionButtons[i].interactable = false;
            }

            if (nextQuestionBtn != null) nextQuestionBtn.gameObject.SetActive(true);
            UpdateHeaderStats();
        }

        public void OnOrganInteractedInLab(OrganInteractable organ)
        {
            if (activeTab != StationTab.Quiz || isQuestionAnswered || quizEngine == null || quizEngine.CurrentQuestion == null) return;
            QuizQuestion q = quizEngine.CurrentQuestion;
            if (q.questionType == QuestionType.Identify3DOrgan && organ != null)
            {
                bool isCorrect = (organ.organName.IndexOf(q.targetOrganName, System.StringComparison.OrdinalIgnoreCase) >= 0);
                isQuestionAnswered = true;

                if (isCorrect)
                {
                    PlaySound(correctClip, 1f);
                    if (feedbackText != null)
                    {
                        feedbackText.text = $"<color=#00FF88><b>✓ CORRECT 3D IDENTIFICATION! (+{q.points} pts)</b></color>\n{q.clinicalExplanation}";
                    }
                    quizEngine.Submit3DOrganSelection(organ);
                }
                else
                {
                    PlaySound(wrongClip, 1f);
                    if (feedbackText != null)
                    {
                        feedbackText.text = $"<color=#FF4444><b>✗ INCORRECT ORGAN</b> Selected: {organ.organName}.</color>\nTarget was: {q.targetOrganName}.\n{q.clinicalExplanation}";
                    }
                    quizEngine.Submit3DOrganSelection(organ);
                }

                if (nextQuestionBtn != null) nextQuestionBtn.gameObject.SetActive(true);
                UpdateHeaderStats();
            }
        }

        private void OnNextQuestionClicked()
        {
            PlaySound(nextClip, 0.8f);
            if (quizEngine != null)
            {
                quizEngine.NextQuestion();
                DisplayCurrentQuizQuestion();
            }
        }

        // --- Clinical Cases ---
        private void InitCasesBank()
        {
            casesList.Clear();
            casesList.Add(new ClinicalCase
            {
                caseId = "CASE-101",
                patientProfile = "62-year-old male with type 2 diabetes and dyslipidemia.",
                chiefComplaint = "Sudden onset of crushing retrosternal chest pressure lasting 45 minutes, with diaphoresis and radiating aching into the left shoulder and jaw.",
                vitalSigns = "BP 158/92, HR 108 regular, RR 22, SpO2 96% on room air.",
                promptQuestion = "Evaluate the patient presentation and confirm the emergency diagnosis:",
                targetOrganName = "Heart",
                differentialOptions = new string[] { "Acute ST-Elevation Myocardial Infarction", "Acute Pulmonary Embolism", "Boerhaave's Esophageal Rupture", "Acute Cholecystitis" },
                correctDifferentialIndex = 0,
                pathophysiologicalMechanism = "Rupture of an unstable coronary atherosclerotic plaque leading to acute thrombotic occlusion, acute transmural myocardial ischemia, and arm/jaw referred pain via T1-T4 visceral afferents.",
                primaryTreatmentProtocol = "Immediate catheterization laboratory activation for emergent Percutaneous Coronary Intervention (PCI), Aspirin 325mg, and Heparin anticoagulation."
            });

            casesList.Add(new ClinicalCase
            {
                caseId = "CASE-102",
                patientProfile = "24-year-old tall, slender male presenting after sudden breathlessness.",
                chiefComplaint = "Sharp, unilateral pleuritic chest pain and acute shortness of breath during light basketball practice. Absence of breath sounds on the right side.",
                vitalSigns = "BP 118/76, HR 114, RR 28, SpO2 88%. Trachea slightly deviated to the left.",
                promptQuestion = "Identify the emergency thoracic condition:",
                targetOrganName = "Lungs",
                differentialOptions = new string[] { "Primary Spontaneous Tension Pneumothorax", "Acute Viral Myocarditis", "Congenital Diaphragmatic Hernia", "Stanford Type A Aortic Dissection" },
                correctDifferentialIndex = 0,
                pathophysiologicalMechanism = "Rupture of an apical subpleural bleb allowing atmospheric air to enter the pleural cavity, collapsing the right lung and causing mediastinal shift.",
                primaryTreatmentProtocol = "Immediate needle decompression at 2nd intercostal space midclavicular line, followed by tube thoracostomy (chest tube)."
            });

            casesList.Add(new ClinicalCase
            {
                caseId = "CASE-103",
                patientProfile = "58-year-old female with long-standing viral hepatitis C.",
                chiefComplaint = "Progressive abdominal distension, scleral icterus (yellow eyes), confusion (asterixis flapping tremor), and caput medusae venous distension.",
                vitalSigns = "BP 102/64, HR 88, Albumin 2.1 g/dL, Total Bilirubin 4.8 mg/dL, INR 2.3.",
                promptQuestion = "Determine the primary visceral etiology:",
                targetOrganName = "Liver",
                differentialOptions = new string[] { "Decompensated End-Stage Hepatic Cirrhosis", "Acute Tubular Necrosis", "Right-Sided Congestive Heart Failure", "Acute Necrotizing Pancreatitis" },
                correctDifferentialIndex = 0,
                pathophysiologicalMechanism = "Parenchymal bridging fibrosis and regenerative nodules compressing sinusoids, creating sinusoidal hypertension (portal pressure > 10 mmHg), splanchnic vasodilation, and ascites.",
                primaryTreatmentProtocol = "Sodium restriction, spironolactone/furosemide for ascites, lactulose for hyperammonemia, and evaluation for orthotopic liver transplantation."
            });
        }

        public void DisplayCurrentClinicalCase()
        {
            if (casesList.Count == 0) return;
            if (currentCaseIndex >= casesList.Count) currentCaseIndex = 0;

            ClinicalCase c = casesList[currentCaseIndex];
            if (caseTitleText != null) caseTitleText.text = $"Case {currentCaseIndex + 1} of {casesList.Count}: <color=#00E5FF>{c.caseId}</color>";
            if (casePatientProfileText != null) casePatientProfileText.text = $"<b>Patient Profile:</b> {c.patientProfile}";
            if (caseComplaintText != null) caseComplaintText.text = $"<b>Chief Complaint:</b> {c.chiefComplaint}";
            if (caseVitalsText != null) caseVitalsText.text = $"<b>Vital Signs:</b> <color=#FFDD44>{c.vitalSigns}</color>";
            if (casePromptText != null) casePromptText.text = c.promptQuestion;
            if (caseDebriefText != null) caseDebriefText.text = "";
            if (nextCaseBtn != null) nextCaseBtn.gameObject.SetActive(false);

            for (int i = 0; i < differentialButtons.Length; i++)
            {
                if (i < c.differentialOptions.Length)
                {
                    differentialButtons[i].gameObject.SetActive(true);
                    differentialButtons[i].interactable = true;
                    var colors = differentialButtons[i].colors;
                    colors.normalColor = new Color(0.12f, 0.22f, 0.35f, 1f);
                    differentialButtons[i].colors = colors;

                    if (differentialTexts[i] != null)
                    {
                        differentialTexts[i].text = $"<b>{(char)('A' + i)}.</b> {c.differentialOptions[i]}";
                    }
                }
                else
                {
                    differentialButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnDifferentialSelected(int index)
        {
            ClinicalCase c = casesList[currentCaseIndex];
            bool isCorrect = (index == c.correctDifferentialIndex);

            if (isCorrect)
            {
                PlaySound(correctClip, 1f);
                SetButtonColor(differentialButtons[index], new Color(0.1f, 0.65f, 0.25f, 1f));
                if (caseDebriefText != null)
                {
                    caseDebriefText.text = $"<color=#00FF88><b>✓ CORRECT CLINICAL DIAGNOSIS!</b></color>\n<b>Pathophysiology:</b> {c.pathophysiologicalMechanism}\n<b>Treatment Protocol:</b> {c.primaryTreatmentProtocol}";
                }
            }
            else
            {
                PlaySound(wrongClip, 1f);
                SetButtonColor(differentialButtons[index], new Color(0.75f, 0.15f, 0.15f, 1f));
                if (c.correctDifferentialIndex < differentialButtons.Length)
                {
                    SetButtonColor(differentialButtons[c.correctDifferentialIndex], new Color(0.1f, 0.65f, 0.25f, 1f));
                }
                if (caseDebriefText != null)
                {
                    caseDebriefText.text = $"<color=#FF4444><b>✗ INCORRECT DIAGNOSIS</b></color>\n<b>Pathophysiology:</b> {c.pathophysiologicalMechanism}\n<b>Treatment Protocol:</b> {c.primaryTreatmentProtocol}";
                }
            }

            for (int i = 0; i < differentialButtons.Length; i++)
            {
                if (differentialButtons[i] != null) differentialButtons[i].interactable = false;
            }

            if (nextCaseBtn != null) nextCaseBtn.gameObject.SetActive(true);
        }

        private void OnNextCaseClicked()
        {
            currentCaseIndex = (currentCaseIndex + 1) % casesList.Count;
            PlaySound(nextClip, 0.8f);
            DisplayCurrentClinicalCase();
        }

        private void SetButtonColor(Button btn, Color color)
        {
            if (btn == null) return;
            var c = btn.colors;
            c.normalColor = color;
            c.highlightedColor = color;
            c.selectedColor = color;
            btn.colors = c;
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
