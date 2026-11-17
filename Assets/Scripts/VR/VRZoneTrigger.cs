using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ImmersiveAnatomy.VR
{
    public class VRZoneTrigger : MonoBehaviour
    {
        public enum LabZoneType
        {
            Dissection,
            Quiz,
            TeachingAI,
            Comparison
        }

        [Header("Zone Configuration")]
        public LabZoneType zoneType = LabZoneType.Dissection;
        public string zoneDisplayName = "Anatomical Dissection Stage";
        public Color zoneAccentColor = Color.cyan;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip enterZoneClip;

        private void Awake()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Detect if player camera or rig entered zone
            if (other.CompareTag("MainCamera") || other.GetComponentInParent<Camera>() != null || other.name.Contains("XR") || other.name.Contains("Player"))
            {
                OnPlayerEnteredZone();
            }
        }

        public void OnPlayerEnteredZone()
        {
            PlayEnterSound();

            AnatomyInteraction interaction = FindFirstObjectByType<AnatomyInteraction>();
            if (interaction == null) return;

            switch (zoneType)
            {
                case LabZoneType.Dissection:
                    interaction.currentArea = AnatomyInteraction.LabArea.Exploration;
                    break;

                case LabZoneType.Quiz:
                    interaction.currentArea = AnatomyInteraction.LabArea.Quiz;
                    if (interaction.learningUI != null && interaction.learningUI.quizEngine != null)
                    {
                        if (interaction.learningUI.quizEngine.State == ImmersiveAnatomy.Learning.QuizState.Idle)
                        {
                            interaction.learningUI.quizEngine.StartQuizSession(5);
                        }
                    }
                    break;

                case LabZoneType.TeachingAI:
                    interaction.currentArea = AnatomyInteraction.LabArea.AITutor;
                    if (interaction.learningUI != null)
                    {
                        interaction.learningUI.activeTab = ImmersiveAnatomy.Learning.LearningTab.AITutor;
                    }
                    break;

                case LabZoneType.Comparison:
                    if (interaction.comparisonController != null && !interaction.comparisonController.IsInComparisonMode)
                    {
                        interaction.comparisonController.StartComparisonMode(null);
                    }
                    break;
            }
        }

        private void PlayEnterSound()
        {
            if (enterZoneClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(enterZoneClip, 0.7f);
            }
            else if (audioSource != null)
            {
                float freq = zoneType == LabZoneType.Quiz ? 587.33f : (zoneType == LabZoneType.TeachingAI ? 659.25f : 440f);
                int sampleRate = 44100;
                int sampleCount = (int)(sampleRate * 0.18f);
                float[] samples = new float[sampleCount];
                for (int i = 0; i < sampleCount; i++)
                {
                    float t = (float)i / sampleRate;
                    float env = Mathf.Sin(Mathf.PI * (t / 0.18f));
                    samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.25f;
                }
                AudioClip clip = AudioClip.Create("ZoneTone", sampleCount, 1, sampleRate, false);
                clip.SetData(samples, 0);
                audioSource.PlayOneShot(clip);
            }
        }
    }
}
