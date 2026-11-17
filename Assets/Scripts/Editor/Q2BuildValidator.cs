#if UNITY_EDITOR
using System;
using System.IO;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ImmersiveAnatomy.Editor
{
    public static class Q2BuildValidator
    {
        private const string MAIN_SCENE = "Assets/Scenes/VR_MainLab.unity";
        private const string REPORT_PATH = "scratch/build_validation_report.json";
        private const string TRIGGER_FILE = "scratch/run_build_validation.flag";

        [MenuItem("Tools/Dual Platform Release/Run Q2 Build Validation (Windows + Quest)")]
        public static void RunBuildValidation()
        {
            Debug.Log("<color=#00FFAA><b>[Q2 Build Validation] Starting independent validation builds...</b></color>");

            // --- 1. WINDOWS DESKTOP BUILD ---
            string winDir = "Builds/Windows";
            if (!Directory.Exists(winDir)) Directory.CreateDirectory(winDir);
            string winExe = Path.Combine(winDir, "ImmersiveAnatomyLab.exe");

            Debug.Log("[Q2 Build Validation] Starting Windows Desktop x64 Build...");
            Stopwatch winSw = Stopwatch.StartNew();

            BuildPlayerOptions winOptions = new BuildPlayerOptions
            {
                scenes = new string[] { MAIN_SCENE },
                locationPathName = winExe,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.Development
            };

            BuildReport winReport = BuildPipeline.BuildPlayer(winOptions);
            winSw.Stop();

            bool winSuccess = (winReport.summary.result == BuildResult.Succeeded);
            long winSize = 0;
            if (File.Exists(winExe))
            {
                FileInfo fi = new FileInfo(winExe);
                winSize = fi.Length;
            }

            Debug.Log($"[Q2 Build Validation] Windows Build {(winSuccess ? "SUCCEEDED" : "FAILED")} in {winSw.ElapsedMilliseconds} ms. Size: {winSize} bytes. Errors: {winReport.summary.totalErrors}");

            // --- 2. QUEST ANDROID BUILD ---
            string questDir = "Builds/Quest";
            if (!Directory.Exists(questDir)) Directory.CreateDirectory(questDir);
            string questApk = Path.Combine(questDir, "ImmersiveAnatomyLab-Quest-v1.0.0.apk");

            Debug.Log("[Q2 Build Validation] Starting Meta Quest Android Build...");
            Stopwatch questSw = Stopwatch.StartNew();

            BuildPlayerOptions questOptions = new BuildPlayerOptions
            {
                scenes = new string[] { MAIN_SCENE },
                locationPathName = questApk,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development
            };

            BuildReport questReport = BuildPipeline.BuildPlayer(questOptions);
            questSw.Stop();

            bool questSuccess = (questReport.summary.result == BuildResult.Succeeded);
            long questSize = 0;
            if (File.Exists(questApk))
            {
                FileInfo fi = new FileInfo(questApk);
                questSize = fi.Length;
            }

            Debug.Log($"[Q2 Build Validation] Quest Build {(questSuccess ? "SUCCEEDED" : "FAILED")} in {questSw.ElapsedMilliseconds} ms. Size: {questSize} bytes. Errors: {questReport.summary.totalErrors}");

            // --- 3. WRITE REPORT JSON ---
            string json = $@"{{
  ""windows"": {{
    ""success"": {winSuccess.ToString().ToLower()},
    ""duration_ms"": {winSw.ElapsedMilliseconds},
    ""output_path"": ""{winExe.Replace("\\", "/")}"",
    ""file_size_bytes"": {winSize},
    ""total_errors"": {winReport.summary.totalErrors},
    ""total_warnings"": {winReport.summary.totalWarnings}
  }},
  ""quest"": {{
    ""success"": {questSuccess.ToString().ToLower()},
    ""duration_ms"": {questSw.ElapsedMilliseconds},
    ""output_path"": ""{questApk.Replace("\\", "/")}"",
    ""file_size_bytes"": {questSize},
    ""total_errors"": {questReport.summary.totalErrors},
    ""total_warnings"": {questReport.summary.totalWarnings}
  }}
}}";

            string reportDir = Path.GetDirectoryName(REPORT_PATH);
            if (!Directory.Exists(reportDir)) Directory.CreateDirectory(reportDir);
            File.WriteAllText(REPORT_PATH, json);

            Debug.Log($"<color=#00FFAA><b>[Q2 Build Validation] Validation report written to {REPORT_PATH}</b></color>");
        }

        [InitializeOnLoadMethod]
        private static void CheckAutoTrigger()
        {
            if (File.Exists(TRIGGER_FILE))
            {
                File.Delete(TRIGGER_FILE);
                EditorApplication.delayCall += RunBuildValidation;
            }
        }
    }
}
#endif
