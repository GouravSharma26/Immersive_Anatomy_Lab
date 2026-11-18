#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ImmersiveAnatomy.Editor
{
    public static class ReleaseScreenshotCapture
    {
        private const string TRIGGER_FILE = "scratch/capture_screenshots.flag";
        private static string TriggerPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../", TRIGGER_FILE));

        [InitializeOnLoadMethod]
        private static void InitWatcher()
        {
            EditorApplication.update -= CheckTrigger;
            EditorApplication.update += CheckTrigger;
        }

        private static void CheckTrigger()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (File.Exists(TriggerPath))
            {
                try { File.Delete(TriggerPath); } catch { }
                EditorApplication.delayCall += CaptureScreenshots;
            }
        }

        [MenuItem("Tools/Dual Platform Release/5. Capture Release Screenshots (1080p)")]
        public static void CaptureScreenshots()
        {
            string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Documentation/Release/Images"));
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            Camera cam = Camera.main;
            if (cam == null) cam = Object.FindAnyObjectByType<Camera>();
            if (cam == null)
            {
                Debug.LogError("[ReleaseScreenshotCapture] No active Camera found in scene!");
                return;
            }

            Vector3 origPos = cam.transform.position;
            Quaternion origRot = cam.transform.rotation;
            float origFov = cam.fieldOfView;

            int width = 1920;
            int height = 1080;
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture prevRT = cam.targetTexture;
            RenderTexture.active = rt;
            cam.targetTexture = rt;
            cam.fieldOfView = 60f;

            // 1. Cover Art: Wide Architectural Lab Overview
            cam.transform.position = new Vector3(0f, 2.3f, -3.4f);
            cam.transform.LookAt(new Vector3(0f, 1.15f, 1.4f));
            cam.Render();
            SaveRT(rt, Path.Combine(outDir, "cover_art_lab_overview.png"));

            // 2. Dissection Table Close-up: Anatomical specimens (Heart, Lungs, Liver, Skeleton)
            cam.transform.position = new Vector3(0f, 1.65f, 0.35f);
            cam.transform.LookAt(new Vector3(0f, 1.05f, 1.45f));
            cam.Render();
            SaveRT(rt, Path.Combine(outDir, "screenshot_dissection_table.png"));

            // 3. VR Dissection Console: Physical console buttons & UI
            cam.transform.position = new Vector3(0f, 1.35f, 0.40f);
            cam.transform.LookAt(new Vector3(0f, 0.95f, 0.95f));
            cam.Render();
            SaveRT(rt, Path.Combine(outDir, "screenshot_vr_dissection_console.png"));

            // 4. Comparison Pedestals: Left/Right comparative anatomy stations
            cam.transform.position = new Vector3(0f, 1.75f, -0.3f);
            cam.transform.LookAt(new Vector3(0f, 1.0f, 1.25f));
            cam.Render();
            SaveRT(rt, Path.Combine(outDir, "screenshot_comparison_pedestals.png"));

            // 5. Medical Visualization Angle: Perspective on Thoracic & Vascular organs
            cam.transform.position = new Vector3(0.95f, 1.55f, 0.55f);
            cam.transform.LookAt(new Vector3(0f, 1.12f, 1.45f));
            cam.Render();
            SaveRT(rt, Path.Combine(outDir, "screenshot_medical_visualization.png"));

            // 6. Clinical Tablet & Study Station
            cam.transform.position = new Vector3(-0.55f, 1.45f, 0.55f);
            cam.transform.LookAt(new Vector3(-0.85f, 1.05f, 1.25f));
            cam.Render();
            SaveRT(rt, Path.Combine(outDir, "screenshot_clinical_tablet.png"));

            // Restore camera state
            cam.targetTexture = prevRT;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            cam.transform.position = origPos;
            cam.transform.rotation = origRot;
            cam.fieldOfView = origFov;

            Debug.Log("<color=#00FF00><b>[ReleaseScreenshotCapture] All 6 high-res 1080p release screenshots captured successfully!</b></color>");
        }

        private static void SaveRT(RenderTexture rt, string filePath)
        {
            Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(filePath, bytes);
            Object.DestroyImmediate(tex);
            Debug.Log($"[ReleaseScreenshotCapture] Saved: {Path.GetFileName(filePath)} ({bytes.Length:N0} bytes)");
        }
    }
}
#endif
