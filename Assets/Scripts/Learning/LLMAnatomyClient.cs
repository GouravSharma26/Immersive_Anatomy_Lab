using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace ImmersiveAnatomy.Learning
{
    public enum BackendEnvironment
    {
        EditorLocalDev,       // http://127.0.0.1:8000
        LocalLANTesting,      // Configurable LAN IP (e.g. http://192.168.1.10:8000)
        ProductionRenderCloud // Deployed Render HTTPS (e.g. https://immersive-anatomy-api.onrender.com)
    }

    public enum TutorConnectionState
    {
        Idle,
        CheckingConnection,
        OnlineCloudAI,
        Thinking,
        WakingUpService,      // For Render Free tier cold start (>8s)
        OfflineReferenceMode,
        ConnectionError
    }

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
        public bool isOfflineFallback;
    }

    public class LLMAnatomyClient : MonoBehaviour
    {
        public static LLMAnatomyClient Instance { get; private set; }

        [Header("Environment & Cloud Configuration")]
        public BackendEnvironment environment = BackendEnvironment.ProductionRenderCloud;
        public string productionCloudUrl = "https://immersive-anatomy-api.onrender.com";
        public string localDevUrl = "http://127.0.0.1:8000";
        public string localLanUrl = "http://192.168.1.100:8000";

        [Header("Connection State")]
        public string serverBaseUrl = "http://127.0.0.1:8000";
        public bool isServerOnline = false;
        public bool isLoading = false;
        public TutorConnectionState connectionState = TutorConnectionState.Idle;

        [Header("Timeouts & Resilience")]
        public int healthTimeoutSeconds = 4;
        public int requestTimeoutSeconds = 15;
        public int maxRetries = 1;

        public ChatResponseData latestResponse { get; private set; }
        private AudioSource audioSource;
        private Coroutine activeChatCoroutine;

        public event Action<TutorConnectionState> OnConnectionStateChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

            ResolveActiveEndpoint();
            StartCoroutine(CheckHealthCoroutine());
        }

        public void ResolveActiveEndpoint()
        {
            // In release builds, never default to 127.0.0.1 localhost (headset loopback trap)
            #if !UNITY_EDITOR
            if (environment == BackendEnvironment.EditorLocalDev)
            {
                environment = BackendEnvironment.ProductionRenderCloud;
            }
            #endif

            switch (environment)
            {
                case BackendEnvironment.ProductionRenderCloud:
                    serverBaseUrl = productionCloudUrl.TrimEnd('/');
                    break;
                case BackendEnvironment.LocalLANTesting:
                    serverBaseUrl = localLanUrl.TrimEnd('/');
                    break;
                case BackendEnvironment.EditorLocalDev:
                default:
                    serverBaseUrl = localDevUrl.TrimEnd('/');
                    break;
            }

            Debug.Log($"[LLMAnatomyClient] Configured active endpoint [{environment}]: {serverBaseUrl}");
        }

        public void SetEnvironment(BackendEnvironment env, string customUrl = null)
        {
            environment = env;
            if (!string.IsNullOrEmpty(customUrl))
            {
                if (env == BackendEnvironment.ProductionRenderCloud) productionCloudUrl = customUrl;
                else if (env == BackendEnvironment.LocalLANTesting) localLanUrl = customUrl;
                else localDevUrl = customUrl;
            }
            ResolveActiveEndpoint();
            CheckServerHealth();
        }

        public void CheckServerHealth()
        {
            StartCoroutine(CheckHealthCoroutine());
        }

        private IEnumerator CheckHealthCoroutine()
        {
            SetState(TutorConnectionState.CheckingConnection);
            string url = $"{serverBaseUrl}/health/live";

            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                req.timeout = healthTimeoutSeconds;
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    isServerOnline = true;
                    SetState(TutorConnectionState.OnlineCloudAI);
                    Debug.Log($"<color=#00FFAA>[LLMAnatomyClient] Dr. Nova AI Backend Online at {serverBaseUrl}</color>");
                }
                else
                {
                    // Fallback to legacy route if /health/live not present
                    string legacyUrl = $"{serverBaseUrl}/api/health";
                    using (UnityWebRequest legReq = UnityWebRequest.Get(legacyUrl))
                    {
                        legReq.timeout = healthTimeoutSeconds;
                        yield return legReq.SendWebRequest();
                        isServerOnline = (legReq.result == UnityWebRequest.Result.Success);
                    }

                    if (isServerOnline)
                    {
                        SetState(TutorConnectionState.OnlineCloudAI);
                    }
                    else
                    {
                        isServerOnline = false;
                        SetState(TutorConnectionState.OfflineReferenceMode);
                        Debug.Log($"<color=#FFCC00>[LLMAnatomyClient] Dr. Nova Cloud Backend unreachable ({req.error}). Engaging Offline Reference Mode.</color>");
                    }
                }
            }
        }

        public void AskAIAssistant(string question, string organName, string bodySystem, string level, Action<ChatResponseData> callback = null)
        {
            if (isLoading)
            {
                Debug.LogWarning("[LLMAnatomyClient] Request already in progress; ignoring duplicate query.");
                return;
            }

            if (activeChatCoroutine != null) StopCoroutine(activeChatCoroutine);
            activeChatCoroutine = StartCoroutine(SendChatCoroutine(question, organName, bodySystem, level, callback));
        }

        public void CancelActiveRequest()
        {
            if (isLoading && activeChatCoroutine != null)
            {
                StopCoroutine(activeChatCoroutine);
                isLoading = false;
                SetState(isServerOnline ? TutorConnectionState.OnlineCloudAI : TutorConnectionState.OfflineReferenceMode);
                Debug.Log("[LLMAnatomyClient] Active Dr. Nova request cancelled by user.");
            }
        }

        private IEnumerator SendChatCoroutine(string question, string organName, string bodySystem, string level, Action<ChatResponseData> callback)
        {
            isLoading = true;
            SetState(TutorConnectionState.Thinking);

            // Cold start detector for Render Free instances
            Coroutine coldStartTimer = StartCoroutine(ColdStartMonitorCoroutine());

            ChatRequestData reqData = new ChatRequestData
            {
                message = question.Length > 500 ? question.Substring(0, 500) : question,
                selected_organ = organName,
                body_system = bodySystem,
                pedagogical_level = string.IsNullOrEmpty(level) ? "student" : level.ToLower(),
                user_accuracy = 100f
            };

            string json = JsonUtility.ToJson(reqData);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

            ChatResponseData responseData = null;
            bool success = false;
            int attempt = 0;

            while (attempt <= maxRetries && !success)
            {
                attempt++;
                string url = $"{serverBaseUrl}/api/chat";

                using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
                {
                    req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    req.downloadHandler = new DownloadHandlerBuffer();
                    req.SetRequestHeader("Content-Type", "application/json");
                    req.timeout = requestTimeoutSeconds;

                    yield return req.SendWebRequest();

                    if (req.result == UnityWebRequest.Result.Success)
                    {
                        try
                        {
                            string resText = req.downloadHandler.text;
                            responseData = JsonUtility.FromJson<ChatResponseData>(resText);
                            if (responseData != null && !string.IsNullOrEmpty(responseData.reply))
                            {
                                responseData.isOfflineFallback = false;
                                latestResponse = responseData;
                                isServerOnline = true;
                                success = true;
                                SetState(TutorConnectionState.OnlineCloudAI);
                            }
                        }
                        catch (Exception parseEx)
                        {
                            Debug.LogWarning($"[LLMAnatomyClient] JSON parse error: {parseEx.Message}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[LLMAnatomyClient] Request attempt {attempt} failed ({req.error}).");
                        if (attempt <= maxRetries)
                        {
                            yield return new WaitForSeconds(1.5f); // Backoff before retry
                        }
                    }
                }
            }

            if (coldStartTimer != null) StopCoroutine(coldStartTimer);
            isLoading = false;

            // If cloud response failed or network is unreachable, trigger honest Offline Fallback
            if (!success || responseData == null)
            {
                responseData = GenerateOfflineFallbackResponse(question, organName, level);
                latestResponse = responseData;
                isServerOnline = false;
                SetState(TutorConnectionState.OfflineReferenceMode);
            }

            callback?.Invoke(responseData);
        }

        private IEnumerator ColdStartMonitorCoroutine()
        {
            yield return new WaitForSeconds(7.0f);
            if (isLoading)
            {
                SetState(TutorConnectionState.WakingUpService);
                Debug.Log("[LLMAnatomyClient] Cloud service waking up from sleep...");
            }
        }

        private ChatResponseData GenerateOfflineFallbackResponse(string question, string organName, string level)
        {
            var tutor = FindFirstObjectByType<AIAnatomyTutor>();
            string organKey = string.IsNullOrEmpty(organName) ? "Heart" : organName;
            string explanation = (tutor != null) ? tutor.GetExplanation(organKey) : "Detailed clinical anatomical references available in local lab banks.";

            return new ChatResponseData
            {
                reply = $"<b>[Offline Reference Mode]</b>\n" +
                        $"<i>(Dr. Nova cloud AI is currently offline or unreachable. Displaying verified local clinical textbook reference data.)</i>\n\n" +
                        $"{explanation}",
                citations = new string[] { "Local Pre-compiled Anatomical Reference Bank (Offline Mode)" },
                clinical_pearl = $"Verified landmark data for {organKey}. Reconnect to the internet for live AI consultation.",
                mnemonic = "Review primary neurovascular, hemodynamic, and structural landmarks in the 3D examination table.",
                disclaimer = "Educational medical simulation only. Offline reference compilation.",
                audio_url = null,
                isOfflineFallback = true
            };
        }

        private void SetState(TutorConnectionState newState)
        {
            connectionState = newState;
            OnConnectionStateChanged?.Invoke(newState);
        }

        public bool IsAudioPlaying()
        {
            return audioSource != null && audioSource.isPlaying;
        }

        public void StopAudio()
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
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
            if (string.IsNullOrEmpty(relativeUrl)) return;
            StartCoroutine(DownloadAndPlayAudioCoroutine(relativeUrl));
        }

        private IEnumerator DownloadAndPlayAudioCoroutine(string relativeUrl)
        {
            string fullUrl = relativeUrl.StartsWith("http") ? relativeUrl : $"{serverBaseUrl}{relativeUrl}";
            AudioType audioType = relativeUrl.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ? AudioType.MPEG : AudioType.WAV;

            using (UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(fullUrl, audioType))
            {
                req.timeout = 8;
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
                    Debug.LogWarning($"[LLMAnatomyClient] Audio stream not loaded from {fullUrl} ({req.error}). Text reply remains active.");
                }
            }
        }
    }
}
