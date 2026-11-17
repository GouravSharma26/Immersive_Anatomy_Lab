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

            // Ensure floor pads are clearly spaced out at their dedicated stations away from the central table
            if (zoneType == LabZoneType.Quiz && transform.position.x > -4f)
            {
                transform.position = new Vector3(-4.6f, 0f, 1.4f);
            }
            else if (zoneType == LabZoneType.TeachingAI && transform.position.x < 4f)
            {
                transform.position = new Vector3(4.6f, 0f, 1.4f);
            }
            else if (zoneType == LabZoneType.Comparison && transform.position.z > -5f)
            {
                transform.position = new Vector3(0f, 0f, -5.0f);
            }
            else if (zoneType == LabZoneType.Dissection)
            {
                transform.position = new Vector3(0f, 0f, 0.2f);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Detect if player camera or rig entered zone
            if (other.CompareTag("MainCamera") || other.GetComponentInParent<Camera>() != null || other.name.Contains("XR") || other.name.Contains("Player"))
            {
                OnPlayerEnteredZone();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            // Detect if player camera or rig exited zone
            if (other.CompareTag("MainCamera") || other.GetComponentInParent<Camera>() != null || other.name.Contains("XR") || other.name.Contains("Player"))
            {
                OnPlayerExitedZone();
            }
        }

        public void OnPlayerExitedZone()
        {
            AnatomyInteraction interaction = FindFirstObjectByType<AnatomyInteraction>();
            if (interaction == null) return;

            if (zoneType == LabZoneType.TeachingAI)
            {
                if (interaction.isTypingTutorQuestion)
                {
                    interaction.isTypingTutorQuestion = false;
                    interaction.SetCursorVisible(false);
                }
                if (interaction.currentArea == AnatomyInteraction.LabArea.AITutor)
                {
                    interaction.currentArea = AnatomyInteraction.LabArea.Exploration;
                }
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
                    // Note: Do not auto-start quiz! Let the user press "START QUIZ" when ready.
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
