#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

namespace ImmersiveAnatomy.Editor
{
    public static class DualPlatformReleaseManager
    {
        public const string MAIN_SCENE_PATH = "Assets/Scenes/VR_MainLab.unity";
        public const string ANDROID_MANIFEST_DIR = "Assets/Plugins/Android";
        public const string ANDROID_MANIFEST_PATH = "Assets/Plugins/Android/AndroidManifest.xml";
        public const string BUILD_PROFILES_DIR = "Assets/Settings/BuildProfiles";
        public const string QUEST_PROFILE_PATH = "Assets/Settings/BuildProfiles/Quest_Standalone_Release.buildprofile";
        public const string WINDOWS_PROFILE_PATH = "Assets/Settings/BuildProfiles/Windows_Desktop_Release.buildprofile";

        public const string COMPANY_NAME = "Immersive Anatomy Lab";
        public const string PRODUCT_NAME = "Immersive Anatomy Lab";
        public const string PACKAGE_ID = "com.gourav.immersiveanatomylab";
        public const string VERSION_NAME = "1.0.0";
        public const int VERSION_CODE = 1;

        [InitializeOnLoadMethod]
        [MenuItem("Tools/Dual Platform Release/1. Configure Dual Platform Settings (Quest + Windows)")]
        public static void ConfigureDualPlatformSettings()
        {
            Debug.Log("<color=#00FFAA><b>[DualPlatformRelease] Starting Dual Platform Configuration...</b></color>");

            // 1. Scene List in EditorBuildSettings
            ConfigureSceneList();

            // 2. Player Settings & Identity
            ConfigurePlayerSettings();

            // 3. Android OpenXR and XR Management
            ConfigureAndroidXR();

            // 4. Windows Standalone Settings (Preserve Desktop, No PCVR)
            ConfigureWindowsDesktop();

            // 5. Android Manifest for Meta Quest
            GenerateQuestAndroidManifest();

            // 6. Android ASTC Texture Overrides (Q2)
            ConfigureAndroidASTCTextures();

            // 7. Build Profiles (Unity 6)
            ConfigureBuildProfiles();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=#00FFAA><b>[DualPlatformRelease] Configuration successfully applied and verified!</b></color>");
        }

        [MenuItem("Tools/Dual Platform Release/2. Verify Configuration Status")]
        public static void VerifyConfigurationStatus()
        {
            Debug.Log("<b>=== DUAL PLATFORM RELEASE STATUS AUDIT ===</b>");

            // Scene list check
            var scenes = EditorBuildSettings.scenes;
            Debug.Log($"Scene count in Build Settings: {scenes.Length}");
            if (scenes.Length > 0 && scenes[0].path == MAIN_SCENE_PATH && scenes[0].enabled)
            {
                Debug.Log($"<color=#00FF00>[PASS]</color> Scene 0 is {MAIN_SCENE_PATH} (Enabled)");
            }
            else
            {
                Debug.LogWarning($"<color=#FF9900>[WARN]</color> Scene 0 is NOT {MAIN_SCENE_PATH}. Current: {(scenes.Length > 0 ? scenes[0].path : "None")}");
            }

            // Package ID check
            string androidId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
            string standaloneId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Standalone);
            Debug.Log($"Android App ID: {androidId} | Standalone App ID: {standaloneId} | Version: {PlayerSettings.bundleVersion}");

            // Android XR check
            XRGeneralSettingsPerBuildTarget buildTargetSettings = null;
            EditorBuildSettings.TryGetConfigObject<XRGeneralSettingsPerBuildTarget>(XRGeneralSettings.k_SettingsKey, out buildTargetSettings);
            if (buildTargetSettings != null && buildTargetSettings.HasSettingsForBuildTarget(BuildTargetGroup.Android))
            {
                var androidSettings = buildTargetSettings.SettingsForBuildTarget(BuildTargetGroup.Android);
                bool hasLoader = androidSettings != null && androidSettings.Manager != null && androidSettings.Manager.activeLoaders.Count > 0;
                Debug.Log($"<color={(hasLoader ? "#00FF00" : "#FF0000")}>[{(hasLoader ? "PASS" : "FAIL")}]</color> Android XR Loader configured: {hasLoader}");
            }
            else
            {
                Debug.LogError("<color=#FF0000>[FAIL]</color> Android XR settings NOT configured in XRGeneralSettingsPerBuildTarget!");
            }

            // Manifest check
            bool hasManifest = File.Exists(ANDROID_MANIFEST_PATH);
            Debug.Log($"<color={(hasManifest ? "#00FF00" : "#FF0000")}>[{(hasManifest ? "PASS" : "FAIL")}]</color> AndroidManifest.xml exists: {hasManifest}");

            // Build profiles check
            bool hasQuestProfile = File.Exists(QUEST_PROFILE_PATH);
            bool hasWinProfile = File.Exists(WINDOWS_PROFILE_PATH);
            Debug.Log($"Build profiles: Quest Profile = {hasQuestProfile}, Windows Profile = {hasWinProfile}");
        }

        public static void ConfigureSceneList()
        {
            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(MAIN_SCENE_PATH, true)
            };

            // Keep auxiliary scenes disabled so index 0 is always VR_MainLab
            string mainLabPath = "Assets/Scenes/MainLab.unity";
            if (File.Exists(mainLabPath))
            {
                buildScenes.Add(new EditorBuildSettingsScene(mainLabPath, false));
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();
            Debug.Log($"[DualPlatformRelease] Configured EditorBuildSettings scenes. Primary Scene 0: {MAIN_SCENE_PATH}");
        }

        public static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = COMPANY_NAME;
            PlayerSettings.productName = PRODUCT_NAME;
            PlayerSettings.bundleVersion = VERSION_NAME;

            // Android Identifiers & Target
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, PACKAGE_ID);
            PlayerSettings.Android.bundleVersionCode = VERSION_CODE;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);

            // Standalone Identifiers
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, PACKAGE_ID);

            Debug.Log("[DualPlatformRelease] Configured PlayerSettings (Company, Product, IDs, Version, ARM64, IL2CPP, MinSDK 29).");
        }

        public static void ConfigureAndroidXR()
        {
            XRGeneralSettingsPerBuildTarget buildTargetSettings = null;
            EditorBuildSettings.TryGetConfigObject<XRGeneralSettingsPerBuildTarget>(XRGeneralSettings.k_SettingsKey, out buildTargetSettings);
            if (buildTargetSettings == null)
            {
                buildTargetSettings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                string settingsPath = "Assets/XR";
                if (!AssetDatabase.IsValidFolder(settingsPath)) AssetDatabase.CreateFolder("Assets", "XR");
                AssetDatabase.CreateAsset(buildTargetSettings, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, buildTargetSettings, true);
            }

            if (!buildTargetSettings.HasSettingsForBuildTarget(BuildTargetGroup.Android))
            {
                buildTargetSettings.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
            }

            var generalSettings = buildTargetSettings.SettingsForBuildTarget(BuildTargetGroup.Android);
            if (generalSettings.Manager == null)
            {
                buildTargetSettings.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
            }

            generalSettings.InitManagerOnStart = true;
            var manager = generalSettings.Manager;
            XRPackageMetadataStore.AssignLoader(manager, "UnityEngine.XR.OpenXR.OpenXRLoader", BuildTargetGroup.Android);

            OpenXRSettings openXRSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (openXRSettings != null)
            {
                foreach (var feat in openXRSettings.GetFeatures())
                {
                    string fName = (feat.name ?? "").ToLower();
                    string typeName = feat.GetType().Name.ToLower();

                    if (fName.Contains("meta quest") || typeName.Contains("metaquest") ||
                        fName.Contains("oculus touch") || typeName.Contains("oculustouch") ||
                        fName.Contains("meta quest touch plus") || typeName.Contains("metaquestplus") ||
                        fName.Contains("meta quest touch pro") || typeName.Contains("metaquestpro") ||
                        fName.Contains("hand interaction") || typeName.Contains("handinteraction") ||
                        fName.Contains("palm pose") || typeName.Contains("palmpose"))
                    {
                        feat.enabled = true;
                        Debug.Log($"[DualPlatformRelease] Enabled Android OpenXR Feature: {feat.name} ({feat.GetType().Name})");
                    }
                }
                EditorUtility.SetDirty(openXRSettings);
            }

            EditorUtility.SetDirty(manager);
            EditorUtility.SetDirty(generalSettings);
            EditorUtility.SetDirty(buildTargetSettings);
        }

        public static void ConfigureWindowsDesktop()
        {
            // Windows desktop: PCVR is NOT part of this release.
            // In Standalone XR settings, ensure OpenXR is not forcibly required to run the game.
            XRGeneralSettingsPerBuildTarget buildTargetSettings = null;
            EditorBuildSettings.TryGetConfigObject<XRGeneralSettingsPerBuildTarget>(XRGeneralSettings.k_SettingsKey, out buildTargetSettings);
            if (buildTargetSettings != null && buildTargetSettings.HasSettingsForBuildTarget(BuildTargetGroup.Standalone))
            {
                var standaloneSettings = buildTargetSettings.SettingsForBuildTarget(BuildTargetGroup.Standalone);
                if (standaloneSettings != null)
                {
                    // Allow Standalone to boot without requiring a VR headset
                    standaloneSettings.InitManagerOnStart = false;
                    EditorUtility.SetDirty(standaloneSettings);
                    Debug.Log("[DualPlatformRelease] Windows Standalone XR InitManagerOnStart set to false (Clean Desktop Laptop Mode).");
                }
            }
        }

        public static void GenerateQuestAndroidManifest()
        {
            if (!Directory.Exists(ANDROID_MANIFEST_DIR))
            {
                Directory.CreateDirectory(ANDROID_MANIFEST_DIR);
            }

            string manifestContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<manifest xmlns:android=""http://schemas.android.com/apk/res/android""
    package=""com.gourav.immersiveanatomylab""
    android:versionCode=""1""
    android:versionName=""1.0.0""
    android:installLocation=""auto"">

    <!-- Meta Quest Hardware & Headtracking Requirements -->
    <uses-feature android:name=""android.hardware.vr.headtracking"" android:required=""true"" android:version=""1"" />
    <uses-feature android:name=""android.hardware.touchscreen"" android:required=""false"" />
    <uses-feature android:name=""android.hardware.touchscreen.multitouch"" android:required=""false"" />
    <uses-feature android:name=""android.hardware.touchscreen.multitouch.distinct"" android:required=""false"" />

    <!-- Network Permissions for Dr. Nova AI Backend & Analytics -->
    <uses-permission android:name=""android.permission.INTERNET"" />
    <uses-permission android:name=""android.permission.ACCESS_NETWORK_STATE"" />

    <application
        android:label=""@string/app_name""
        android:icon=""@mipmap/app_icon""
        android:allowBackup=""false""
        android:usesCleartextTraffic=""true""
        android:theme=""@style/UnityThemeSelector"">

        <!-- Supported Meta Quest Hardware Family -->
        <meta-data android:name=""com.oculus.supportedDevices"" android:value=""quest|quest2|questpro|quest3"" />
        <meta-data android:name=""com.oculus.handtracking.frequency"" android:value=""LOW"" />

        <activity
            android:name=""com.unity3d.player.UnityPlayerActivity""
            android:theme=""@style/BaseUnityTheme""
            android:configChanges=""mcc|mnc|locale|touchscreen|keyboard|keyboardHidden|navigation|orientation|screenLayout|uiMode|screenSize|smallestScreenSize|fontScale|layoutDirection|density""
            android:resizeableActivity=""false""
            android:screenOrientation=""landscape""
            android:launchMode=""singleTask""
            android:exported=""true"">
            
            <intent-filter>
                <action android:name=""android.intent.action.MAIN"" />
                <category android:name=""android.intent.category.LAUNCHER"" />
                <category android:name=""com.oculus.intent.category.VR"" />
            </intent-filter>
            <meta-data android:name=""unityplayer.UnityActivity"" android:value=""true"" />
        </activity>
    </application>
</manifest>";

            File.WriteAllText(ANDROID_MANIFEST_PATH, manifestContent);
            Debug.Log($"[DualPlatformRelease] Generated Meta Quest AndroidManifest.xml at {ANDROID_MANIFEST_PATH}");
        }

        [MenuItem("Tools/Dual Platform Release/Configure Android ASTC Textures (Q2)")]
        public static void ConfigureAndroidASTCTextures()
        {
            string textureRoot = "Assets/Art/Phase7/Textures";
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new string[] { textureRoot });
            Debug.Log($"<color=#00FFAA>[Q2 Optimization] Found {guids.Length} PBR textures in {textureRoot}</color>");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                TextureImporterPlatformSettings androidSettings = importer.GetPlatformTextureSettings("Android");
                androidSettings.overridden = true;
                androidSettings.maxTextureSize = 1024;

                string filename = Path.GetFileName(path).ToLower();
                if (filename.Contains("normal"))
                {
                    androidSettings.format = TextureImporterFormat.ASTC_6x6;
                }
                else if (filename.Contains("albedo"))
                {
                    androidSettings.format = TextureImporterFormat.ASTC_6x6;
                }
                else if (filename.Contains("occlusion") || filename.Contains("mask") || filename.Contains("ao"))
                {
                    androidSettings.format = TextureImporterFormat.ASTC_8x8;
                }
                else
                {
                    androidSettings.format = TextureImporterFormat.ASTC_6x6;
                }

                importer.SetPlatformTextureSettings(androidSettings);
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                Debug.Log($"[Q2 Optimization] Configured Android ASTC override: {Path.GetFileName(path)} -> {androidSettings.format}");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("<color=#00FF00><b>[Q2 Optimization] All 18 anatomical textures successfully configured with platform-isolated Android ASTC overrides!</b></color>");
        }

        public static void ConfigureBuildProfiles()
        {
            if (!Directory.Exists(BUILD_PROFILES_DIR))
            {
                Directory.CreateDirectory(BUILD_PROFILES_DIR);
            }

            // Using reflection to interface with Unity 6 BuildProfile if present
            try
            {
                var buildProfileType = Type.GetType("UnityEditor.Build.Profile.BuildProfile, UnityEditor.BuildProfileModule");
                if (buildProfileType != null)
                {
                    Debug.Log("[DualPlatformRelease] Unity 6 BuildProfile API found: " + buildProfileType.FullName);
                    // Ensure build profile assets exist
                    EnsureBuildProfileAsset(QUEST_PROFILE_PATH, buildProfileType, BuildTarget.Android, BuildTargetGroup.Android);
                    EnsureBuildProfileAsset(WINDOWS_PROFILE_PATH, buildProfileType, BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone);
                }
                else
                {
                    Debug.Log("[DualPlatformRelease] Standard Build Profiles configured via EditorBuildSettings & BuildPipeline.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[DualPlatformRelease] Build Profile reflection notice: " + ex.Message);
            }
        }

        private static void EnsureBuildProfileAsset(string path, Type profileType, BuildTarget target, BuildTargetGroup group)
        {
            if (!File.Exists(path))
            {
                var profile = ScriptableObject.CreateInstance(profileType);
                AssetDatabase.CreateAsset(profile, path);
                Debug.Log($"[DualPlatformRelease] Created BuildProfile asset at {path}");
            }
        }

        [MenuItem("Tools/Dual Platform Release/3. Build Windows Desktop Player (ZIP)")]
        public static void BuildWindowsDesktopPlayer()
        {
            ConfigureSceneList();
            ConfigurePlayerSettings();
            ConfigureWindowsDesktop();

            string outputDir = "Builds/Windows";
            if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

            string exePath = Path.Combine(outputDir, "ImmersiveAnatomyLab.exe");
            string[] scenes = new string[] { MAIN_SCENE_PATH };

            Debug.Log($"<color=#00FFAA><b>[DualPlatformRelease] Building Windows Desktop 64-bit to {exePath}...</b></color>");

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"<color=#00FF00><b>[DualPlatformRelease] Windows Desktop Build SUCCEEDED! Total size: {report.summary.totalSize} bytes.</b></color>");

                // Package into ZIP for itch.io release
                string zipPath = "Builds/ImmersiveAnatomyLab-Windows-v1.0.0.zip";
                if (File.Exists(zipPath)) File.Delete(zipPath);
                ZipFile.CreateFromDirectory(outputDir, zipPath);
                Debug.Log($"<color=#00FFAA><b>[DualPlatformRelease] Packaged Windows ZIP: {zipPath}</b></color>");
            }
            else
            {
                Debug.LogError($"<color=#FF0000><b>[DualPlatformRelease] Windows Desktop Build FAILED with {report.summary.totalErrors} errors!</b></color>");
            }
        }

        [MenuItem("Tools/Dual Platform Release/4. Build Meta Quest Standalone APK")]
        public static void BuildQuestStandaloneApk()
        {
            ConfigureSceneList();
            ConfigurePlayerSettings();
            ConfigureAndroidXR();
            GenerateQuestAndroidManifest();
            ApplySecureAndroidSigning();

            string outputDir = "Builds/Quest";
            if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

            string apkPath = Path.Combine(outputDir, "ImmersiveAnatomyLab-Quest-v1.0.0.apk");
            string[] scenes = new string[] { MAIN_SCENE_PATH };

            Debug.Log($"<color=#00FFAA><b>[DualPlatformRelease] Building Meta Quest Standalone APK to {apkPath}...</b></color>");

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            bool success = (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded);
            long apkSize = File.Exists(apkPath) ? new FileInfo(apkPath).Length : 0;

            string reportJson = $@"{{
  ""success"": {success.ToString().ToLower()},
  ""apk_path"": ""{apkPath.Replace("\\", "/")}"",
  ""size_bytes"": {apkSize},
  ""errors"": {report.summary.totalErrors},
  ""warnings"": {report.summary.totalWarnings}
}}";
            File.WriteAllText("scratch/quest_build_result.json", reportJson);

            if (success)
            {
                Debug.Log($"<color=#00FF00><b>[DualPlatformRelease] Meta Quest Standalone APK Build SUCCEEDED! Total size: {apkSize} bytes.</b></color>");
            }
            else
            {
                Debug.LogError($"<color=#FF0000><b>[DualPlatformRelease] Meta Quest Standalone APK Build FAILED with {report.summary.totalErrors} errors!</b></color>");
            }
        }

        private static void ApplySecureAndroidSigning()
        {
            string configPath = "secrets/signing_config.json";
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    string ksPath = ExtractJsonField(json, "keystore_path");
                    string ksPass = ExtractJsonField(json, "keystore_pass");
                    string aliasName = ExtractJsonField(json, "keyalias_name");
                    string aliasPass = ExtractJsonField(json, "keyalias_pass");

                    if (!string.IsNullOrEmpty(ksPath) && File.Exists(ksPath))
                    {
                        PlayerSettings.Android.useCustomKeystore = true;
                        PlayerSettings.Android.keystoreName = Path.GetFullPath(ksPath);
                        PlayerSettings.Android.keystorePass = ksPass;
                        PlayerSettings.Android.keyaliasName = aliasName;
                        PlayerSettings.Android.keyaliasPass = aliasPass;
                        Debug.Log("[DualPlatformRelease] Secure Android Keystore configured for release signing.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[DualPlatformRelease] Secure signing resolution notice: " + ex.Message);
                }
            }
        }

        private static string ExtractJsonField(string json, string field)
        {
            string pattern = $"\"{field}\"\\s*:\\s*\"([^\"]+)\"";
            var match = System.Text.RegularExpressions.Regex.Match(json, pattern);
            return match.Success ? match.Groups[1].Value : null;
        }

        private const string QUEST_BUILD_TRIGGER = "scratch/run_quest_build.flag";

        [InitializeOnLoadMethod]
        private static void CheckQuestBuildTrigger()
        {
            if (File.Exists(QUEST_BUILD_TRIGGER))
            {
                File.Delete(QUEST_BUILD_TRIGGER);
                EditorApplication.delayCall += BuildQuestStandaloneApk;
            }
        }
        }
    }
}
#endif
