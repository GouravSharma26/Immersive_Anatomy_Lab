using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ImmersiveAnatomy.Learning
{
    public enum QuizState
    {
        Idle,
        QuestionActive,
        AnswerFeedback,
        ClinicalCaseActive,
        CaseFeedback,
        QuizComplete
    }

    public class QuizEngine : MonoBehaviour
    {
        [Header("References")]
        public LearningAnalytics analytics;
        public AIAnatomyTutor tutor;
        public OrganHighlighter highlighter;

        [Header("Quiz Session Settings")]
        public float timePerQuestion = 30f;
        public bool isTimerEnabled = true;

        public QuizState State { get; private set; } = QuizState.Idle;
        public QuizQuestion CurrentQuestion { get; private set; }
        public ClinicalCase CurrentCase { get; private set; }
        public OrganInteractable CurrentTargetOrgan { get; private set; }
        public float RemainingTime { get; private set; }
        public int CurrentQuestionIndex { get; private set; } = 0;
        public int TotalQuestionsInSession => activeSessionQuestions.Count;

        // Feedback state
        public bool LastAnswerCorrect { get; private set; }
        public string FeedbackMessage { get; private set; }

        private List<QuizQuestion> questionBank = new List<QuizQuestion>();
        private List<ClinicalCase> clinicalCaseBank = new List<ClinicalCase>();
        private List<QuizQuestion> activeSessionQuestions = new List<QuizQuestion>();

        private AudioSource audioSource;

        private void Awake()
        {
            if (analytics == null) analytics = GetComponent<LearningAnalytics>();
            if (tutor == null) tutor = GetComponent<AIAnatomyTutor>();
            if (highlighter == null) highlighter = FindFirstObjectByType<OrganHighlighter>();

            // Setup audio source for procedural chimes
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            BuildQuestionBank();
            BuildClinicalCaseBank();
        }

        private void Update()
        {
            if (State == QuizState.QuestionActive && isTimerEnabled)
            {
                RemainingTime -= Time.deltaTime;
                if (RemainingTime <= 0f)
                {
                    RemainingTime = 0f;
                    OnTimeExpired();
                }
            }
        }

        private void BuildQuestionBank()
        {
            // --- CARDIOVASCULAR ---
            questionBank.Add(new QuizQuestion
            {
                id = "CARD-01",
                questionText = "Look at the highlighted organ in the thoracic cavity. What is the anatomical name of this organ?",
                bodySystem = "Cardiovascular",
                difficulty = QuizDifficulty.Beginner,
                questionType = QuestionType.MultipleChoice,
                options = new string[] { "Heart", "Lungs & Trachea", "Liver", "Spleen" },
                correctOptionIndex = 0,
                targetOrganName = "Heart",
                clinicalExplanation = "The human heart lies in the middle mediastinum, with its apex tilted anteroinferiorly toward the left 5th intercostal space midclavicular line (PMI).",
                hint = "It is the muscular central vascular pump situated between the lungs.",
                points = 100
            });

            questionBank.Add(new QuizQuestion
            {
                id = "CARD-02",
                questionText = "Which coronary artery is colloquially referred to as the 'widow maker' due to supplying the anterior interventricular septum and apex?",
                bodySystem = "Cardiovascular",
                difficulty = QuizDifficulty.Intermediate,
                questionType = QuestionType.MultipleChoice,
                options = new string[] { "Left Anterior Descending (LAD)", "Right Coronary Artery (RCA)", "Left Circumflex Artery (LCx)", "Posterior Descending Artery (PDA)" },
                correctOptionIndex = 0,
                targetOrganName = "Heart",
                clinicalExplanation = "Occlusion of the Left Anterior Descending (LAD) branch of the left coronary artery causes massive anterior wall myocardial infarction and carries high mortality.",
                hint = "Think about the primary artery traveling down the anterior interventricular sulcus.",
                points = 120
            });

            // --- RESPIRATORY ---
            questionBank.Add(new QuizQuestion
            {
                id = "RESP-01",
                questionText = "Look at the highlighted organ featuring cartilaginous C-rings and asymmetric lobes. What is its anatomical name?",
                bodySystem = "Respiratory",
                difficulty = QuizDifficulty.Beginner,
                questionType = QuestionType.MultipleChoice,
                options = new string[] { "Lungs & Trachea", "Esophagus", "Diaphragm", "Thyroid Gland" },
                correctOptionIndex = 0,
                targetOrganName = "Lungs",
                clinicalExplanation = "The trachea is braced by 16-20 hyaline cartilaginous C-rings to prevent collapse during negative intrapleural pressures, bifurcating into the right (3 lobes) and left (2 lobes) lungs.",
                hint = "The primary gas-exchange organs flanking the heart.",
                points = 100
            });

            questionBank.Add(new QuizQuestion
            {
                id = "RESP-02",
                questionText = "At which anatomical vertebral plane does the trachea bifurcate into the left and right mainstem bronchi (the carina)?",
                bodySystem = "Respiratory",
                difficulty = QuizDifficulty.Intermediate,
                questionType = QuestionType.MultipleChoice,
                options = new string[] { "T4/T5 (Sternal Angle of Louis)", "C6 (Cricoid cartilage level)", "T1/T2 (Jugular notch)", "T8 (Caval opening)" },
                correctOptionIndex = 0,
                targetOrganName = "Lungs",
                clinicalExplanation = "The sternal angle of Louis marks the T4/T5 intervertebral disc plane, where the tracheal carina, aortic arch, and azygos vein arch are clinically located.",
                hint = "This landmark aligns with the junction of the sternal manubrium and body.",
                points = 120
            });

            // --- NERVOUS ---
            questionBank.Add(new QuizQuestion
            {
                id = "NERV-01",
                questionText = "Look at the highlighted cephalic organ displaying cerebral gyri, sulci, and the cerebellum. What is its name?",
                bodySystem = "Nervous",
                difficulty = QuizDifficulty.Beginner,
                questionType = QuestionType.MultipleChoice,
                options = new string[] { "Brain (Cerebrum & Cerebellum)", "Spinal Cord", "Calvaria", "Pituitary Gland" },
                correctOptionIndex = 0,
                targetOrganName = "Brain",
                clinicalExplanation = "The human brain contains ~86 billion neurons, with deeply folded cerebral sulci and gyri dramatically increasing cortical surface area.",
                hint = "The primary central nervous system organ resting inside the calvaria.",
                points = 100
            });

            questionBank.Add(new QuizQuestion
            {
                id = "NERV-02",
                questionText = "A patient loses the ability to speak fluently (expressive aphasia) but can comprehend language. Which cerebral cortical area is compromised?",
                bodySystem = "Nervous",
                difficulty = QuizDifficulty.Advanced,
                questionType = QuestionType.MultipleChoice,
                options = new string[] { "Broca's Area (Inferior Frontal Gyrus)", "Wernicke's Area (Superior Temporal Gyrus)", "Primary Somatosensory Cortex", "Prefrontal Association Cortex" },
                correctOptionIndex = 0,
                targetOrganName = "Brain",
                clinicalExplanation = "Broca's motor speech area (Brodmann areas 44/45) resides in the posterior inferior frontal gyrus of the dominant hemisphere. Damage produces expressive, non-fluent aphasia.",
                hint = "Remember: Broca's broken speech; Wernicke's word salad.",
                points = 150
            });

            // --- DIGESTIVE ---
            questionBank.Add(new QuizQuestion
            {
                id = "DIG-01",
                questionText = "Look at the large highlighted visceral gland in the right upper quadrant. What is its anatomical name?",
                bodySystem = "Digestive",
                difficulty = QuizDifficulty.Beginner,
                questionType = QuestionType.MultipleChoice,
                options = new string[] { "Liver", "Gallbladder", "Pancreas", "Duodenum" },
                correctOptionIndex = 0,
                targetOrganName = "Liver",
                clinicalExplanation = "The liver occupies the right upper quadrant, performing gluconeogenesis, albumin synthesis, drug detoxification via Cytochrome P450, and bile production.",
                hint = "The reddish-brown organ situated inferior to the right hemidiaphragm.",
                points = 100
            });

            questionBank.Add(new QuizQuestion
            {
                id = "DIG-02",
                questionText = "Which gastric cells synthesize hydrochloric acid (HCl) and Intrinsic Factor necessary for vitamin B12 absorption?",
                bodySystem = "Digestive",
                difficulty = QuizDifficulty.Intermediate,
                questionType = QuestionType.MultipleChoice,
                options = new string[] { "Parietal (Oxyntic) Cells", "Chief (Zymogenic) Cells", "G Cells (Gastrin secreting)", "Enterochromaffin-like (ECL) Cells" },
                correctOptionIndex = 0,
                targetOrganName = "Stomach",
                clinicalExplanation = "Gastric parietal cells use H+/K+ ATPase pumps to secrete concentrated HCl and synthesize Intrinsic Factor; autoimmune destruction results in pernicious anemia.",
                hint = "These cells are targeted pharmacologically by Proton Pump Inhibitors (PPIs).",
                points = 120
            });

            // --- SKELETAL ---
            questionBank.Add(new QuizQuestion
            {
                id = "SKEL-01",
                questionText = "Look at the highlighted osseous cage protecting thoracic viscera. What is this anatomical system?",
                bodySystem = "Skeletal",
                difficulty = QuizDifficulty.Beginner,
                questionType = QuestionType.MultipleChoice,
                options = new string[] { "Skeletal Ribcage & Spine", "Pectoral Girdle", "Appendicular Skeleton", "Muscular Wall" },
                correctOptionIndex = 0,
                targetOrganName = "Skeletal",
                clinicalExplanation = "The 12 pairs of ribs articulate posteriorly with thoracic vertebrae T1-T12 and anteriorly via costal cartilage to the sternum.",
                hint = "The ribcage and vertebral column structure.",
                points = 100
            });
        }

        private void BuildClinicalCaseBank()
        {
            clinicalCaseBank.Add(new ClinicalCase
            {
                caseId = "CASE-101",
                patientProfile = "62-year-old male with type 2 diabetes and dyslipidemia.",
                chiefComplaint = "Sudden onset of crushing retrosternal chest pressure lasting 45 minutes, with diaphoresis and radiating aching into the left shoulder and jaw.",
                vitalSigns = "BP 158/92, HR 108 regular, RR 22, SpO2 96% on room air.",
                promptQuestion = "Examine the 3D anatomy and select the primary ischemic organ requiring immediate catheterization.",
                targetOrganName = "Heart",
                differentialOptions = new string[] { "Acute ST-Elevation Myocardial Infarction", "Acute Pulmonary Embolism", "Boerhaave's Esophageal Rupture", "Acute Cholecystitis" },
                correctDifferentialIndex = 0,
                pathophysiologicalMechanism = "Rupture of an atherosclerotic plaque with overlying thrombotic occlusion of a coronary artery, depriving myocardium of oxygen.",
                primaryTreatmentProtocol = "Emergent coronary angiography with Percutaneous Coronary Intervention (PCI) and dual antiplatelet therapy (DAPT)."
            });

            clinicalCaseBank.Add(new ClinicalCase
            {
                caseId = "CASE-102",
                patientProfile = "24-year-old tall, slender male presenting after sudden breathlessness.",
                chiefComplaint = "Sharp, unilateral pleuritic chest pain and acute shortness of breath during light basketball practice. Absence of breath sounds on the right side.",
                vitalSigns = "BP 118/76, HR 114, RR 28, SpO2 88%. Trachea slightly deviated to the left.",
                promptQuestion = "Identify the compromised organ system and confirm the emergency diagnosis.",
                targetOrganName = "Lungs",
                differentialOptions = new string[] { "Primary Spontaneous Pneumothorax", "Myocarditis", "Diaphragmatic Hernia", "Aortic Dissection" },
                correctDifferentialIndex = 0,
                pathophysiologicalMechanism = "Rupture of an apical subpleural bleb allowing atmospheric air into the negative intrapleural space, causing complete right lung atelectasis.",
                primaryTreatmentProtocol = "Immediate needle thoracostomy at the 2nd intercostal space midclavicular line, followed by tube thoracostomy (chest tube)."
            });

            clinicalCaseBank.Add(new ClinicalCase
            {
                caseId = "CASE-103",
                patientProfile = "58-year-old female with long-standing viral hepatitis C.",
                chiefComplaint = "Progressive abdominal distension, scleral icterus (yellow eyes), confusion (asterixis flap), and spider angiomas on torso.",
                vitalSigns = "BP 102/64, HR 88, Albumin 2.1 g/dL, Total Bilirubin 4.8 mg/dL, INR 2.3.",
                promptQuestion = "Select the failing visceral organ and determine the primary etiology.",
                targetOrganName = "Liver",
                differentialOptions = new string[] { "Decompensated End-Stage Hepatic Cirrhosis", "Acute Tubular Necrosis", "Congestive Heart Failure", "Acute Pancreatitis" },
                correctDifferentialIndex = 0,
                pathophysiologicalMechanism = "Diffuse hepatic parenchymal fibrosis and regenerative nodules obstructing portal flow, leading to portal hypertension and hepatic encephalopathy.",
                primaryTreatmentProtocol = "Sodium restriction, spironolactone/furosemide for ascites, lactulose for hyperammonemia, and evaluation for liver transplantation."
            });
        }

        public void StartQuizSession(int questionCount = 5)
        {
            activeSessionQuestions.Clear();
            List<QuizQuestion> pool = new List<QuizQuestion>(questionBank);
            // Shuffle
            for (int i = 0; i < pool.Count; i++)
            {
                int r = UnityEngine.Random.Range(i, pool.Count);
                var temp = pool[i];
                pool[i] = pool[r];
                pool[r] = temp;
            }

            int count = Mathf.Min(questionCount, pool.Count);
            for (int i = 0; i < count; i++) activeSessionQuestions.Add(pool[i]);

            CurrentQuestionIndex = 0;
            LoadQuestion(activeSessionQuestions[0]);
        }

        public void StartClinicalCase(int caseIndex = 0)
        {
            if (clinicalCaseBank.Count == 0) return;
            caseIndex = Mathf.Clamp(caseIndex, 0, clinicalCaseBank.Count - 1);
            CurrentCase = clinicalCaseBank[caseIndex];
            State = QuizState.ClinicalCaseActive;
        }

        private void LoadQuestion(QuizQuestion question)
        {
            CurrentQuestion = question;
            RemainingTime = timePerQuestion;
            State = QuizState.QuestionActive;
            FeedbackMessage = "";
            LastAnswerCorrect = false;

            if (highlighter != null) highlighter.ClearSelection();

            // Spotlight the target organ on the 3D model
            if (!string.IsNullOrEmpty(question.targetOrganName))
            {
                CurrentTargetOrgan = FindOrganByName(question.targetOrganName);
                if (CurrentTargetOrgan != null)
                {
                    if (BodySystemManager.Instance != null)
                    {
                        BodySystemManager.Instance.SetSystemVisibility("Body", true);
                        BodySystemManager.Instance.SetSystemVisibility(CurrentTargetOrgan.bodySystem, true);
                    }
                    if (highlighter != null)
                    {
                        highlighter.SelectOrgan(CurrentTargetOrgan);
                    }
                }
            }
        }

        public void SubmitMultipleChoiceAnswer(int optionIndex)
        {
            if (State != QuizState.QuestionActive || CurrentQuestion == null) return;

            bool isCorrect = (optionIndex == CurrentQuestion.correctOptionIndex);
            ProcessAnswer(isCorrect);
        }

        public void Submit3DOrganSelection(OrganInteractable clickedOrgan)
        {
            if (State == QuizState.QuestionActive && CurrentQuestion != null)
            {
                if (clickedOrgan == null) return;
                bool isCorrect = clickedOrgan.organName.IndexOf(CurrentQuestion.targetOrganName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 clickedOrgan.bodySystem.IndexOf(CurrentQuestion.targetOrganName, StringComparison.OrdinalIgnoreCase) >= 0;
                ProcessAnswer(isCorrect);
            }
            else if (State == QuizState.ClinicalCaseActive && CurrentCase != null)
            {
                if (clickedOrgan == null) return;
                bool isCorrect = clickedOrgan.organName.IndexOf(CurrentCase.targetOrganName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 clickedOrgan.bodySystem.IndexOf(CurrentCase.targetOrganName, StringComparison.OrdinalIgnoreCase) >= 0;
                
                LastAnswerCorrect = isCorrect;
                FeedbackMessage = isCorrect
                    ? $"<b>CORRECT CLINICAL TARGET:</b> Identified {clickedOrgan.organName} as the primary failing structure.\n{CurrentCase.pathophysiologicalMechanism}"
                    : $"<b>INCORRECT TARGET:</b> You selected {clickedOrgan.organName}. The primary affected organ is the {CurrentCase.targetOrganName}.";

                if (isCorrect)
                {
                    PlayAudioTone(880f, 0.25f);
                    if (analytics != null) analytics.RecordClinicalCaseCompleted();
                }
                else
                {
                    PlayAudioTone(220f, 0.35f);
                }

                State = QuizState.CaseFeedback;
            }
        }

        private void ProcessAnswer(bool isCorrect)
        {
            LastAnswerCorrect = isCorrect;
            int points = 0;

            if (isCorrect)
            {
                int streak = analytics != null ? analytics.currentStreak + 1 : 1;
                float multiplier = 1f + (Mathf.Min(streak - 1, 4) * 0.25f);
                float timeBonus = isTimerEnabled ? (RemainingTime / timePerQuestion) * 30f : 0f;
                points = Mathf.RoundToInt((CurrentQuestion.points + timeBonus) * multiplier);

                FeedbackMessage = $"<b>CORRECT! (+{points} pts)</b>\n{CurrentQuestion.clinicalExplanation}";
                PlayAudioTone(660f, 0.2f);
            }
            else
            {
                FeedbackMessage = $"<b>INCORRECT.</b>\n{CurrentQuestion.clinicalExplanation}";
                PlayAudioTone(260f, 0.3f);
            }

            if (analytics != null)
            {
                analytics.RecordAnswer(CurrentQuestion.bodySystem, isCorrect, points);
            }

            State = QuizState.AnswerFeedback;
        }

        private void OnTimeExpired()
        {
            ProcessAnswer(false);
            FeedbackMessage = "<b>TIME EXPIRED!</b>\n" + CurrentQuestion.clinicalExplanation;
        }

        public void NextQuestion()
        {
            CurrentQuestionIndex++;
            if (CurrentQuestionIndex < activeSessionQuestions.Count)
            {
                LoadQuestion(activeSessionQuestions[CurrentQuestionIndex]);
            }
            else
            {
                State = QuizState.QuizComplete;
            }
        }

        public void ExitQuiz()
        {
            State = QuizState.Idle;
            CurrentQuestion = null;
            CurrentCase = null;
            CurrentTargetOrgan = null;
            if (highlighter != null) highlighter.ClearSelection();
        }

        public OrganInteractable FindOrganByName(string organOrSystem)
        {
            if (string.IsNullOrEmpty(organOrSystem)) return null;
            OrganInteractable[] all = UnityEngine.Object.FindObjectsByType<OrganInteractable>(FindObjectsSortMode.None);
            foreach (var o in all)
            {
                if (o.organName.IndexOf(organOrSystem, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    organOrSystem.IndexOf(o.organName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return o;
                }
            }
            foreach (var o in all)
            {
                if (o.bodySystem.IndexOf(organOrSystem, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return o;
                }
            }
            return null;
        }

        // Procedural Audio Synthesizer: plays a pleasant sine chord tone without requiring external audio clips
        private void PlayAudioTone(float frequency, float duration)
        {
            if (audioSource == null) return;
            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Sin(Mathf.PI * (t / duration)); // Smooth bell envelope
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * envelope * 0.4f;
            }
            AudioClip clip = AudioClip.Create("ProceduralTone", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            audioSource.PlayOneShot(clip);
        }
    }
}
