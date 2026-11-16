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

        [Header("UI State")]
        public bool isLearningUIOpen = false;
        public LearningTab activeTab = LearningTab.Quiz;

        private OrganInteractable lastSelectedOrgan;
        private Vector2 scrollPos;

        private void Start()
        {
            if (quizEngine == null) quizEngine = GetComponent<QuizEngine>();
            if (aiTutor == null) aiTutor = GetComponent<AIAnatomyTutor>();
            if (analytics == null) analytics = GetComponent<LearningAnalytics>();
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
            if (!isLearningUIOpen) return;

            int modalW = 520;
            int modalH = 500;
            int modalX = 25;
            int modalY = 65;

            // Main Background Panel (Labster clinical medical card style)
            GUI.Box(new Rect(modalX, modalY, modalW, modalH), "");

            // Header Ribbon
            GUI.Box(new Rect(modalX, modalY, modalW, 40), "");
            GUI.Label(new Rect(modalX + 15, modalY + 10, 360, 24), "<b>CLINICAL LEARNING & AI TUTOR SYSTEM</b>");

            if (GUI.Button(new Rect(modalX + modalW - 90, modalY + 8, 75, 24), "Close [L]"))
            {
                ToggleLearningUI();
                return;
            }

            // Tab Navigation
            int tabW = (modalW - 30) / 4;
            string[] tabNames = new string[] { "Anatomy Quiz", "Clinical Cases", "AI Tutor", "Analytics" };
            for (int i = 0; i < tabNames.Length; i++)
            {
                bool isSelected = (activeTab == (LearningTab)i);
                string label = isSelected ? $"<b><color=#00E5FF>{tabNames[i]}</color></b>" : tabNames[i];
                if (GUI.Button(new Rect(modalX + 15 + i * tabW, modalY + 46, tabW, 26), label))
                {
                    activeTab = (LearningTab)i;
                    if (activeTab == LearningTab.Quiz && quizEngine != null && quizEngine.State == QuizState.Idle)
                    {
                        quizEngine.StartQuizSession(5);
                    }
                    else if (activeTab == LearningTab.ClinicalCases && quizEngine != null && quizEngine.State != QuizState.ClinicalCaseActive && quizEngine.State != QuizState.CaseFeedback)
                    {
                        quizEngine.StartClinicalCase(0);
                    }
                }
            }

            // Separator
            GUI.Box(new Rect(modalX + 15, modalY + 76, modalW - 30, 2), "");

            // TAB CONTENT
            Rect contentRect = new Rect(modalX + 15, modalY + 84, modalW - 30, modalH - 95);

            switch (activeTab)
            {
                case LearningTab.Quiz:
                    RenderQuizTab(contentRect);
                    break;
                case LearningTab.ClinicalCases:
                    RenderClinicalCasesTab(contentRect);
                    break;
                case LearningTab.AITutor:
                    RenderAITutorTab(contentRect);
                    break;
                case LearningTab.Analytics:
                    RenderAnalyticsTab(contentRect);
                    break;
            }
        }

        private void RenderQuizTab(Rect r)
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

        private void RenderClinicalCasesTab(Rect r)
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

        private void RenderAITutorTab(Rect r)
        {
            if (aiTutor == null) return;

            GUI.Label(new Rect(r.x, r.y, 300, 22), $"<b>{aiTutor.tutorName}</b>");

            string lvlText = aiTutor.pedagogicalLevel == TutorPedagogicalLevel.SimplifiedStudent
                ? "Level: <color=#00FF88>Student Mode</color>"
                : "Level: <color=#00E5FF>Clinical Specialist</color>";

            if (GUI.Button(new Rect(r.x + r.width - 180, r.y, 180, 24), lvlText))
            {
                aiTutor.TogglePedagogicalLevel();
            }

            string organName = lastSelectedOrgan != null ? lastSelectedOrgan.organName : "Heart";
            GUI.Label(new Rect(r.x, r.y + 28, r.width, 22), $"Target Organ: <b>{organName}</b> (Click any organ in 3D to switch)");

            // AI Explanation Box with scroll
            string explanation = aiTutor.GetExplanation(organName);
            GUI.Box(new Rect(r.x, r.y + 54, r.width, 260), "");
            GUI.Label(new Rect(r.x + 12, r.y + 60, r.width - 24, 245), explanation);

            // Mnemonic / High-Yield Study Tip
            if (GUI.Button(new Rect(r.x, r.y + 324, r.width / 2 - 5, 30), "Show High-Yield Clinical Fact"))
            {
                // Triggers update
            }

            if (GUI.Button(new Rect(r.x + r.width / 2 + 5, r.y + 324, r.width / 2 - 5, 30), "Toggle Student / Clinical Mode"))
            {
                aiTutor.TogglePedagogicalLevel();
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
