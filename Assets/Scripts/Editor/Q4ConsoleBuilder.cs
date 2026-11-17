#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using ImmersiveAnatomy.VR;

namespace ImmersiveAnatomy.Editor
{
    public static class Q4ConsoleBuilder
    {
        private const string SCENE_PATH = "Assets/Scenes/VR_MainLab.unity";

        [MenuItem("Tools/Dual Platform Release/Configure Q4 Console & Tablet Controls")]
        public static void BuildConsoleAndTablet()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);

            // 1. Locate or create Dissection Table Console Root
            GameObject consoleRoot = GameObject.Find("DissectionTableConsole");
            if (consoleRoot != null)
            {
                Object.DestroyImmediate(consoleRoot);
            }
            consoleRoot = new GameObject("DissectionTableConsole");
            
            // Position on front rail of DissectionTable (Table center: 0, 0.45, 1.5; Table top Y=0.90, Front Z=0.95)
            consoleRoot.transform.position = new Vector3(0f, 0.92f, 0.95f);
            consoleRoot.transform.rotation = Quaternion.Euler(35f, 0f, 0f); // 35-degree ergonomic tilt towards player

            // Physical console backing plate
            GameObject backing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backing.name = "ConsolePlate";
            backing.transform.SetParent(consoleRoot.transform, false);
            backing.transform.localPosition = new Vector3(0f, -0.015f, 0f);
            backing.transform.localScale = new Vector3(1.4f, 0.025f, 0.35f);

            var plateRend = backing.GetComponent<MeshRenderer>();
            if (plateRend != null)
            {
                Material plateMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                plateMat.name = "M_ConsolePlate";
                plateMat.color = new Color(0.06f, 0.09f, 0.14f);
                plateMat.SetFloat("_Metallic", 0.85f);
                plateMat.SetFloat("_Smoothness", 0.5f);
                plateRend.sharedMaterial = plateMat;
            }

            // World Canvas for buttons (1400 x 350 px -> 1.4m x 0.35m at scale 0.001)
            GameObject canvasGO = new GameObject("ConsoleCanvas");
            canvasGO.transform.SetParent(consoleRoot.transform, false);
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
            canvasGO.AddComponent<TrackedDeviceGraphicRaycaster>(); // Allows Meta Quest controller ray to click

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Attach VRDissectionTableConsole component
            VRDissectionTableConsole consoleScript = consoleRoot.AddComponent<VRDissectionTableConsole>();

            // Header Banner
            CreateText("ConsoleHeader", canvasGO.transform, new Vector2(0f, 135f), new Vector2(1360f, 35f),
                "DISSECTION TABLE MEDICAL VISUALIZATION & CONTROL CONSOLE", font, 18, FontStyle.Bold, new Color(0.3f, 0.85f, 1f), TextAnchor.MiddleCenter);

            // --- ROW 1: ADVANCED VIEW MODES ---
            // 5 buttons across: Exploded, X-Ray, Vascular, Thoracic, Reset
            float r1Y = 70f;
            float bW = 250f;
            float bH = 55f;
            float spacingX = 265f;
            float startX = -2f * spacingX;

            // Exploded View Button
            var explodeBtnGO = CreateConsoleButton("ExplodedViewButton", canvasGO.transform, new Vector2(startX + 0 * spacingX, r1Y), new Vector2(bW, bH),
                "Exploded View", font, 18, new Color(0.18f, 0.28f, 0.45f));
            consoleScript.explodeBtn = explodeBtnGO.GetComponent<Button>();
            consoleScript.explodeBtnText = explodeBtnGO.GetComponentInChildren<Text>();

            // X-Ray View Button
            var xrayBtnGO = CreateConsoleButton("XRayViewButton", canvasGO.transform, new Vector2(startX + 1 * spacingX, r1Y), new Vector2(bW, bH),
                "X-Ray Mode", font, 18, new Color(0.12f, 0.35f, 0.50f));
            consoleScript.xrayBtn = xrayBtnGO.GetComponent<Button>();
            consoleScript.xrayBtnText = xrayBtnGO.GetComponentInChildren<Text>();

            // Vascular Angiogram Button (Q4 Addition)
            var vascularBtnGO = CreateConsoleButton("VascularViewButton", canvasGO.transform, new Vector2(startX + 2 * spacingX, r1Y), new Vector2(bW, bH),
                "Vascular Angiogram", font, 18, new Color(0.50f, 0.15f, 0.22f));
            consoleScript.vascularBtn = vascularBtnGO.GetComponent<Button>();
            consoleScript.vascularBtnText = vascularBtnGO.GetComponentInChildren<Text>();

            // Thoracic Silhouette Button (Q4 Addition)
            var thoracicBtnGO = CreateConsoleButton("ThoracicViewButton", canvasGO.transform, new Vector2(startX + 3 * spacingX, r1Y), new Vector2(bW, bH),
                "Thoracic Silhouette", font, 18, new Color(0.15f, 0.40f, 0.45f));
            consoleScript.thoracicBtn = thoracicBtnGO.GetComponent<Button>();
            consoleScript.thoracicBtnText = thoracicBtnGO.GetComponentInChildren<Text>();

            // Reset Anatomy Button
            var resetBtnGO = CreateConsoleButton("ResetAnatomyButton", canvasGO.transform, new Vector2(startX + 4 * spacingX, r1Y), new Vector2(bW, bH),
                "Reset Anatomy", font, 18, new Color(0.15f, 0.45f, 0.28f));
            consoleScript.resetAnatomyBtn = resetBtnGO.GetComponent<Button>();

            // --- ROW 2: SYSTEM TOGGLES ---
            float r2Y = -15f;
            float sysBW = 205f;
            float sysBH = 48f;
            float sysSpacingX = 220f;
            float sysStartX = -2.5f * sysSpacingX;

            var skeletalGO = CreateConsoleButton("BtnSkeletal", canvasGO.transform, new Vector2(sysStartX + 0 * sysSpacingX, r2Y), new Vector2(sysBW, sysBH), "Skeletal System", font, 16, new Color(0.20f, 0.25f, 0.35f));
            consoleScript.skeletalBtn = skeletalGO.GetComponent<Button>();

            var cardioGO = CreateConsoleButton("BtnCardio", canvasGO.transform, new Vector2(sysStartX + 1 * sysSpacingX, r2Y), new Vector2(sysBW, sysBH), "Cardiovascular", font, 16, new Color(0.20f, 0.25f, 0.35f));
            consoleScript.cardioBtn = cardioGO.GetComponent<Button>();

            var respGO = CreateConsoleButton("BtnResp", canvasGO.transform, new Vector2(sysStartX + 2 * sysSpacingX, r2Y), new Vector2(sysBW, sysBH), "Respiratory", font, 16, new Color(0.20f, 0.25f, 0.35f));
            consoleScript.respBtn = respGO.GetComponent<Button>();

            var nervousGO = CreateConsoleButton("BtnNervous", canvasGO.transform, new Vector2(sysStartX + 3 * sysSpacingX, r2Y), new Vector2(sysBW, sysBH), "Nervous System", font, 16, new Color(0.20f, 0.25f, 0.35f));
            consoleScript.nervousBtn = nervousGO.GetComponent<Button>();

            var digestiveGO = CreateConsoleButton("BtnDigestive", canvasGO.transform, new Vector2(sysStartX + 4 * sysSpacingX, r2Y), new Vector2(sysBW, sysBH), "Digestive System", font, 16, new Color(0.20f, 0.25f, 0.35f));
            consoleScript.digestiveBtn = digestiveGO.GetComponent<Button>();

            var showAllGO = CreateConsoleButton("BtnShowAll", canvasGO.transform, new Vector2(sysStartX + 5 * sysSpacingX, r2Y), new Vector2(sysBW, sysBH), "Show All Systems", font, 16, new Color(0.12f, 0.40f, 0.48f));
            consoleScript.showAllBtn = showAllGO.GetComponent<Button>();

            // --- ROW 3: METRIC HINT FOOTER ---
            CreateText("ConsoleFooter", canvasGO.transform, new Vector2(0f, -85f), new Vector2(1360f, 30f),
                "Interact using Quest Controller Laser Ray / Windows Mouse Click | [8] Vascular  [9] Thoracic  [Z] X-Ray  [X] Explode", font, 15, FontStyle.Italic, new Color(0.6f, 0.8f, 0.9f), TextAnchor.MiddleCenter);

            // Wire controllers
            consoleScript.explodedViewController = Object.FindFirstObjectByType<ExplodedViewController>();
            consoleScript.xrayViewController = Object.FindFirstObjectByType<XRayViewController>();
            consoleScript.isolationController = Object.FindFirstObjectByType<OrganIsolationController>();

            // Audio source
            AudioSource aSrc = consoleRoot.AddComponent<AudioSource>();
            aSrc.playOnAwake = false;
            consoleScript.audioSource = aSrc;

            EditorUtility.SetDirty(consoleRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("<color=#00FFAA><b>[Q4ConsoleBuilder] Dissection Table Console with Vascular & Thoracic buttons successfully built & saved!</b></color>");
        }

        private const string TRIGGER_FILE = "scratch/run_q4_console_build.flag";

        [InitializeOnLoadMethod]
        private static void CheckAutoTrigger()
        {
            if (File.Exists(TRIGGER_FILE))
            {
                File.Delete(TRIGGER_FILE);
                EditorApplication.delayCall += BuildConsoleAndTablet;
            }
        }

        private static GameObject CreateConsoleButton(string name, Transform parent, Vector2 pos, Vector2 size, string text, Font font, int fontSize, Color baseColor)
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

            // Box collider for raycast precision
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

        private static Text CreateText(string name, Transform parent, Vector2 pos, Vector2 size, string text, Font font, int fontSize, FontStyle style, Color color, TextAnchor align)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Text t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.color = color;
            t.alignment = align;
            t.text = text;
            return t;
        }
    }
}
#endif
