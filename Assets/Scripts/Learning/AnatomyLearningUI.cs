using System;
using UnityEngine;

namespace ImmersiveAnatomy.Learning
{
    public enum LearningTab
    {
        Quiz,
        ClinicalCases,
        AITutor,
        Analytics
    }

    public class AnatomyLearningUI : MonoBehaviour
    {
        [Header("References")]
        public QuizEngine quizEngine;
        public AIAnatomyTutor aiTutor;
        public LearningAnalytics analytics;
        public LLMAnatomyClient llmClient;

        [Header("UI State")]
        public bool isLearningUIOpen = false;
        public LearningTab activeTab = LearningTab.Quiz;

        private OrganInteractable lastSelectedOrgan;
        private Vector2 scrollPos;
        private Texture2D bgTexture;
        private string customQuestionText = "Why does myocardial ischemia radiate to the left arm?";
        private string aiChatResponseText = "";
        private string[] aiCitations = new string[0];
        private string aiPearl = "";

        private Texture2D GetBgTexture()
        {
            if (bgTexture == null)
            {
                bgTexture = new Texture2D(1, 1);
                bgTexture.SetPixel(0, 0, new Color(0.08f, 0.12f, 0.18f, 0.95f));
                bgTexture.Apply();
            }
            return bgTexture;
        }

        private void Start()
        {
            if (quizEngine == null) quizEngine = GetComponent<QuizEngine>();
            if (aiTutor == null) aiTutor = GetComponent<AIAnatomyTutor>();
            if (analytics == null) analytics = GetComponent<LearningAnalytics>();
            if (llmClient == null) llmClient = GetComponent<LLMAnatomyClient>();
            if (llmClient == null) llmClient = FindFirstObjectByType<LLMAnatomyClient>();
        }

        private void Update()
        {
            // Hotkey Q or L to toggle learning interface
            if (Input.GetKeyDown(KeyCode.L))
            {
                ToggleLearningUI();
            }
        }

        public void ToggleLearningUI()
        {
            isLearningUIOpen = !isLearningUIOpen;
            if (isLearningUIOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                if (quizEngine != null && quizEngine.State == QuizState.Idle)
                {
                    quizEngine.StartQuizSession(5);
                }
            }
        }

        public void NotifyOrganSelected(OrganInteractable organ)
        {
            lastSelectedOrgan = organ;
            if (quizEngine != null && (quizEngine.State == QuizState.QuestionActive || quizEngine.State == QuizState.ClinicalCaseActive))
            {
                quizEngine.Submit3DOrganSelection(organ);
            }
        }

        private void OnGUI()
        {
            // Modal display is disabled in favor of the clean docked left area in AnatomyInteraction
            // so the 3D body mannequin remains completely unobstructed in the center.
            return;
        }

        public void RenderQuizTab(Rect r)
        {
            if (quizEngine == null) return;

            // Session Progress Bar & Score
            int total = quizEngine.TotalQuestionsInSession;
            int current = quizEngine.CurrentQuestionIndex + 1;
            float progress = total > 0 ? ((float)current / total) * 100f : 0f;

            int score = analytics != null ? analytics.currentSessionScore : 0;
            int streak = analytics != null ? analytics.currentStreak : 0;

            GUI.Label(new Rect(r.x, r.y, 250, 22), $"<b>QUESTION {current} OF {total}</b> | <b>PROGRESS:</b> {progress:F0}%");
            GUI.Label(new Rect(r.x + r.width - 200, r.y, 200, 22), $"<b>SCORE:</b> {score}  |  <b>STREAK:</b> {streak}x");

            // Timer bar
            if (quizEngine.isTimerEnabled && quizEngine.State == QuizState.QuestionActive)
            {
                float timePct = quizEngine.RemainingTime / quizEngine.timePerQuestion;
                GUI.Box(new Rect(r.x, r.y + 24, r.width, 6), "");
                GUI.Box(new Rect(r.x, r.y + 24, r.width * timePct, 6), "");
            }

            if (quizEngine.State == QuizState.QuestionActive && quizEngine.CurrentQuestion != null)
            {
                var q = quizEngine.CurrentQuestion;

                // Category & Difficulty Badge
                GUI.Label(new Rect(r.x, r.y + 36, r.width, 22), $"<color=#00E5FF><b>[{q.bodySystem.ToUpper()}]</b></color>  Difficulty: <b>{q.difficulty}</b>  |  Points: <b>+{q.points}</b>");

                // Question Prompt Card
                GUI.Box(new Rect(r.x, r.y + 60, r.width, 70), "");
                GUI.Label(new Rect(r.x + 10, r.y + 65, r.width - 20, 60), $"<b>{q.questionText}</b>");

                if (q.questionType == QuestionType.Identify3DOrgan)
                {
                    // 3D Organ Click Instruction
                    GUI.Box(new Rect(r.x, r.y + 140, r.width, 100), "");
                    GUI.Label(new Rect(r.x + 15, r.y + 155, r.width - 30, 60),
                        "<b>ACTION REQUIRED:</b>\n" +
                        "Aim your crosshair at the 3D anatomical model in the lab and <b>Left-Click</b> directly on the target organ to submit your answer!");

                    // Hint button
                    if (GUI.Button(new Rect(r.x, r.y + 250, 160, 28), "Request AI Hint"))
                    {
                        if (aiTutor != null)
                        {
                            quizEngine.SendMessage("LogHint", aiTutor.GetQuizHint(q), SendMessageOptions.DontRequireReceiver);
                        }
                    }
                }
                else if (q.questionType == QuestionType.MultipleChoice)
                {
                    // 4 Multiple Choice Options
                    for (int i = 0; i < q.options.Length; i++)
                    {
                        char optLetter = (char)('A' + i);
                        string btnText = $"<b>[{optLetter}]</b>  {q.options[i]}";
                        if (GUI.Button(new Rect(r.x, r.y + 140 + i * 36, r.width, 30), btnText))
                        {
                            quizEngine.SubmitMultipleChoiceAnswer(i);
                        }
                    }

                    // Hint button
                    if (GUI.Button(new Rect(r.x, r.y + 295, 160, 26), "Request AI Hint"))
                    {
                        if (aiTutor != null)
                        {
                            quizEngine.SendMessage("LogHint", aiTutor.GetQuizHint(q), SendMessageOptions.DontRequireReceiver);
                        }
                    }
                }
            }
            else if (quizEngine.State == QuizState.AnswerFeedback)
            {
                // Answer Review Feedback Card
                string color = quizEngine.LastAnswerCorrect ? "#00FF88" : "#FF4444";
                GUI.Box(new Rect(r.x, r.y + 40, r.width, 220), "");
                GUI.Label(new Rect(r.x + 15, r.y + 50, r.width - 30, 200), $"<color={color}>{quizEngine.FeedbackMessage}</color>");

                if (GUI.Button(new Rect(r.x, r.y + 270, r.width, 34), "<b>CONTINUE TO NEXT QUESTION -></b>"))
                {
                    quizEngine.NextQuestion();
                }
            }
            else if (quizEngine.State == QuizState.QuizComplete)
            {
                // Summary Card
                GUI.Box(new Rect(r.x, r.y + 40, r.width, 220), "");
                GUI.Label(new Rect(r.x + 15, r.y + 55, r.width - 30, 40), "<size=16><b>ANATOMY QUIZ SESSION COMPLETE!</b></size>");
                GUI.Label(new Rect(r.x + 15, r.y + 100, r.width - 30, 30), $"Final Session Score: <b>+{score} points</b>");
                GUI.Label(new Rect(r.x + 15, r.y + 130, r.width - 30, 30), $"Highest Streak: <b>{streak} in a row</b>");
                GUI.Label(new Rect(r.x + 15, r.y + 160, r.width - 30, 40), "Detailed system mastery and badges have been updated in your Analytics profile.");

                if (GUI.Button(new Rect(r.x, r.y + 270, r.width, 34), "<b>START NEW PRACTICE QUIZ</b>"))
                {
                    quizEngine.StartQuizSession(5);
                }
            }
        }

        public void RenderClinicalCasesTab(Rect r)
        {
            if (quizEngine == null) return;

            GUI.Label(new Rect(r.x, r.y, 350, 22), "<b>PATIENT CLINICAL CASE SIMULATION</b>");

            // Case Selector buttons
            if (GUI.Button(new Rect(r.x, r.y + 26, 150, 24), "Case 1: Chest Pain")) quizEngine.StartClinicalCase(0);
            if (GUI.Button(new Rect(r.x + 160, r.y + 26, 150, 24), "Case 2: Breathlessness")) quizEngine.StartClinicalCase(1);
            if (GUI.Button(new Rect(r.x + 320, r.y + 26, 150, 24), "Case 3: Jaundice")) quizEngine.StartClinicalCase(2);

            var c = quizEngine.CurrentCase;
            if (c == null) return;

            // Patient Card
            GUI.Box(new Rect(r.x, r.y + 58, r.width, 240), "");
            GUI.Label(new Rect(r.x + 12, r.y + 65, r.width - 24, 22), $"<b>Patient ID:</b> {c.caseId}  |  {c.patientProfile}");
            GUI.Label(new Rect(r.x + 12, r.y + 90, r.width - 24, 20), $"<b>Vitals:</b> <color=#00E5FF>{c.vitalSigns}</color>");
            GUI.Label(new Rect(r.x + 12, r.y + 115, r.width - 24, 50), $"<b>Chief Complaint:</b>\n{c.chiefComplaint}");
            GUI.Label(new Rect(r.x + 12, r.y + 170, r.width - 24, 40), $"<b>Clinical Prompt:</b>\n<color=#FFCC00>{c.promptQuestion}</color>");

            if (quizEngine.State == QuizState.ClinicalCaseActive)
            {
                GUI.Label(new Rect(r.x + 12, r.y + 215, r.width - 24, 30), "<i>Aim and click the affected organ in the 3D model to confirm diagnosis.</i>");
            }
            else if (quizEngine.State == QuizState.CaseFeedback)
            {
                GUI.Label(new Rect(r.x + 12, r.y + 215, r.width - 24, 75), $"<color=#00FF88>{quizEngine.FeedbackMessage}</color>");
                
                if (GUI.Button(new Rect(r.x, r.y + 310, r.width, 30), "Review Full Pathophysiology Debrief"))
                {
                    if (aiTutor != null)
                    {
                        activeTab = LearningTab.AITutor;
                    }
                }
            }
        }

        public void RenderAITutorTab(Rect r)
        {
            if (aiTutor == null) return;

            string backendStatus = (llmClient != null && llmClient.isServerOnline)
                ? "<color=#00FF88>● Live RAG & LLM Online</color>"
                : "<color=#FFCC00>● Standalone Clinical Corpus</color>";

            GUI.Label(new Rect(r.x, r.y, 215, 22), $"<b>{aiTutor.tutorName}</b>");
            GUI.Label(new Rect(r.x + 220, r.y, 155, 22), backendStatus);

            string lvlText = aiTutor.pedagogicalLevel == TutorPedagogicalLevel.SimplifiedStudent
                ? "Level: <color=#00FF88>Student</color>"
                : "Level: <color=#00E5FF>Clinical</color>";

            if (GUI.Button(new Rect(r.x + r.width - 130, r.y, 130, 24), lvlText))
            {
                aiTutor.TogglePedagogicalLevel();
            }

            string organName = lastSelectedOrgan != null ? lastSelectedOrgan.organName : "Heart";
            string bodySystem = lastSelectedOrgan != null ? lastSelectedOrgan.bodySystem : "Cardiovascular";

            // AI Explanation / Response Box
            string displayText = !string.IsNullOrEmpty(aiChatResponseText)
                ? aiChatResponseText
                : aiTutor.GetExplanation(organName);

            GUI.Box(new Rect(r.x, r.y + 28, r.width, 225), "");
            GUI.Label(new Rect(r.x + 10, r.y + 32, r.width - 20, 215), displayText);

            // Citations and Voice row
            int rowY = (int)r.y + 258;
            if (aiCitations.Length > 0)
            {
                string citeStr = string.Join(" | ", aiCitations);
                GUI.Label(new Rect(r.x, rowY, r.width - 120, 36), $"<size=9><b>Sources:</b> {citeStr}</size>");
            }
            else
            {
                GUI.Label(new Rect(r.x, rowY, r.width - 120, 36), "<size=9><b>Sources:</b> Gray's Anatomy 4th Ed & Guyton & Hall Medical Physiology 14th Ed</size>");
            }

            // Audio / TTS Listen Button
            if (GUI.Button(new Rect(r.x + r.width - 110, rowY, 110, 28), "🔊 Listen [TTS]"))
            {
                if (llmClient != null)
                {
                    llmClient.PlayLatestAudio();
                }
            }

            // Quick Question Suggestions
            int qY = rowY + 32;
            GUI.Label(new Rect(r.x, qY, r.width, 18), "<size=10><b>Quick Clinical Inquiries:</b></size>");
            if (GUI.Button(new Rect(r.x, qY + 18, 160, 22), "Referred Angina Pain"))
            {
                customQuestionText = "Why does myocardial ischemia radiate to the left arm and jaw?";
                SubmitChatQuestion(organName, bodySystem);
            }
            if (GUI.Button(new Rect(r.x + 168, qY + 18, 165, 22), "Portal Vein Hemodynamics"))
            {
                customQuestionText = "Explain the dual blood supply to the liver and portal hypertension.";
                SubmitChatQuestion(organName, bodySystem);
            }
            if (GUI.Button(new Rect(r.x + 340, qY + 18, 160, 22), "Carina & Cough Reflex"))
            {
                customQuestionText = "What is the clinical significance of the tracheal carina?";
                SubmitChatQuestion(organName, bodySystem);
            }

            // Custom Question Input Field & Submit
            int inputY = qY + 44;
            customQuestionText = GUI.TextField(new Rect(r.x, inputY, r.width - 100, 24), customQuestionText);
            
            string sendBtnLabel = (llmClient != null && llmClient.isLoading) ? "Thinking..." : "Ask AI";
            if (GUI.Button(new Rect(r.x + r.width - 95, inputY, 95, 24), sendBtnLabel))
            {
                SubmitChatQuestion(organName, bodySystem);
            }

            // Disclaimer Footer
            GUI.Label(new Rect(r.x, inputY + 28, r.width, 24), "<size=8><color=#888888>DISCLAIMER: Educational medical simulation only. Not intended for clinical diagnosis or medical management.</color></size>");
        }

        private void SubmitChatQuestion(string organName, string bodySystem)
        {
            if (string.IsNullOrWhiteSpace(customQuestionText)) return;
            string lvl = aiTutor != null ? aiTutor.pedagogicalLevel.ToString() : "Student";

            if (llmClient != null && llmClient.isServerOnline)
            {
                llmClient.AskAIAssistant(customQuestionText, organName, bodySystem, lvl, (res) =>
                {
                    if (res != null)
                    {
                        aiChatResponseText = $"<b>[Dr. Nova - Live AI Response]</b>\n{res.reply}\n\n<b>Clinical Pearl:</b> <i>{res.clinical_pearl}</i>";
                        aiCitations = res.citations != null ? res.citations : new string[0];
                        aiPearl = res.clinical_pearl;
                    }
                });
            }
            else
            {
                // Standalone fallback
                aiChatResponseText = $"<b>[Dr. Nova]</b> Regarding {organName}: " +
                    "Referred anginal pain occurs due to dermatomal convergence of visceral sympathetic afferents (T1-T4) with somatic fibers in the spinal dorsal horn.\n\n" +
                    "<b>Reference:</b> Guyton and Hall Textbook of Medical Physiology (14th Ed), Chapter 21.";
                aiCitations = new string[] { "Guyton and Hall Medical Physiology (14th Ed)" };
            }
        }

        private void RenderAnalyticsTab(Rect r)
        {
            if (analytics == null) return;

            var d = analytics.data;
            GUI.Label(new Rect(r.x, r.y, 250, 22), "<b>LEARNING MASTERY & PERFORMANCE</b>");

            GUI.Box(new Rect(r.x, r.y + 26, r.width, 70), "");
            GUI.Label(new Rect(r.x + 12, r.y + 32, 220, 20), $"Total Questions: <b>{d.totalAttempted}</b>");
            GUI.Label(new Rect(r.x + 12, r.y + 52, 220, 20), $"Overall Accuracy: <b>{analytics.OverallAccuracy:F1}%</b>");
            GUI.Label(new Rect(r.x + 240, r.y + 32, 220, 20), $"Total Score: <b>{d.totalScore} pts</b>");
            GUI.Label(new Rect(r.x + 240, r.y + 52, 220, 20), $"Clinical Cases Solved: <b>{d.clinicalCasesCompleted}</b>");

            // System Mastery Bars
            GUI.Label(new Rect(r.x, r.y + 102, r.width, 20), "<b>Anatomical System Mastery:</b>");
            string[] sys = new string[] { "Cardiovascular", "Respiratory", "Nervous", "Digestive", "Skeletal" };
            for (int i = 0; i < sys.Length; i++)
            {
                float acc = analytics.GetSystemAccuracy(sys[i]);
                GUI.Label(new Rect(r.x, r.y + 125 + i * 26, 130, 20), $"{sys[i]}:");
                GUI.Box(new Rect(r.x + 130, r.y + 128 + i * 26, 250, 14), "");
                GUI.Box(new Rect(r.x + 130, r.y + 128 + i * 26, 250 * (acc / 100f), 14), "");
                GUI.Label(new Rect(r.x + 390, r.y + 125 + i * 26, 80, 20), $"{acc:F0}%");
            }

            // Badges
            GUI.Label(new Rect(r.x, r.y + 265, r.width, 20), "<b>Unlocked Clinical Badges:</b>");
            string badges = d.unlockedBadges.Count > 0 ? string.Join(" | ", d.unlockedBadges) : "<i>Complete quizzes to earn clinical badges.</i>";
            GUI.Box(new Rect(r.x, r.y + 288, r.width, 50), "");
            GUI.Label(new Rect(r.x + 10, r.y + 293, r.width - 20, 40), badges);
        }
    }
}
