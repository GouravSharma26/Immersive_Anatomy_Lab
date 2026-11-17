using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ImmersiveAnatomy.VR
{
    public class VRDissectionTableConsole : MonoBehaviour
    {
        public static VRDissectionTableConsole Instance { get; private set; }

        [Header("Controllers")]
        public ExplodedViewController explodedViewController;
        public XRayViewController xrayViewController;
        public OrganIsolationController isolationController;

        [Header("Console Buttons")]
        public Button explodeBtn;
        public Button xrayBtn;
        public Button vascularBtn;
        public Button thoracicBtn;
        public Button resetAnatomyBtn;
        public Text explodeBtnText;
        public Text xrayBtnText;
        public Text vascularBtnText;
        public Text thoracicBtnText;

        [Header("System Toggle Buttons")]
        public Button skeletalBtn;
        public Button cardioBtn;
        public Button respBtn;
        public Button nervousBtn;
        public Button digestiveBtn;
        public Button showAllBtn;

        [Header("Holographic Organ Card (Side Display)")]
        public GameObject organCardRoot;
        public Text cardOrganNameText;
        public Text cardSystemText;
        public Text cardPhysiologyText;
        public Text cardVascularText;
        public Text cardPathologyText;
        public Button returnToSocketBtn;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip clickClip;
        public AudioClip resetClip;

        private VRGrabbableOrgan activeExaminedOrgan;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureDissectionConsole()
        {
            if (FindFirstObjectByType<VRDissectionTableConsole>() == null)
            {
                GameObject table = GameObject.Find("DissectionTable");
                GameObject consoleGO = new GameObject("DissectionTableConsole");
                if (table != null)
                {
                    consoleGO.transform.position = new Vector3(table.transform.position.x, 0.92f, table.transform.position.z - 0.55f);
                }
                else
                {
                    consoleGO.transform.position = new Vector3(0f, 0.92f, 0.95f);
                }
                consoleGO.transform.rotation = Quaternion.Euler(35f, 0f, 0f);
                consoleGO.AddComponent<VRDissectionTableConsole>();
            }
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }

            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }

        private void Start()
        {
            FindControllers();
            EnsureConsoleUI();
            InitButtons();
            UpdateCardDefault();

            if (xrayViewController != null)
            {
                xrayViewController.OnModeChanged += HandleModeChanged;
                UpdateModeButtonStates();
            }
        }

        private void OnDestroy()
        {
            if (xrayViewController != null)
            {
                xrayViewController.OnModeChanged -= HandleModeChanged;
            }
        }

        private void HandleModeChanged(XRayViewController.MedicalVisualizationMode mode)
        {
            UpdateModeButtonStates();
        }

        public void EnsureConsoleUI()
        {
            if (vascularBtn != null && thoracicBtn != null) return;

            Transform canvasTrans = transform.Find("ConsoleCanvas");
            if (canvasTrans == null)
            {
                GameObject canvasGO = new GameObject("ConsoleCanvas");
                canvasGO.transform.SetParent(transform, false);
                canvasGO.transform.localPosition = new Vector3(0f, 0.005f, 0f);
                canvasGO.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                canvasGO.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

                Canvas canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                RectTransform crt = canvasGO.GetComponent<RectTransform>();
                crt.sizeDelta = new Vector2(1400f, 350f);

                CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.dynamicPixelsPerUnit = 4f;

                canvasGO.AddComponent<GraphicRaycaster>();
                canvasGO.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
                canvasTrans = canvasGO.transform;
            }

            if (transform.Find("ConsolePlate") == null)
            {
                GameObject backing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                backing.name = "ConsolePlate";
                backing.transform.SetParent(transform, false);
                backing.transform.localPosition = new Vector3(0f, -0.015f, 0f);
                backing.transform.localScale = new Vector3(1.4f, 0.025f, 0.35f);
                var plateRend = backing.GetComponent<MeshRenderer>();
                if (plateRend != null && plateRend.material != null)
                {
                    plateRend.material.color = new Color(0.06f, 0.09f, 0.14f);
                }
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Build Row 1 buttons if unassigned
            float r1Y = 70f;
            float bW = 250f;
            float bH = 55f;
            float spacingX = 265f;
            float startX = -2f * spacingX;

            if (explodeBtn == null)
            {
                var go = CreateConsoleButton("ExplodedViewButton", canvasTrans, new Vector2(startX + 0 * spacingX, r1Y), new Vector2(bW, bH), "Exploded View", font, 18, new Color(0.18f, 0.28f, 0.45f));
                explodeBtn = go.GetComponent<Button>();
                explodeBtnText = go.GetComponentInChildren<Text>();
            }

            if (xrayBtn == null)
            {
                var go = CreateConsoleButton("XRayViewButton", canvasTrans, new Vector2(startX + 1 * spacingX, r1Y), new Vector2(bW, bH), "X-Ray Mode", font, 18, new Color(0.12f, 0.35f, 0.50f));
                xrayBtn = go.GetComponent<Button>();
                xrayBtnText = go.GetComponentInChildren<Text>();
            }

            if (vascularBtn == null)
            {
                var go = CreateConsoleButton("VascularViewButton", canvasTrans, new Vector2(startX + 2 * spacingX, r1Y), new Vector2(bW, bH), "Vascular Angiogram", font, 18, new Color(0.50f, 0.15f, 0.22f));
                vascularBtn = go.GetComponent<Button>();
                vascularBtnText = go.GetComponentInChildren<Text>();
            }

            if (thoracicBtn == null)
            {
                var go = CreateConsoleButton("ThoracicViewButton", canvasTrans, new Vector2(startX + 3 * spacingX, r1Y), new Vector2(bW, bH), "Thoracic Silhouette", font, 18, new Color(0.15f, 0.40f, 0.45f));
                thoracicBtn = go.GetComponent<Button>();
                thoracicBtnText = go.GetComponentInChildren<Text>();
            }

            if (resetAnatomyBtn == null)
            {
                var go = CreateConsoleButton("ResetAnatomyButton", canvasTrans, new Vector2(startX + 4 * spacingX, r1Y), new Vector2(bW, bH), "Reset Anatomy", font, 18, new Color(0.15f, 0.45f, 0.28f));
                resetAnatomyBtn = go.GetComponent<Button>();
            }

            // Build Row 2 buttons if unassigned
            float r2Y = -15f;
            float sysBW = 205f;
            float sysBH = 48f;
            float sysSpacingX = 220f;
            float sysStartX = -2.5f * sysSpacingX;

            if (skeletalBtn == null) skeletalBtn = CreateConsoleButton("BtnSkeletal", canvasTrans, new Vector2(sysStartX + 0 * sysSpacingX, r2Y), new Vector2(sysBW, sysBH), "Skeletal System", font, 16, new Color(0.20f, 0.25f, 0.35f)).GetComponent<Button>();
            if (cardioBtn == null) cardioBtn = CreateConsoleButton("BtnCardio", canvasTrans, new Vector2(sysStartX + 1 * sysSpacingX, r2Y), new Vector2(sysBW, sysBH), "Cardiovascular", font, 16, new Color(0.20f, 0.25f, 0.35f)).GetComponent<Button>();
            if (respBtn == null) respBtn = CreateConsoleButton("BtnResp", canvasTrans, new Vector2(sysStartX + 2 * sysSpacingX, r2Y), new Vector2(sysBW, sysBH), "Respiratory", font, 16, new Color(0.20f, 0.25f, 0.35f)).GetComponent<Button>();
            if (nervousBtn == null) nervousBtn = CreateConsoleButton("BtnNervous", canvasTrans, new Vector2(sysStartX + 3 * sysSpacingX, r2Y), new Vector2(sysBW, sysBH), "Nervous System", font, 16, new Color(0.20f, 0.25f, 0.35f)).GetComponent<Button>();
            if (digestiveBtn == null) digestiveBtn = CreateConsoleButton("BtnDigestive", canvasTrans, new Vector2(sysStartX + 4 * sysSpacingX, r2Y), new Vector2(sysBW, sysBH), "Digestive System", font, 16, new Color(0.20f, 0.25f, 0.35f)).GetComponent<Button>();
            if (showAllBtn == null) showAllBtn = CreateConsoleButton("BtnShowAll", canvasTrans, new Vector2(sysStartX + 5 * sysSpacingX, r2Y), new Vector2(sysBW, sysBH), "Show All Systems", font, 16, new Color(0.12f, 0.40f, 0.48f)).GetComponent<Button>();
        }

        private GameObject CreateConsoleButton(string name, Transform parent, Vector2 pos, Vector2 size, string text, Font font, int fontSize, Color baseColor)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = go.AddComponent<Image>();
            img.color = baseColor;

            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock cb = btn.colors;
            cb.highlightedColor = baseColor * 1.45f;
            cb.pressedColor = baseColor * 1.8f;
            btn.colors = cb;

            BoxCollider col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(size.x, size.y, 10f);

            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            RectTransform lrt = labelGO.AddComponent<RectTransform>();
            lrt.anchoredPosition = Vector2.zero;
            lrt.sizeDelta = size;

            Text labelText = labelGO.AddComponent<Text>();
            labelText.font = font;
            labelText.fontSize = fontSize;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = Color.white;
            labelText.text = text;

            return go;
        }

        private void FindControllers()
        {
            if (explodedViewController == null) explodedViewController = FindFirstObjectByType<ExplodedViewController>();
            if (xrayViewController == null) xrayViewController = FindFirstObjectByType<XRayViewController>();
            if (isolationController == null) isolationController = FindFirstObjectByType<OrganIsolationController>();
        }

        private void InitButtons()
        {
            if (explodeBtn != null)
            {
                explodeBtn.onClick.AddListener(ToggleExplodedView);
            }

            if (xrayBtn != null)
            {
                xrayBtn.onClick.AddListener(ToggleXRayView);
            }

            if (vascularBtn != null)
            {
                vascularBtn.onClick.AddListener(ToggleVascularView);
            }

            if (thoracicBtn != null)
            {
                thoracicBtn.onClick.AddListener(ToggleThoracicView);
            }

            if (resetAnatomyBtn != null)
            {
                resetAnatomyBtn.onClick.AddListener(ResetAnatomy);
            }

            if (skeletalBtn != null) skeletalBtn.onClick.AddListener(() => ToggleSystem("Skeletal"));
            if (cardioBtn != null) cardioBtn.onClick.AddListener(() => ToggleSystem("Cardiovascular"));
            if (respBtn != null) respBtn.onClick.AddListener(() => ToggleSystem("Respiratory"));
            if (nervousBtn != null) nervousBtn.onClick.AddListener(() => ToggleSystem("Nervous"));
            if (digestiveBtn != null) digestiveBtn.onClick.AddListener(() => ToggleSystem("Digestive"));

            if (showAllBtn != null)
            {
                showAllBtn.onClick.AddListener(() =>
                {
                    if (BodySystemManager.Instance != null)
                    {
                        BodySystemManager.Instance.ShowAllSystems();
                        PlaySound(clickClip, 0.8f);
                    }
                });
            }

            if (returnToSocketBtn != null)
            {
                returnToSocketBtn.onClick.AddListener(() =>
                {
                    if (activeExaminedOrgan != null)
                    {
                        activeExaminedOrgan.SmoothReturnToSocket();
                        PlaySound(resetClip, 0.9f);
                    }
                });
            }
        }

        public void ToggleExplodedView()
        {
            if (explodedViewController == null) FindControllers();
            if (explodedViewController != null)
            {
                explodedViewController.ToggleExplodedView();
                if (explodeBtnText != null)
                {
                    explodeBtnText.text = explodedViewController.IsExploded ? "Assembly View" : "Exploded View";
                }
                PlaySound(clickClip, 0.8f);
            }
        }

        public void ToggleXRayView()
        {
            if (xrayViewController == null) FindControllers();
            if (xrayViewController != null)
            {
                xrayViewController.ToggleXRay();
                UpdateModeButtonStates();
                PlaySound(clickClip, 0.8f);
            }
        }

        public void ToggleVascularView()
        {
            if (xrayViewController == null) FindControllers();
            if (xrayViewController != null)
            {
                xrayViewController.ToggleVascularMode();
                UpdateModeButtonStates();
                PlaySound(clickClip, 0.8f);
            }
        }

        public void ToggleThoracicView()
        {
            if (xrayViewController == null) FindControllers();
            if (xrayViewController != null)
            {
                xrayViewController.ToggleThoracicMode();
                UpdateModeButtonStates();
                PlaySound(clickClip, 0.8f);
            }
        }

        public void UpdateModeButtonStates()
        {
            if (xrayViewController == null) return;

            var mode = xrayViewController.CurrentMode;

            if (xrayBtnText != null)
            {
                xrayBtnText.text = (mode == XRayViewController.MedicalVisualizationMode.XRay) ? "Normal View" : "X-Ray Mode";
            }

            if (vascularBtnText != null)
            {
                vascularBtnText.text = (mode == XRayViewController.MedicalVisualizationMode.Vascular) ? "Exit Vascular" : "Vascular Angiogram";
            }

            if (thoracicBtnText != null)
            {
                thoracicBtnText.text = (mode == XRayViewController.MedicalVisualizationMode.ThoracicTranslucent) ? "Exit Thoracic" : "Thoracic Silhouette";
            }

            // Sync with VRClinicalTablet if present in scene
            if (VRClinicalTablet.Instance != null)
            {
                VRClinicalTablet.Instance.SyncModeStates(mode);
            }
        }

        public void ResetAnatomy()
        {
            if (explodedViewController == null) FindControllers();
            if (explodedViewController != null && explodedViewController.IsExploded)
            {
                explodedViewController.ResetExplosion();
            }

            if (xrayViewController != null)
            {
                xrayViewController.ResetToNormal();
            }

            if (isolationController != null && isolationController.IsInIsolationMode)
            {
                isolationController.ExitIsolation();
            }

            if (BodySystemManager.Instance != null)
            {
                BodySystemManager.Instance.ShowAllSystems();
            }

            // Snap all organs back to socket
            VRGrabbableOrgan[] allOrgans = FindObjectsByType<VRGrabbableOrgan>(FindObjectsSortMode.None);
            foreach (var organ in allOrgans)
            {
                organ.SmoothReturnToSocket();
            }

            if (explodeBtnText != null) explodeBtnText.text = "Exploded View";
            UpdateModeButtonStates();

            PlaySound(resetClip, 1f);
            UpdateCardDefault();
        }

        private void ToggleSystem(string systemName)
        {
            if (BodySystemManager.Instance != null)
            {
                BodySystemManager.Instance.ToggleSystem(systemName);
                PlaySound(clickClip, 0.8f);
            }
        }

        public void DisplayOrganMetrics(OrganInteractable data, VRGrabbableOrgan grabbable)
        {
            activeExaminedOrgan = grabbable;

            if (data == null)
            {
                UpdateCardDefault();
                return;
            }

            if (cardOrganNameText != null) cardOrganNameText.text = data.organName;
            if (cardSystemText != null) cardSystemText.text = $"System: <color=#00FFAA>{data.bodySystem}</color>  |  Mass: <color=#FFDD44>{data.normalWeight} g</color>";
            if (cardPhysiologyText != null) cardPhysiologyText.text = $"<b>Physiology:</b> {data.clinicalFunction}";
            if (cardVascularText != null) cardVascularText.text = $"<b>Blood Supply:</b> {data.arterialSupply}";
            if (cardPathologyText != null) cardPathologyText.text = $"<b>Clinical Pathology:</b> {data.commonPathology}";

            if (returnToSocketBtn != null) returnToSocketBtn.gameObject.SetActive(true);
        }

        public void UpdateCardDefault()
        {
            activeExaminedOrgan = null;
            if (cardOrganNameText != null) cardOrganNameText.text = "Central Anatomy Specimen";
            if (cardSystemText != null) cardSystemText.text = "Grab an organ with your VR controller grip to inspect live metrics.";
            if (cardPhysiologyText != null) cardPhysiologyText.text = "<b>Exam Tip:</b> Point your controller laser at any organ to highlight, or middle-click (grip) to extract and inspect in 3D.";
            if (cardVascularText != null) cardVascularText.text = "<b>Sockets:</b> Releasing near the body will magnetically snap the organ into its correct anatomical socket.";
            if (cardPathologyText != null) cardPathologyText.text = "<b>Stations:</b> Visit the Left Kiosk for Quizzes & Cases, or the Right Pod for Dr. Nova AI.";
            if (returnToSocketBtn != null) returnToSocketBtn.gameObject.SetActive(false);
        }

        private void PlaySound(AudioClip clip, float volume = 1f)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip, volume);
            }
        }
    }
}
