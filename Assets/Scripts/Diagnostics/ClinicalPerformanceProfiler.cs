using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace ImmersiveAnatomy.Diagnostics
{
    public class ClinicalPerformanceProfiler : MonoBehaviour
    {
        [Header("Profiler Configuration")]
        [SerializeField] private bool showProfilerGUI = false;
        [SerializeField] private KeyCode toggleKey = KeyCode.P;
        [SerializeField] private KeyCode altToggleKey = KeyCode.F3;
        [SerializeField] private float updateInterval = 0.5f;

        [Header("Geometry Metrics (Phase 7.3 Audited)")]
        public int anatomicalVertexCount = 47720;
        public int anatomicalTriangleCount = 34956;
        public int activeModelCount = 6;

        // Frame timing metrics
        private float accumTime = 0f;
        private int frameCount = 0;
        private float currentFps = 90f;
        private float currentFrameTimeMs = 11.1f;
        private float minFps = 999f;
        private float maxFps = 0f;

        // References
        private XRayViewController visualizationController;
        private GUIStyle panelStyle;
        private GUIStyle labelStyle;
        private GUIStyle headerStyle;
        private bool stylesInitialized = false;

        public float CurrentFPS => currentFps;
        public float CurrentFrameTimeMs => currentFrameTimeMs;
        public bool IsVisible => showProfilerGUI;

        private void Start()
        {
            visualizationController = FindFirstObjectByType<XRayViewController>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(altToggleKey))
            {
                showProfilerGUI = !showProfilerGUI;
            }

            accumTime += Time.unscaledDeltaTime;
            frameCount++;

            if (accumTime >= updateInterval)
            {
                currentFps = frameCount / accumTime;
                currentFrameTimeMs = (accumTime / frameCount) * 1000f;

                if (currentFps < minFps && Time.timeSinceLevelLoad > 2f) minFps = currentFps;
                if (currentFps > maxFps) maxFps = currentFps;

                accumTime = 0f;
                frameCount = 0;
            }
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            Texture2D bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, new Color(0.04f, 0.08f, 0.12f, 0.88f));
            bgTex.Apply();

            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.normal.background = bgTex;
            panelStyle.padding = new RectOffset(14, 14, 12, 12);

            headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 13;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = new Color(0.0f, 0.9f, 1.0f);

            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 11;
            labelStyle.normal.textColor = new Color(0.9f, 0.95f, 1.0f);

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (!showProfilerGUI) return;
            InitStyles();

            int panelW = 340;
            int panelH = 220;
            int posX = Screen.width - panelW - 20;
            int posY = 65;

            GUILayout.BeginArea(new Rect(posX, posY, panelW, panelH), panelStyle);

            GUILayout.Label("CLINICAL RENDERING DIAGNOSTICS", headerStyle);
            GUILayout.Space(4);

            string fpsColor = currentFps >= 85f ? "#00FF88" : (currentFps >= 60f ? "#FFCC00" : "#FF4444");
            GUILayout.Label($"<b>Framerate:</b> <color={fpsColor}>{currentFps:F1} FPS</color> ({currentFrameTimeMs:F2} ms)", labelStyle);
            GUILayout.Label($"<b>Target XR Budget:</b> 90.0 FPS (11.11 ms)", labelStyle);
            GUILayout.Label($"<b>FPS Range:</b> Min {minFps:F0} / Max {maxFps:F0}", labelStyle);
            GUILayout.Space(4);

            GUILayout.Label($"<b>Anatomical Models:</b> {activeModelCount} high-poly organs", labelStyle);
            GUILayout.Label($"<b>Geometry Load:</b> {anatomicalVertexCount:N0} verts / {anatomicalTriangleCount:N0} tris", labelStyle);
            GUILayout.Label($"<b>SRP Batcher:</b> {(GraphicsSettings.useScriptableRenderPipelineBatching ? "<color=#00FF88>ACTIVE</color>" : "<color=#FF4444>OFF</color>")}", labelStyle);

            string activeMode = "Normal PBR";
            if (visualizationController != null)
            {
                activeMode = visualizationController.CurrentMode.ToString();
            }
            GUILayout.Label($"<b>Diagnostic Mode:</b> <color=#00FFFF>{activeMode}</color>", labelStyle);

            string xrStatus = XRSettings.isDeviceActive ? $"Active ({XRSettings.loadedDeviceName})" : "Desktop Simulation";
            GUILayout.Label($"<b>XR Subsystem:</b> {xrStatus}", labelStyle);

            GUILayout.EndArea();
        }

        public void SetVisible(bool visible)
        {
            showProfilerGUI = visible;
        }
    }
}
