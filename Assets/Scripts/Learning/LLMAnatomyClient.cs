using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace ImmersiveAnatomy.Learning
{
    [Serializable]
    public class ChatRequestData
    {
        public string message;
        public string selected_organ;
        public string body_system;
        public string pedagogical_level;
        public float user_accuracy;
    }

    [Serializable]
    public class ChatResponseData
    {
        public string reply;
        public string[] citations;
        public string clinical_pearl;
        public string mnemonic;
        public string disclaimer;
        public string audio_url;
    }

    public class LLMAnatomyClient : MonoBehaviour
    {
        [Header("Backend Connection")]
        public string serverBaseUrl = "http://127.0.0.1:8000";
        public bool isServerOnline = false;
        public bool isLoading = false;

        public ChatResponseData latestResponse { get; private set; }
        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            StartCoroutine(CheckHealthCoroutine());
        }

        public void CheckServerHealth()
        {
            StartCoroutine(CheckHealthCoroutine());
        }

        private IEnumerator CheckHealthCoroutine()
        {
            string url = $"{serverBaseUrl}/api/health";
            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                req.timeout = 3;
                yield return req.SendWebRequest();
                isServerOnline = (req.result == UnityWebRequest.Result.Success);
                if (isServerOnline)
                {
                    Debug.Log($"[LLMAnatomyClient] Connected to AI Tutor Backend at {serverBaseUrl}");
                }
            }
        }

        public void AskAIAssistant(string question, string organName, string bodySystem, string level, Action<ChatResponseData> callback = null)
        {
            StartCoroutine(SendChatCoroutine(question, organName, bodySystem, level, callback));
        }

        private IEnumerator SendChatCoroutine(string question, string organName, string bodySystem, string level, Action<ChatResponseData> callback)
        {
            isLoading = true;
            string url = $"{serverBaseUrl}/api/chat";

            ChatRequestData reqData = new ChatRequestData
            {
                message = question,
                selected_organ = organName,
                body_system = bodySystem,
                pedagogical_level = level.ToLower(),
                user_accuracy = 100f
            };

            string json = JsonUtility.ToJson(reqData);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

            using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = 10;

                yield return req.SendWebRequest();

                isLoading = false;

                if (req.result == UnityWebRequest.Result.Success)
                {
                    string resText = req.downloadHandler.text;
                    ChatResponseData response = JsonUtility.FromJson<ChatResponseData>(resText);
                    latestResponse = response;
                    isServerOnline = true;
                    callback?.Invoke(response);
                }
                else
                {
                    Debug.LogWarning($"[LLMAnatomyClient] Backend request failed: {req.error}");
                    isServerOnline = false;
                    callback?.Invoke(null);
                }
            }
        }

        public void PlayLatestAudio()
        {
            if (latestResponse != null && !string.IsNullOrEmpty(latestResponse.audio_url))
            {
                PlayAudioFromEndpoint(latestResponse.audio_url);
            }
        }

        public void PlayAudioFromEndpoint(string relativeUrl)
        {
            StartCoroutine(DownloadAndPlayAudioCoroutine(relativeUrl));
        }

        private IEnumerator DownloadAndPlayAudioCoroutine(string relativeUrl)
        {
            string fullUrl = $"{serverBaseUrl}{relativeUrl}";
            using (UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(fullUrl, AudioType.WAV))
            {
                req.timeout = 5;
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(req);
                    if (clip != null && audioSource != null)
                    {
                        audioSource.Stop();
                        audioSource.clip = clip;
                        audioSource.Play();
                    }
                }
                else
                {
                    Debug.LogWarning($"[LLMAnatomyClient] Could not fetch audio from {fullUrl}: {req.error}");
                }
            }
        }
    }
}
