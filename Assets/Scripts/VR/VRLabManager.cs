using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ImmersiveAnatomy.VR
{
    public class VRLabManager : MonoBehaviour
    {
        public static VRLabManager Instance { get; private set; }

        [Header("VR Rig References")]
        public GameObject xrOrigin;
        public GameObject xrDeviceSimulator;
        public Transform mannequinRoot;
        public Transform tabletAnchor;

        [Header("Audio Clips")]
        public AudioClip chimeGrab;
        public AudioClip chimeSnap;
        public AudioClip chimeRelease;
        public AudioClip chimeAction;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != null && Instance != this) Destroy(gameObject);

            GenerateProceduralAudioIfNull();
        }

        private void GenerateProceduralAudioIfNull()
        {
            if (chimeGrab == null) chimeGrab = CreateProceduralChime(587.33f, 880f, 0.12f); // D5 to A5
            if (chimeSnap == null) chimeSnap = CreateProceduralChime(880f, 1174.66f, 0.18f); // A5 to D6
            if (chimeRelease == null) chimeRelease = CreateProceduralChime(659.25f, 523.25f, 0.10f); // E5 to C5
            if (chimeAction == null) chimeAction = CreateProceduralChime(523.25f, 659.25f, 0.08f); // C5 to E5
        }

        private AudioClip CreateProceduralChime(float startFreq, float endFreq, float duration)
        {
            int sampleRate = 44100;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;
                float freq = Mathf.Lerp(startFreq, endFreq, progress);
                float envelope = Mathf.Sin(progress * Mathf.PI); // Smooth attack and decay
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.4f;
            }

            AudioClip clip = AudioClip.Create("VR_Chime_" + startFreq, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
