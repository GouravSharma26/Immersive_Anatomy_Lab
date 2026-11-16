using System;
using System.Collections.Generic;
using UnityEngine;

namespace ImmersiveAnatomy.Learning
{
    [Serializable]
    public class SystemMastery
    {
        public string systemName;
        public int attempted;
        public int correct;
        public float Accuracy => attempted > 0 ? ((float)correct / attempted) * 100f : 0f;
    }

    [Serializable]
    public class AnalyticsData
    {
        public int totalScore = 0;
        public int totalAttempted = 0;
        public int totalCorrect = 0;
        public int highestStreak = 0;
        public int clinicalCasesCompleted = 0;
        public List<SystemMastery> systemStats = new List<SystemMastery>();
        public List<string> unlockedBadges = new List<string>();
    }

    public class LearningAnalytics : MonoBehaviour
    {
        private const string SAVE_KEY = "ImmersiveAnatomy_Analytics_v1";
        public AnalyticsData data = new AnalyticsData();

        public int currentStreak { get; private set; } = 0;
        public int currentSessionScore { get; private set; } = 0;

        private void Awake()
        {
            LoadAnalytics();
        }

        public void RecordAnswer(string systemName, bool isCorrect, int pointsAwarded)
        {
            data.totalAttempted++;
            if (isCorrect)
            {
                data.totalCorrect++;
                currentStreak++;
                if (currentStreak > data.highestStreak) data.highestStreak = currentStreak;
                currentSessionScore += pointsAwarded;
                data.totalScore += pointsAwarded;
            }
            else
            {
                currentStreak = 0;
            }

            // Update per-system statistics
            SystemMastery mastery = data.systemStats.Find(s => s.systemName.Equals(systemName, StringComparison.OrdinalIgnoreCase));
            if (mastery == null)
            {
                mastery = new SystemMastery { systemName = systemName };
                data.systemStats.Add(mastery);
            }
            mastery.attempted++;
            if (isCorrect) mastery.correct++;

            CheckBadges();
            SaveAnalytics();
        }

        public void RecordClinicalCaseCompleted()
        {
            data.clinicalCasesCompleted++;
            CheckBadges();
            SaveAnalytics();
        }

        private void CheckBadges()
        {
            UnlockBadge("First Incision", "Completed your first anatomical knowledge check.");

            if (data.highestStreak >= 5)
                UnlockBadge("Clinical Precision", "Maintained an unbroken streak of 5 correct answers.");

            if (data.clinicalCasesCompleted >= 3)
                UnlockBadge("Chief Resident", "Successfully resolved 3 patient clinical cases.");

            foreach (var stat in data.systemStats)
            {
                if (stat.correct >= 3 && stat.Accuracy >= 75f)
                {
                    UnlockBadge($"{stat.systemName} Specialist", $"Achieved over 75% accuracy in {stat.systemName} system anatomy.");
                }
            }
        }

        private void UnlockBadge(string badgeName, string description)
        {
            if (!data.unlockedBadges.Contains(badgeName))
            {
                data.unlockedBadges.Add(badgeName);
                Debug.Log($"[LearningAnalytics] BADGE UNLOCKED: {badgeName} - {description}");
            }
        }

        public float OverallAccuracy => data.totalAttempted > 0 ? ((float)data.totalCorrect / data.totalAttempted) * 100f : 0f;

        public float GetSystemAccuracy(string systemName)
        {
            var stat = data.systemStats.Find(s => s.systemName.Equals(systemName, StringComparison.OrdinalIgnoreCase));
            return stat != null ? stat.Accuracy : 0f;
        }

        public void ResetAnalytics()
        {
            data = new AnalyticsData();
            currentStreak = 0;
            currentSessionScore = 0;
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.Save();
        }

        private void SaveAnalytics()
        {
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
        }

        private void LoadAnalytics()
        {
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                string json = PlayerPrefs.GetString(SAVE_KEY);
                data = JsonUtility.FromJson<AnalyticsData>(json);
            }
            if (data == null) data = new AnalyticsData();
            if (data.systemStats == null) data.systemStats = new List<SystemMastery>();
            if (data.unlockedBadges == null) data.unlockedBadges = new List<string>();
        }
    }
}
