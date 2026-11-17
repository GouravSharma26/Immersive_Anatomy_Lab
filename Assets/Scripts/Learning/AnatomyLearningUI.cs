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

            // Header Exit Button (Always accessible)
            if (GUI.Button(new Rect(r.x + r.width - 85, r.y, 85, 22), "<b>✕ EXIT</b>"))
            {
                quizEngine.ExitQuiz();
                AnatomyInteraction ai = FindFirstObjectByType<AnatomyInteraction>();
                if (ai != null) ai.currentArea = AnatomyInteraction.LabArea.Exploration;
                return;
            }

            int score = analytics != null ? analytics.currentSessionScore : 0;
            int streak = analytics != null ? analytics.currentStreak : 0;

            if (quizEngine.State == QuizState.Idle)
            {
                // QUIZ LANDING / START SCREEN
                GUI.Label(new Rect(r.x, r.y, r.width - 90, 22), "<size=13><b>ANATOMICAL ASSESSMENT</b></size>");

                GUI.Box(new Rect(r.x, r.y + 28, r.width, 155), "");
                string intro = "<b>Welcome to the Anatomy Quiz Station!</b>\n\n" +
                               "• In each round, a specific organ will be <b>highlighted in 3D</b> directly on the body mannequin.\n\n" +
                               "• Answer by selecting one of the <b>4 options</b> or by clicking the glowing organ in the 3D lab!\n\n" +
                               "• Test your anatomical recognition, blood supply, and clinical pearls.";
                GUI.Label(new Rect(r.x + 10, r.y + 34, r.width - 20, 145), intro);

                GUI.Label(new Rect(r.x, r.y + 192, r.width, 20), $"Current Score: <b>{score} pts</b>  |  Best Streak: <b>{streak}x</b>");

                if (GUI.Button(new Rect(r.x, r.y + 220, r.width, 42), "<b>▶ START 5-QUESTION QUIZ</b>"))
                {
                    quizEngine.StartQuizSession(5);
                }

                if (GUI.Button(new Rect(r.x, r.y + 270, r.width, 28), "Return to Anatomy Exploration"))
                {
                    quizEngine.ExitQuiz();
                    AnatomyInteraction ai = FindFirstObjectByType<AnatomyInteraction>();
                    if (ai != null) ai.currentArea = AnatomyInteraction.LabArea.Exploration;
                }
                return;
            }

            // Session Progress Bar & Score
            int total = quizEngine.TotalQuestionsInSession;
            int current = quizEngine.CurrentQuestionIndex + 1;
            float progress = total > 0 ? ((float)current / total) * 100f : 0f;

            GUI.Label(new Rect(r.x, r.y, r.width - 90, 20), $"<b>Q {current}/{total}</b> ({progress:F0}%) | <b>{score}pts</b> | <b>{streak}x</b>");

            // Timer bar
            if (quizEngine.isTimerEnabled && quizEngine.State == QuizState.QuestionActive)
            {
                float timePct = quizEngine.RemainingTime / quizEngine.timePerQuestion;
                GUI.Box(new Rect(r.x, r.y + 22, r.width, 5), "");
                GUI.Box(new Rect(r.x, r.y + 22, r.width * timePct, 5), "");
            }

            if (quizEngine.State == QuizState.QuestionActive && quizEngine.CurrentQuestion != null)
            {
                var q = quizEngine.CurrentQuestion;

                // Category & Difficulty Badge
                GUI.Label(new Rect(r.x, r.y + 30, r.width, 20), $"<color=#00E5FF><b>[{q.bodySystem.ToUpper()}]</b></color> {q.difficulty} | <b>+{q.points}pts</b>");

                // 3D Spotlight Notice if target organ is set
                if (quizEngine.CurrentTargetOrgan != null)
                {
                    GUI.Label(new Rect(r.x, r.y + 50, r.width, 18), "<color=#FFCC00><b>★ 3D Spotlight Active:</b> Check illuminated organ</color>");
                }

                // Question Prompt Card
                GUI.Box(new Rect(r.x, r.y + 70, r.width, 65), "");
                GUI.Label(new Rect(r.x + 8, r.y + 74, r.width - 16, 58), $"<b>{q.questionText}</b>");

                // 4 Multiple Choice Options
                if (q.options != null && q.options.Length > 0)
                {
                    for (int i = 0; i < q.options.Length; i++)
                    {
                        char optLetter = (char)('A' + i);
                        string btnText = $"<b>[{optLetter}]</b>  {q.options[i]}";
                        if (GUI.Button(new Rect(r.x, r.y + 142 + i * 34, r.width, 30), btnText))
                        {
                            quizEngine.SubmitMultipleChoiceAnswer(i);
                        }
                    }
                }

                // Direct Click / Hint Row
                int hintY = (int)r.y + 284;
                GUI.Label(new Rect(r.x, hintY, r.width - 130, 26), "<i>Or click highlighted 3D organ directly!</i>");
                if (GUI.Button(new Rect(r.x + r.width - 125, hintY, 125, 24), "Request AI Hint"))
                {
                    if (aiTutor != null)
                    {
                        quizEngine.SendMessage("LogHint", aiTutor.GetQuizHint(q), SendMessageOptions.DontRequireReceiver);
                    }
                }
            }
            else if (quizEngine.State == QuizState.AnswerFeedback)
            {
                // Answer Review Feedback Card
                string color = quizEngine.LastAnswerCorrect ? "#00FF88" : "#FF4444";
                GUI.Box(new Rect(r.x, r.y + 35, r.width, 220), "");
                GUI.Label(new Rect(r.x + 12, r.y + 42, r.width - 24, 205), $"<color={color}>{quizEngine.FeedbackMessage}</color>");

                if (GUI.Button(new Rect(r.x, r.y + 265, r.width, 34), "<b>CONTINUE TO NEXT QUESTION ➔</b>"))
                {
                    quizEngine.NextQuestion();
                }

                if (GUI.Button(new Rect(r.x, r.y + 305, r.width, 26), "Exit Quiz"))
                {
                    quizEngine.ExitQuiz();
                    AnatomyInteraction ai = FindFirstObjectByType<AnatomyInteraction>();
                    if (ai != null) ai.currentArea = AnatomyInteraction.LabArea.Exploration;
                }
            }
            else if (quizEngine.State == QuizState.QuizComplete)
            {
                // Summary Card
                GUI.Box(new Rect(r.x, r.y + 35, r.width, 220), "");
                GUI.Label(new Rect(r.x + 12, r.y + 45, r.width - 24, 30), "<size=15><b>ANATOMY QUIZ COMPLETE!</b></size>");
                GUI.Label(new Rect(r.x + 12, r.y + 80, r.width - 24, 25), $"Final Session Score: <b>+{score} points</b>");
                GUI.Label(new Rect(r.x + 12, r.y + 110, r.width - 24, 25), $"Highest Streak: <b>{streak} in a row</b>");
                GUI.Label(new Rect(r.x + 12, r.y + 140, r.width - 24, 45), "Detailed system mastery and badges have been updated in your profile.");

                if (GUI.Button(new Rect(r.x, r.y + 265, r.width, 34), "<b>START NEW PRACTICE QUIZ</b>"))
                {
                    quizEngine.StartQuizSession(5);
                }

                if (GUI.Button(new Rect(r.x, r.y + 305, r.width, 26), "Exit to Lab Exploration"))
                {
                    quizEngine.ExitQuiz();
                    AnatomyInteraction ai = FindFirstObjectByType<AnatomyInteraction>();
                    if (ai != null) ai.currentArea = AnatomyInteraction.LabArea.Exploration;
                }
            }
        }

        public void RenderClinicalCasesTab(Rect r)
        {
            if (quizEngine == null) return;

            // Header Exit Button (Always accessible)
            if (GUI.Button(new Rect(r.x + r.width - 85, r.y, 85, 22), "<b>✕ EXIT</b>"))
            {
                quizEngine.ExitQuiz();
                AnatomyInteraction ai = FindFirstObjectByType<AnatomyInteraction>();
                if (ai != null) ai.currentArea = AnatomyInteraction.LabArea.Exploration;
                return;
            }

            GUI.Label(new Rect(r.x, r.y, r.width - 90, 22), "<b>PATIENT CASE SIMULATION</b>");

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

            // Header Exit Button (Always accessible)
            if (GUI.Button(new Rect(r.x + r.width - 85, r.y, 85, 22), "<b>✕ EXIT</b>"))
            {
                AnatomyInteraction ai = FindFirstObjectByType<AnatomyInteraction>();
                if (ai != null) ai.currentArea = AnatomyInteraction.LabArea.Exploration;
                return;
            }

            string backendStatus = (llmClient != null && llmClient.isServerOnline)
                ? "<color=#00FF88>● Live AI Online</color>"
                : "<color=#FFCC00>● Standalone Corpus</color>";

            GUI.Label(new Rect(r.x, r.y, 140, 20), $"<b>{aiTutor.tutorName}</b>");
            GUI.Label(new Rect(r.x + 145, r.y, 105, 20), backendStatus);

            // Sub-line: Pedagogical Level Toggle
            string lvlText = aiTutor.pedagogicalLevel == TutorPedagogicalLevel.SimplifiedStudent
                ? "Mode: <color=#00FF88>Student (Core)</color>"
                : "Mode: <color=#00E5FF>Clinical (Advanced)</color>";

            if (GUI.Button(new Rect(r.x, r.y + 24, r.width, 22), lvlText))
            {
                aiTutor.TogglePedagogicalLevel();
            }

            string organName = lastSelectedOrgan != null ? lastSelectedOrgan.organName : "Heart";
            string bodySystem = lastSelectedOrgan != null ? lastSelectedOrgan.bodySystem : "Cardiovascular";

            // AI Explanation / Response Box
            string displayText = !string.IsNullOrEmpty(aiChatResponseText)
                ? aiChatResponseText
                : aiTutor.GetExplanation(organName);

            GUI.Box(new Rect(r.x, r.y + 50, r.width, 185), "");
            GUI.Label(new Rect(r.x + 8, r.y + 54, r.width - 16, 175), displayText);

            // Citations and Voice row
            int rowY = (int)r.y + 240;
            string sourceStr = aiCitations.Length > 0 ? string.Join(" | ", aiCitations) : "Gray's Anatomy & Guyton-Hall";
            GUI.Label(new Rect(r.x, rowY, r.width - 110, 26), $"<size=9><b>Source:</b> {sourceStr}</size>");

            // Audio / TTS Listen Button
            if (GUI.Button(new Rect(r.x + r.width - 105, rowY, 105, 24), "🔊 Listen [TTS]"))
            {
                if (llmClient != null)
                {
                    llmClient.PlayLatestAudio();
                }
            }

            // Quick Question Suggestions (2x2 grid)
            int qY = rowY + 28;
            GUI.Label(new Rect(r.x, qY, r.width, 16), "<size=9><b>Quick Clinical Inquiries:</b></size>");
            int halfW = ((int)r.width - 6) / 2;
            if (GUI.Button(new Rect(r.x, qY + 18, halfW, 22), "Referred Angina"))
            {
                customQuestionText = "Why does myocardial ischemia radiate to the left arm and jaw?";
                SubmitChatQuestion(organName, bodySystem);
            }
            if (GUI.Button(new Rect(r.x + halfW + 6, qY + 18, halfW, 22), "Portal Hemodynamics"))
            {
                customQuestionText = "Explain the dual blood supply to the liver and portal hypertension.";
                SubmitChatQuestion(organName, bodySystem);
            }
            if (GUI.Button(new Rect(r.x, qY + 44, halfW, 22), "Carina & Cough Reflex"))
            {
                customQuestionText = "What is the clinical significance of the tracheal carina?";
                SubmitChatQuestion(organName, bodySystem);
            }
            if (GUI.Button(new Rect(r.x + halfW + 6, qY + 44, halfW, 22), "Baroreceptor Reflex"))
            {
                customQuestionText = "How do aortic arch baroreceptors regulate arterial blood pressure?";
                SubmitChatQuestion(organName, bodySystem);
            }

            // Custom Question Input Field & Submit
            int inputY = qY + 72;
            customQuestionText = GUI.TextField(new Rect(r.x, inputY, r.width - 85, 24), customQuestionText);
            
            string sendBtnLabel = (llmClient != null && llmClient.isLoading) ? "..." : "Ask AI";
            if (GUI.Button(new Rect(r.x + r.width - 80, inputY, 80, 24), sendBtnLabel))
            {
                SubmitChatQuestion(organName, bodySystem);
            }

            // Disclaimer Footer
            GUI.Label(new Rect(r.x, inputY + 26, r.width, 22), "<size=8><color=#888888>DISCLAIMER: Educational medical simulation only.</color></size>");
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
