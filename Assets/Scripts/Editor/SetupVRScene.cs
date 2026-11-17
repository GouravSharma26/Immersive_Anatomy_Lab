#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.UI;
using ImmersiveAnatomy.Learning;
using ImmersiveAnatomy.VR;

public class SetupVRScene
{
    private const string VR_SCENE_PATH = "Assets/Scenes/VR_MainLab.unity";

    [MenuItem("Tools/Build Phase 6 VR OpenXR Scene")]
    public static void SetupAndConfigureVR()
    {
        Debug.Log("[Phase 6] Starting Multi-Station VR Lab Scene Configuration...");
        ConfigureOpenXRSettings();
        BuildVRMainLabScene();
        Debug.Log("[Phase 6] Multi-Station VR OpenXR Scene successfully built and configured!");
    }

    [MenuItem("Tools/Build Standalone Windows VR Player")]
    public static void BuildVRPlayer()
    {
        SetupAndConfigureVR();

        string buildDir = "Build";
        if (!Directory.Exists(buildDir)) Directory.CreateDirectory(buildDir);

        string exePath = Path.Combine(buildDir, "ImmersiveAnatomyLab_VR.exe");
        string[] scenes = new string[] { VR_SCENE_PATH };

        Debug.Log($"[Phase 6] Building Standalone Windows 64-bit VR Player to {exePath}...");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[Phase 6] VR Player build SUCCEEDED! Total size: {report.summary.totalSize} bytes");
        }
        else
        {
            Debug.LogError($"[Phase 6] VR Player build FAILED with {report.summary.totalErrors} errors!");
        }
    }

    public static void ConfigureOpenXRSettings()
    {
        XRGeneralSettingsPerBuildTarget buildTargetSettings = null;
        EditorBuildSettings.TryGetConfigObject<XRGeneralSettingsPerBuildTarget>(XRGeneralSettings.k_SettingsKey, out buildTargetSettings);
        if (buildTargetSettings == null)
        {
            buildTargetSettings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            string settingsPath = "Assets/XR";
            if (!AssetDatabase.IsValidFolder(settingsPath)) AssetDatabase.CreateFolder("Assets", "XR");
            string assetPath = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";
            AssetDatabase.CreateAsset(buildTargetSettings, assetPath);
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, buildTargetSettings, true);
        }

        if (!buildTargetSettings.HasSettingsForBuildTarget(BuildTargetGroup.Standalone))
        {
            buildTargetSettings.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Standalone);
        }

        var generalSettings = buildTargetSettings.SettingsForBuildTarget(BuildTargetGroup.Standalone);
        if (generalSettings.Manager == null)
        {
            buildTargetSettings.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Standalone);
        }

        generalSettings.InitManagerOnStart = true;
        var manager = generalSettings.Manager;
        XRPackageMetadataStore.AssignLoader(manager, "UnityEngine.XR.OpenXR.OpenXRLoader", BuildTargetGroup.Standalone);

        OpenXRSettings openXRSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
        if (openXRSettings != null)
        {
            foreach (var feat in openXRSettings.GetFeatures())
            {
                if (feat.name.Contains("Oculus Touch") ||
                    feat.name.Contains("Meta Quest") ||
                    feat.name.Contains("Eye Gaze") ||
                    feat.name.Contains("Hand Interaction") ||
                    feat.name.Contains("Mock Driver"))
                {
                    feat.enabled = true;
                }
            }
            EditorUtility.SetDirty(openXRSettings);
        }

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(generalSettings);
        EditorUtility.SetDirty(buildTargetSettings);
        AssetDatabase.SaveAssets();
    }

    public static void BuildVRMainLabScene()
    {
        Scene scene = EditorSceneManager.OpenScene(VR_SCENE_PATH, OpenSceneMode.Single);
        Debug.Log($"[Phase 6] Opened {VR_SCENE_PATH} for Multi-Station VR configuration.");

        // 1. Remove desktop Player and loose cameras
        GameObject desktopPlayer = GameObject.Find("Player");
        if (desktopPlayer != null) UnityEngine.Object.DestroyImmediate(desktopPlayer);

        Camera[] looseCams = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var c in looseCams)
        {
            if (c.gameObject.name == "Main Camera" && c.transform.parent == null)
            {
                UnityEngine.Object.DestroyImmediate(c.gameObject);
            }
        }

        // 2. Remove desktop UI canvas and old floating tablet
        GameObject desktopUI = GameObject.Find("UI");
        if (desktopUI != null) UnityEngine.Object.DestroyImmediate(desktopUI);

        GameObject oldTablet = GameObject.Find("VR_ClinicalTablet");
        if (oldTablet != null) UnityEngine.Object.DestroyImmediate(oldTablet);

        // 3. Setup Teleportation Floor & Room Environment
        SetupRoomEnvironment();

        // 4. Setup XR Origin Rig
        GameObject xrRig = SetupXROriginRig();

        // 5. Setup XR Device Simulator
        SetupXRDeviceSimulator();

        // 6. Equip Organs with VRGrabbableOrgan
        SetupGrabbableOrgans();

        // 7. Setup Central Dissection Table & Tabletop Console
        SetupCentralDissectionTableAndConsole();

        // 8. Setup Left Station: Clinical Quiz & Case Kiosk
        SetupQuizKioskStation();

        // 9. Setup Right Station: Dr. Nova AI Consultation Pod
        SetupAINovaPodStation();

        // 10. Setup Rear Station: Organ Comparison & Pathology Bay
        SetupComparisonBayStation();

        // 11. Setup Physical Dedicated Zones & Spatial Floor Triggers
        SetupZoneTriggersAndFloorPads();

        // 12. Ensure Backend Clients and Managers
        SetupManagers();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Phase 6] Saved {VR_SCENE_PATH} with multi-station layout!");
    }

    private static void SetupRoomEnvironment()
    {
        // Floor
        GameObject floor = GameObject.Find("Floor");
        if (floor == null)
        {
            floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
        }
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(1.6f, 1f, 1.6f); // 16m x 16m floor

        Material floorMat = SetupLabScene.CreateMaterial("Lab", "VR_FloorMat", new Color(0.06f, 0.08f, 0.12f), 0.9f, 0.2f, false, Color.black);
        floor.GetComponent<Renderer>().sharedMaterial = floorMat;

        TeleportationArea teleArea = floor.GetComponent<TeleportationArea>();
        if (teleArea == null) teleArea = floor.AddComponent<TeleportationArea>();

        // Walls
        EnsureWall("BackWall", new Vector3(0f, 3f, 7.5f), new Vector3(16f, 6f, 0.2f));
        EnsureWall("FrontWall", new Vector3(0f, 3f, -7.5f), new Vector3(16f, 6f, 0.2f));
        EnsureWall("LeftWall", new Vector3(-7.5f, 3f, 0f), new Vector3(0.2f, 6f, 16f));
        EnsureWall("RightWall", new Vector3(7.5f, 3f, 0f), new Vector3(0.2f, 6f, 16f));

        // Overhead Station Spotlights
        EnsureSpotlight("Spotlight_Dissection", new Vector3(0f, 4.5f, 1.4f), new Color(0.85f, 0.95f, 1f), 2.5f, 60f);
        EnsureSpotlight("Spotlight_Quiz", new Vector3(-2.8f, 4.5f, 1.2f), new Color(0.2f, 0.85f, 1f), 2.2f, 50f);
        EnsureSpotlight("Spotlight_AINova", new Vector3(2.8f, 4.5f, 1.2f), new Color(0.2f, 1f, 0.7f), 2.2f, 50f);
        EnsureSpotlight("Spotlight_Comparison", new Vector3(0f, 4.5f, -1.8f), new Color(1f, 0.6f, 0.9f), 2.2f, 50f);
    }

    private static void EnsureWall(string name, Vector3 pos, Vector3 scale)
    {
        GameObject wall = GameObject.Find(name);
        if (wall == null)
        {
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
        }
        wall.transform.position = pos;
        wall.transform.localScale = scale;
        Material wallMat = SetupLabScene.CreateMaterial("Lab", "VR_WallMat", new Color(0.12f, 0.15f, 0.22f), 0.7f, 0.1f, false, Color.black);
        wall.GetComponent<Renderer>().sharedMaterial = wallMat;
    }

    private static void EnsureSpotlight(string name, Vector3 pos, Color color, float intensity, float spotAngle)
    {
        GameObject lightGO = GameObject.Find(name);
        if (lightGO == null) lightGO = new GameObject(name);
        lightGO.transform.position = pos;
        lightGO.transform.rotation = Quaternion.Euler(85f, 0f, 0f);

        Light l = lightGO.GetComponent<Light>();
        if (l == null) l = lightGO.AddComponent<Light>();
        l.type = LightType.Spot;
        l.color = color;
        l.intensity = intensity;
        l.spotAngle = spotAngle;
        l.range = 8f;
    }

    private static GameObject SetupXROriginRig()
    {
        GameObject existingRig = GameObject.Find("XR Origin (XR Rig)") ?? GameObject.Find("XR Origin (VR)");
        if (existingRig != null) UnityEngine.Object.DestroyImmediate(existingRig);

        string prefabPath = "Assets/Samples/XR Interaction Toolkit/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
        GameObject rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        GameObject rigInstance = null;

        if (rigPrefab != null)
        {
            rigInstance = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
            rigInstance.name = "XR Origin (XR Rig)";
        }
        else
        {
            rigInstance = new GameObject("XR Origin (VR)");
            var origin = rigInstance.AddComponent<Unity.XR.CoreUtils.XROrigin>();
            rigInstance.AddComponent<XRInteractionManager>();

            GameObject cameraOffset = new GameObject("Camera Offset");
            cameraOffset.transform.SetParent(rigInstance.transform, false);

            GameObject mainCam = new GameObject("Main Camera");
            mainCam.transform.SetParent(cameraOffset.transform, false);
            mainCam.tag = "MainCamera";
            var cam = mainCam.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            mainCam.AddComponent<AudioListener>();

            origin.CameraFloorOffsetObject = cameraOffset;
            origin.Camera = cam;
        }

        // Position player in center observation spot
        rigInstance.transform.position = new Vector3(0f, 0f, -0.2f);
        rigInstance.transform.rotation = Quaternion.identity;

        // Ensure character collider / kinematic body so trigger detection works
        Rigidbody rigRb = rigInstance.GetComponent<Rigidbody>();
        if (rigRb == null) rigRb = rigInstance.AddComponent<Rigidbody>();
        rigRb.isKinematic = true;
        rigRb.useGravity = false;

        CapsuleCollider rigCol = rigInstance.GetComponent<CapsuleCollider>();
        if (rigCol == null) rigCol = rigInstance.AddComponent<CapsuleCollider>();
        rigCol.height = 1.8f;
        rigCol.radius = 0.35f;
        rigCol.center = new Vector3(0f, 0.9f, 0f);
        rigCol.isTrigger = false;

        return rigInstance;
    }

    private static void SetupXRDeviceSimulator()
    {
        GameObject existingSim = GameObject.Find("XR Device Simulator");
        if (existingSim != null) UnityEngine.Object.DestroyImmediate(existingSim);

        string simPrefabPath = "Assets/Samples/XR Interaction Toolkit/XR Device Simulator/XR Device Simulator.prefab";
        GameObject simPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(simPrefabPath);
        if (simPrefab != null)
        {
            GameObject simInstance = (GameObject)PrefabUtility.InstantiatePrefab(simPrefab);
            simInstance.name = "XR Device Simulator";
        }
    }

    private static void SetupGrabbableOrgans()
    {
        OrganInteractable[] allOrgans = UnityEngine.Object.FindObjectsByType<OrganInteractable>(FindObjectsSortMode.None);
        Material ghostMat = SetupLabScene.CreateMaterial("Lab", "OrganSocketGhost", new Color(0.1f, 0.7f, 1f, 0.25f), 0.9f, 0.1f, true, new Color(0f, 0.4f, 0.8f, 0.3f));
        Material hoverMat = SetupLabScene.CreateMaterial("Lab", "VR_OrganHover", new Color(0.2f, 0.85f, 1f, 0.9f), 0.7f, 0.2f, false, new Color(0.1f, 0.5f, 0.9f, 0.5f));

        foreach (var organ in allOrgans)
        {
            GameObject go = organ.gameObject;

            Rigidbody rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // Ensure every renderer has an exact convex collider
            Renderer[] rends = go.GetComponentsInChildren<Renderer>();
            foreach (var r in rends)
            {
                Collider c = r.GetComponent<Collider>();
                if (c == null)
                {
                    MeshFilter mf = r.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                    {
                        MeshCollider mc = r.gameObject.AddComponent<MeshCollider>();
                        mc.convex = true;
                    }
                    else
                    {
                        r.gameObject.AddComponent<BoxCollider>();
                    }
                }
            }

            VRGrabbableOrgan grabbable = go.GetComponent<VRGrabbableOrgan>();
            if (grabbable == null) grabbable = go.AddComponent<VRGrabbableOrgan>();

            grabbable.snapDistance = 0.35f;
            grabbable.floatWhenReleased = true;
            grabbable.hoverOutlineMaterial = hoverMat;

            Transform existingGhost = go.transform.parent != null ? go.transform.parent.Find(go.name + "_SocketGhost") : null;
            if (existingGhost == null && go.transform.parent != null)
            {
                GameObject ghost = new GameObject(go.name + "_SocketGhost");
                ghost.transform.SetParent(go.transform.parent, false);
                ghost.transform.localPosition = go.transform.localPosition;
                ghost.transform.localRotation = go.transform.localRotation;
                ghost.transform.localScale = go.transform.localScale;

                MeshFilter mf = go.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    MeshFilter gmf = ghost.AddComponent<MeshFilter>();
                    gmf.sharedMesh = mf.sharedMesh;
                    MeshRenderer gmr = ghost.AddComponent<MeshRenderer>();
                    gmr.sharedMaterial = ghostMat;
                }
                ghost.SetActive(false);
                grabbable.socketGhostIndicator = ghost;
            }
        }
    }

    private static void SetupCentralDissectionTableAndConsole()
    {
        // 1. Central Examination Table
        GameObject table = GameObject.Find("DissectionTable");
        if (table == null) table = GameObject.CreatePrimitive(PrimitiveType.Cube);
        table.name = "DissectionTable";
        table.transform.position = new Vector3(0f, 0.45f, 1.5f);
        table.transform.localScale = new Vector3(1.8f, 0.9f, 1.1f);

        Material tableMat = SetupLabScene.CreateMaterial("Lab", "VR_TableMat", new Color(0.1f, 0.14f, 0.2f), 0.9f, 0.4f, false, new Color(0f, 0.3f, 0.6f, 0.3f));
        table.GetComponent<Renderer>().sharedMaterial = tableMat;

        // Position Anatomy Model directly onto table (clean, unobstructed 360-degree view)
        GameObject anatomy = GameObject.Find("Anatomy");
        if (anatomy != null)
        {
            anatomy.transform.position = new Vector3(0f, 0.95f, 1.5f);
            anatomy.transform.rotation = Quaternion.identity;
        }

        // Clean, dedicated front Inspection / Presentation Focal Point
        GameObject ifp = GameObject.Find("InspectionFocalPoint");
        if (ifp == null) ifp = new GameObject("InspectionFocalPoint");
        ifp.transform.position = new Vector3(0f, 1.30f, 0.60f);
        ifp.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        // Clean up any old floating console slabs, tablets, or cards that obstructed the view
        GameObject consoleRoot = GameObject.Find("DissectionTableConsole");
        if (consoleRoot != null) UnityEngine.Object.DestroyImmediate(consoleRoot);

        GameObject cardRoot = GameObject.Find("HolographicOrganCard");
        if (cardRoot != null) UnityEngine.Object.DestroyImmediate(cardRoot);

        GameObject tablet = GameObject.Find("VR_ClinicalTablet");
        if (tablet != null) UnityEngine.Object.DestroyImmediate(tablet);
    }

    private static void SetupQuizKioskStation()
    {
        GameObject kioskRoot = GameObject.Find("VRQuizStation");
        if (kioskRoot != null) UnityEngine.Object.DestroyImmediate(kioskRoot);
        kioskRoot = new GameObject("VRQuizStation");
        // Wall-mounted on the left wall with clear sightline to central body, facing inward (+X)
        kioskRoot.transform.position = new Vector3(-6.2f, 0.2f, 1.4f);
        kioskRoot.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

        Font font = GetLabFont();
        VRQuizStation station = kioskRoot.AddComponent<VRQuizStation>();

        // Screen Canvas (1300 x 1100 px -> 1.3m x 1.1m)
        GameObject canvasGO = CreateWorldCanvas("KioskCanvas", kioskRoot.transform, new Vector3(0f, 1.45f, 0f), Quaternion.identity, new Vector2(1300f, 1100f), 0.001f);
        CreateImage("ScreenBG", canvasGO.transform, Vector2.zero, new Vector2(1300f, 1100f), new Color(0.04f, 0.07f, 0.12f, 0.95f));

        // Top Header
        CreateImage("TopBar", canvasGO.transform, new Vector2(0f, 500f), new Vector2(1260f, 75f), new Color(0.08f, 0.15f, 0.25f, 1f));
        CreateText("KioskTitle", canvasGO.transform, new Vector2(-280f, 500f), new Vector2(650f, 50f), "CLINICAL ASSESSMENT & QUIZ KIOSK", font, 26, FontStyle.Bold, new Color(0.3f, 0.85f, 1f), TextAnchor.MiddleLeft);

        station.statsScoreText = CreateText("ScoreText", canvasGO.transform, new Vector2(250f, 500f), new Vector2(200f, 40f), "Score: 0 pts", font, 20, FontStyle.Bold, Color.white, TextAnchor.MiddleRight);
        station.statsStreakText = CreateText("StreakText", canvasGO.transform, new Vector2(480f, 500f), new Vector2(200f, 40f), "Streak: 0x", font, 20, FontStyle.Bold, Color.white, TextAnchor.MiddleRight);

        // Navigation Tabs
        station.quizTabBtn = CreateActionButton("TabQuiz", canvasGO.transform, new Vector2(-280f, 420f), new Vector2(300f, 50f), "Interactive Anatomy Quiz", font, 20, new Color(0.15f, 0.35f, 0.55f));
        station.casesTabBtn = CreateActionButton("TabCases", canvasGO.transform, new Vector2(60f, 420f), new Vector2(300f, 50f), "Clinical Case Simulation", font, 20, new Color(0.12f, 0.25f, 0.38f));

        // --- Container 1: Quiz Container ---
        GameObject quizContainer = CreateUIElement("QuizContainer", canvasGO.transform, Vector2.zero, new Vector2(1260f, 800f));
        station.quizContainer = quizContainer;

        station.quizSystemText = CreateText("QSystem", quizContainer.transform, new Vector2(0f, 335f), new Vector2(1200f, 35f), "System: Cardiovascular", font, 19, FontStyle.Bold, new Color(0.4f, 0.9f, 0.75f), TextAnchor.MiddleLeft);
        station.quizQuestionText = CreateText("QText", quizContainer.transform, new Vector2(0f, 250f), new Vector2(1200f, 120f), "Question loading...", font, 23, FontStyle.Normal, Color.white, TextAnchor.UpperLeft);
        station.quizHintText = CreateText("QHint", quizContainer.transform, new Vector2(0f, 155f), new Vector2(1200f, 50f), "Clinical Clue...", font, 17, FontStyle.Italic, new Color(0.7f, 0.85f, 1f), TextAnchor.MiddleLeft);

        station.optionButtons = new Button[4];
        station.optionTexts = new Text[4];

        for (int i = 0; i < 4; i++)
        {
            float yPos = 70f - (i * 68f);
            Button btn = CreateActionButton($"Option_{i}", quizContainer.transform, new Vector2(0f, yPos), new Vector2(1200f, 56f), $"Option {i + 1}", font, 20, new Color(0.12f, 0.22f, 0.35f));
            station.optionButtons[i] = btn;
            station.optionTexts[i] = btn.GetComponentInChildren<Text>();
            station.optionTexts[i].alignment = TextAnchor.MiddleLeft;
            RectTransform lrt = station.optionTexts[i].GetComponent<RectTransform>();
            lrt.offsetMin = new Vector2(25f, 0f);
        }

        station.feedbackText = CreateText("Feedback", quizContainer.transform, new Vector2(0f, -250f), new Vector2(1200f, 110f), "", font, 19, FontStyle.Normal, Color.white, TextAnchor.UpperLeft);
        station.nextQuestionBtn = CreateActionButton("BtnNextQ", quizContainer.transform, new Vector2(420f, -340f), new Vector2(280f, 52f), "Next Question ➔", font, 21, new Color(0.12f, 0.55f, 0.35f));

        // --- Container 2: Clinical Cases Container ---
        GameObject casesContainer = CreateUIElement("CasesContainer", canvasGO.transform, Vector2.zero, new Vector2(1260f, 800f));
        station.casesContainer = casesContainer;
        casesContainer.SetActive(false);

        station.caseTitleText = CreateText("CaseTitle", casesContainer.transform, new Vector2(0f, 340f), new Vector2(1200f, 35f), "Case 1: Emergency Patient", font, 22, FontStyle.Bold, new Color(0.3f, 0.9f, 1f), TextAnchor.MiddleLeft);
        station.casePatientProfileText = CreateText("CaseProfile", casesContainer.transform, new Vector2(0f, 290f), new Vector2(1200f, 45f), "Patient Profile...", font, 18, FontStyle.Normal, Color.white, TextAnchor.UpperLeft);
        station.caseComplaintText = CreateText("CaseComplaint", casesContainer.transform, new Vector2(0f, 220f), new Vector2(1200f, 75f), "Chief Complaint...", font, 18, FontStyle.Normal, new Color(1f, 0.85f, 0.7f), TextAnchor.UpperLeft);
        station.caseVitalsText = CreateText("CaseVitals", casesContainer.transform, new Vector2(0f, 145f), new Vector2(1200f, 35f), "Vital Signs...", font, 18, FontStyle.Bold, new Color(1f, 0.9f, 0.3f), TextAnchor.MiddleLeft);
        station.casePromptText = CreateText("CasePrompt", casesContainer.transform, new Vector2(0f, 95f), new Vector2(1200f, 40f), "Confirm Diagnosis:", font, 19, FontStyle.Bold, new Color(0.4f, 1f, 0.8f), TextAnchor.MiddleLeft);

        station.differentialButtons = new Button[4];
        station.differentialTexts = new Text[4];

        for (int i = 0; i < 4; i++)
        {
            float yPos = 30f - (i * 62f);
            Button btn = CreateActionButton($"Diff_{i}", casesContainer.transform, new Vector2(0f, yPos), new Vector2(1200f, 52f), $"Differential {i + 1}", font, 19, new Color(0.12f, 0.22f, 0.35f));
            station.differentialButtons[i] = btn;
            station.differentialTexts[i] = btn.GetComponentInChildren<Text>();
            station.differentialTexts[i].alignment = TextAnchor.MiddleLeft;
            RectTransform lrt = station.differentialTexts[i].GetComponent<RectTransform>();
            lrt.offsetMin = new Vector2(25f, 0f);
        }

        station.caseDebriefText = CreateText("CaseDebrief", casesContainer.transform, new Vector2(0f, -255f), new Vector2(1200f, 115f), "", font, 18, FontStyle.Normal, Color.white, TextAnchor.UpperLeft);
        station.nextCaseBtn = CreateActionButton("BtnNextCase", casesContainer.transform, new Vector2(420f, -340f), new Vector2(280f, 52f), "Next Case ➔", font, 21, new Color(0.12f, 0.55f, 0.35f));
    }

    private static void SetupAINovaPodStation()
    {
        GameObject podRoot = GameObject.Find("VRAINovaPod");
        if (podRoot != null) UnityEngine.Object.DestroyImmediate(podRoot);
        podRoot = new GameObject("VRAINovaPod");
        // Wall-mounted on the right wall with clear sightline to central body, facing inward (-X)
        podRoot.transform.position = new Vector3(6.2f, 0.2f, 1.4f);
        podRoot.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        Font font = GetLabFont();
        VRAINovaPod pod = podRoot.AddComponent<VRAINovaPod>();

        // Screen Canvas (1300 x 1100 px -> 1.3m x 1.1m)
        GameObject canvasGO = CreateWorldCanvas("AICanvas", podRoot.transform, new Vector3(0f, 1.45f, 0f), Quaternion.identity, new Vector2(1300f, 1100f), 0.001f);
        CreateImage("ScreenBG", canvasGO.transform, Vector2.zero, new Vector2(1300f, 1100f), new Color(0.04f, 0.08f, 0.1f, 0.95f));

        // Top Header
        CreateImage("TopBar", canvasGO.transform, new Vector2(0f, 500f), new Vector2(1260f, 75f), new Color(0.07f, 0.18f, 0.22f, 1f));
        CreateText("AITitle", canvasGO.transform, new Vector2(-280f, 500f), new Vector2(650f, 50f), "DR. NOVA — AI CLINICAL CONSULTANT", font, 26, FontStyle.Bold, new Color(0.3f, 1f, 0.75f), TextAnchor.MiddleLeft);

        pod.connectionStatusText = CreateText("Status", canvasGO.transform, new Vector2(300f, 512f), new Vector2(400f, 30f), "● Live RAG & LLM Online", font, 17, FontStyle.Bold, new Color(0.3f, 1f, 0.6f), TextAnchor.MiddleRight);
        pod.currentOrganContextText = CreateText("Context", canvasGO.transform, new Vector2(300f, 485f), new Vector2(400f, 30f), "Context: Central Anatomy", font, 16, FontStyle.Normal, new Color(0.7f, 0.9f, 1f), TextAnchor.MiddleRight);

        // Mode switch
        pod.modeToggleBtn = CreateActionButton("BtnMode", canvasGO.transform, new Vector2(-460f, 420f), new Vector2(250f, 46f), "Mode: Student", font, 18, new Color(0.12f, 0.35f, 0.3f));
        pod.modeToggleBtnText = pod.modeToggleBtn.GetComponentInChildren<Text>();

        // Quick inquiries section
        CreateText("InqLabel", canvasGO.transform, new Vector2(-40f, 420f), new Vector2(300f, 40f), "High-Yield Clinical Inquiries:", font, 19, FontStyle.Bold, new Color(0.75f, 0.9f, 1f), TextAnchor.MiddleLeft);

        pod.btnAngina = CreateActionButton("BtnAngina", canvasGO.transform, new Vector2(-330f, 350f), new Vector2(280f, 48f), "1. Angina Arm Radiation", font, 18, new Color(0.14f, 0.26f, 0.4f));
        pod.btnPortal = CreateActionButton("BtnPortal", canvasGO.transform, new Vector2(-20f, 350f), new Vector2(280f, 48f), "2. Portal Vein & Ascites", font, 18, new Color(0.14f, 0.26f, 0.4f));
        pod.btnCarina = CreateActionButton("BtnCarina", canvasGO.transform, new Vector2(290f, 350f), new Vector2(280f, 48f), "3. Carina Cough Reflex", font, 18, new Color(0.14f, 0.26f, 0.4f));
        pod.btnBaroreceptor = CreateActionButton("BtnBaro", canvasGO.transform, new Vector2(470f, 420f), new Vector2(280f, 46f), "4. Baroreceptor Reflex", font, 17, new Color(0.14f, 0.26f, 0.4f));

        // Response Display Box
        CreateImage("ResponseBG", canvasGO.transform, new Vector2(0f, 90f), new Vector2(1240f, 420f), new Color(0.06f, 0.12f, 0.16f, 1f));
        pod.responseBodyText = CreateText("ResponseBody", canvasGO.transform, new Vector2(0f, 90f), new Vector2(1200f, 390f), "Select an inquiry or inspect an organ to consult Dr. Nova...", font, 20, FontStyle.Normal, Color.white, TextAnchor.UpperLeft);

        // Sources & Mnemonics
        pod.sourcesText = CreateText("Sources", canvasGO.transform, new Vector2(0f, -160f), new Vector2(1240f, 40f), "Citations: Guyton & Hall Medical Physiology", font, 16, FontStyle.Italic, new Color(0.5f, 0.85f, 1f), TextAnchor.MiddleLeft);
        pod.mnemonicText = CreateText("Mnemonic", canvasGO.transform, new Vector2(0f, -210f), new Vector2(1240f, 40f), "", font, 17, FontStyle.Bold, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleLeft);

        // Audio Stream Button
        pod.listenTTSBtn = CreateActionButton("BtnListenTTS", canvasGO.transform, new Vector2(0f, -280f), new Vector2(450f, 56f), "🔊 Listen to Spoken Explanation", font, 22, new Color(0.15f, 0.55f, 0.38f));
        pod.listenTTSBtnText = pod.listenTTSBtn.GetComponentInChildren<Text>();
    }

    private static void SetupComparisonBayStation()
    {
        Font font = GetLabFont();

        Material leftPedMat = SetupLabScene.CreateMaterial("Lab", "VR_LeftPedMat", new Color(0.1f, 0.15f, 0.25f), 0.9f, 0.5f, false, new Color(0f, 0.6f, 1f, 0.6f));
        Material rightPedMat = SetupLabScene.CreateMaterial("Lab", "VR_RightPedMat", new Color(0.2f, 0.12f, 0.25f), 0.9f, 0.5f, false, new Color(0.9f, 0.2f, 0.8f, 0.6f));
        Material glowRingMatA = SetupLabScene.CreateMaterial("Lab", "GlowRingA", new Color(0f, 0.8f, 1f, 0.9f), 0.9f, 0.2f, false, new Color(0f, 0.9f, 1f, 0.9f));
        Material glowRingMatB = SetupLabScene.CreateMaterial("Lab", "GlowRingB", new Color(1f, 0.2f, 0.8f, 0.9f), 0.9f, 0.2f, false, new Color(1f, 0.3f, 0.9f, 0.9f));

        // 1. PHYSICAL COMPARISON PILLAR A (CYAN) - Sleek platform mounted on top of the examination table
        GameObject leftPed = GameObject.Find("LeftComparisonPedestal");
        if (leftPed != null) UnityEngine.Object.DestroyImmediate(leftPed);
        leftPed = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        leftPed.name = "LeftComparisonPedestal";
        leftPed.transform.position = new Vector3(-0.65f, 0.95f, 1.25f);
        leftPed.transform.localScale = new Vector3(0.38f, 0.05f, 0.38f);
        leftPed.GetComponent<Renderer>().sharedMaterial = leftPedMat;

        var trigA = leftPed.AddComponent<ComparisonPedestalTrigger>();
        trigA.isLeftPedestal = true;

        GameObject leftRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        leftRing.name = "GlowRingA";
        leftRing.transform.SetParent(leftPed.transform, false);
        leftRing.transform.localPosition = new Vector3(0f, 1.02f, 0f);
        leftRing.transform.localScale = new Vector3(1.05f, 0.05f, 1.05f);
        leftRing.GetComponent<Renderer>().sharedMaterial = glowRingMatA;
        if (leftRing.GetComponent<Collider>() != null) UnityEngine.Object.DestroyImmediate(leftRing.GetComponent<Collider>());

        GameObject labelCanvasA = CreateWorldCanvas("LabelCanvasA", leftPed.transform, new Vector3(0f, 1.45f, 0f), Quaternion.Euler(0f, 180f, 0f), new Vector2(400f, 120f), 0.001f);
        CreateText("LabelTextA", labelCanvasA.transform, Vector2.zero, new Vector2(380f, 100f), "<b>PEDESTAL A</b>\n<size=18><color=#00E5FF>Auto-Snap / Place Organ 1</color></size>", font, 24, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

        // 2. PHYSICAL COMPARISON PILLAR B (MAGENTA) - Sleek platform mounted on top of the examination table
        GameObject rightPed = GameObject.Find("RightComparisonPedestal");
        if (rightPed != null) UnityEngine.Object.DestroyImmediate(rightPed);
        rightPed = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rightPed.name = "RightComparisonPedestal";
        rightPed.transform.position = new Vector3(0.65f, 0.95f, 1.25f);
        rightPed.transform.localScale = new Vector3(0.38f, 0.05f, 0.38f);
        rightPed.GetComponent<Renderer>().sharedMaterial = rightPedMat;

        var trigB = rightPed.AddComponent<ComparisonPedestalTrigger>();
        trigB.isLeftPedestal = false;

        GameObject rightRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rightRing.name = "GlowRingB";
        rightRing.transform.SetParent(rightPed.transform, false);
        rightRing.transform.localPosition = new Vector3(0f, 1.02f, 0f);
        rightRing.transform.localScale = new Vector3(1.05f, 0.05f, 1.05f);
        rightRing.GetComponent<Renderer>().sharedMaterial = glowRingMatB;
        if (rightRing.GetComponent<Collider>() != null) UnityEngine.Object.DestroyImmediate(rightRing.GetComponent<Collider>());

        GameObject labelCanvasB = CreateWorldCanvas("LabelCanvasB", rightPed.transform, new Vector3(0f, 1.45f, 0f), Quaternion.Euler(0f, 180f, 0f), new Vector2(400f, 120f), 0.001f);
        CreateText("LabelTextB", labelCanvasB.transform, Vector2.zero, new Vector2(380f, 100f), "<b>PEDESTAL B</b>\n<size=18><color=#FF88FF>Auto-Snap / Place Organ 2</color></size>", font, 24, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

        // 3. WALL COMPARISON BOARD - Mounted on back wall FACING FORWARD (+Z)
        GameObject bayRoot = GameObject.Find("VRComparisonBay");
        if (bayRoot != null) UnityEngine.Object.DestroyImmediate(bayRoot);
        bayRoot = new GameObject("VRComparisonBay");
        bayRoot.transform.position = new Vector3(0f, 0.1f, -6.8f);
        // Canvas faces into the room towards +Z so text reads normally from left to right
        bayRoot.transform.rotation = Quaternion.identity;

        VRComparisonBay bay = bayRoot.AddComponent<VRComparisonBay>();
        bay.leftPedestalMount = leftPed.transform;
        bay.rightPedestalMount = rightPed.transform;

        // Wall Screen Board (1400 x 850 px)
        GameObject canvasGO = CreateWorldCanvas("ComparisonCanvas", bayRoot.transform, new Vector3(0f, 1.75f, 0f), Quaternion.identity, new Vector2(1400f, 850f), 0.001f);
        CreateImage("BoardBG", canvasGO.transform, Vector2.zero, new Vector2(1400f, 850f), new Color(0.05f, 0.07f, 0.12f, 0.96f));

        bay.titleText = CreateText("Title", canvasGO.transform, new Vector2(0f, 375f), new Vector2(1300f, 50f), "PATHOLOGY & ORGAN COMPARISON BAY", font, 26, FontStyle.Bold, new Color(0.85f, 0.6f, 1f), TextAnchor.MiddleCenter);

        bay.organANameText = CreateText("NameA", canvasGO.transform, new Vector2(-360f, 310f), new Vector2(500f, 40f), "Specimen A: [Empty]", font, 22, FontStyle.Bold, new Color(0.3f, 0.85f, 1f), TextAnchor.MiddleLeft);
        bay.organBNameText = CreateText("NameB", canvasGO.transform, new Vector2(360f, 310f), new Vector2(500f, 40f), "Specimen B: [Empty]", font, 22, FontStyle.Bold, new Color(1f, 0.6f, 0.9f), TextAnchor.MiddleRight);

        bay.metricsComparisonText = CreateText("Metrics", canvasGO.transform, new Vector2(0f, 75f), new Vector2(1300f, 400f), "Place extracted organs onto Pedestals A & B to analyze comparative anatomy, mass ratios, and pathology.", font, 20, FontStyle.Normal, Color.white, TextAnchor.UpperLeft);

        bay.swapPedestalsBtn = CreateActionButton("BtnSwap", canvasGO.transform, new Vector2(-180f, -340f), new Vector2(260f, 54f), "⇄ Swap Positions", font, 20, new Color(0.25f, 0.35f, 0.6f));
        bay.clearPedestalsBtn = CreateActionButton("BtnClear", canvasGO.transform, new Vector2(180f, -340f), new Vector2(260f, 54f), "Clear & Return", font, 20, new Color(0.6f, 0.25f, 0.25f));

        // Connect OrganComparisonController
        OrganComparisonController comp = UnityEngine.Object.FindFirstObjectByType<OrganComparisonController>();
        if (comp != null)
        {
            comp.leftPedestal = leftPed.transform;
            comp.rightPedestal = rightPed.transform;
            comp.inspectionFocalPoint = GameObject.Find("InspectionFocalPoint")?.transform;
        }
    }

    private static void SetupZoneTriggersAndFloorPads()
    {
        // 1. Quiz Zone Trigger and Floor Pad (X = -3.0, Z = 1.2)
        EnsureZonePadAndTrigger("Zone_Quiz", new Vector3(-3.0f, 0f, 1.2f), new Vector3(2.5f, 2.5f, 2.5f),
            VRZoneTrigger.LabZoneType.Quiz, "Quiz & Examination Bay", new Color(0.1f, 0.6f, 0.9f, 0.85f));

        // 2. Teaching AI Pod Zone Trigger and Floor Pad (X = +3.0, Z = 1.2)
        EnsureZonePadAndTrigger("Zone_TeachingAI", new Vector3(3.0f, 0f, 1.2f), new Vector3(2.5f, 2.5f, 2.5f),
            VRZoneTrigger.LabZoneType.TeachingAI, "Dr. Nova AI Teaching Pod", new Color(0.1f, 0.85f, 0.5f, 0.85f));

        // 3. Central Dissection Zone Trigger and Floor Pad (X = 0, Z = 0.5)
        EnsureZonePadAndTrigger("Zone_Dissection", new Vector3(0f, 0f, 0.5f), new Vector3(2.8f, 2.5f, 2.6f),
            VRZoneTrigger.LabZoneType.Dissection, "Anatomy Dissection Stage", new Color(0.2f, 0.5f, 0.85f, 0.85f));

        // 4. Comparison Bay Zone Trigger and Floor Pad (X = 0, Z = -4.5)
        EnsureZonePadAndTrigger("Zone_Comparison", new Vector3(0f, 0f, -4.5f), new Vector3(2.8f, 2.5f, 2.6f),
            VRZoneTrigger.LabZoneType.Comparison, "Organ Comparison Bay", new Color(0.7f, 0.3f, 0.85f, 0.85f));
    }

    private static void EnsureZonePadAndTrigger(string name, Vector3 pos, Vector3 size, VRZoneTrigger.LabZoneType zoneType, string label, Color color)
    {
        GameObject zoneGO = GameObject.Find(name);
        if (zoneGO != null) UnityEngine.Object.DestroyImmediate(zoneGO);
        zoneGO = new GameObject(name);
        zoneGO.transform.position = pos;

        // Visual floor decal disc: sleek, flat, non-colliding
        GameObject decal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        decal.name = "FloorDecal";
        decal.transform.SetParent(zoneGO.transform, false);
        decal.transform.localPosition = new Vector3(0f, 0.005f, 0f);
        decal.transform.localScale = new Vector3(size.x, 0.005f, size.z);
        
        Collider colDecal = decal.GetComponent<Collider>();
        if (colDecal != null) UnityEngine.Object.DestroyImmediate(colDecal);

        Material decalMat = SetupLabScene.CreateMaterial("Lab", "Mat_" + name, color * 0.4f, 0.8f, 0.3f, true, color * 0.7f);
        decal.GetComponent<Renderer>().sharedMaterial = decalMat;

        // Trigger Collider for automatic station switching
        BoxCollider boxCol = zoneGO.AddComponent<BoxCollider>();
        boxCol.isTrigger = true;
        boxCol.center = new Vector3(0f, size.y * 0.5f, 0f);
        boxCol.size = size;

        VRZoneTrigger trigger = zoneGO.AddComponent<VRZoneTrigger>();
        trigger.zoneType = zoneType;
        trigger.zoneDisplayName = label;
        trigger.zoneAccentColor = color;
    }

    private static void SetupManagers()
    {
        // VRLabManager
        GameObject mgrGO = GameObject.Find("VRLabManager");
        if (mgrGO == null) mgrGO = new GameObject("VRLabManager");
        VRLabManager mgr = mgrGO.GetComponent<VRLabManager>();
        if (mgr == null) mgr = mgrGO.AddComponent<VRLabManager>();

        // LLMAnatomyClient
        LLMAnatomyClient llm = UnityEngine.Object.FindFirstObjectByType<LLMAnatomyClient>();
        if (llm == null)
        {
            GameObject llmGO = new GameObject("LLMAnatomyClient");
            llm = llmGO.AddComponent<LLMAnatomyClient>();
        }

        // QuizEngine
        QuizEngine qe = UnityEngine.Object.FindFirstObjectByType<QuizEngine>();
        if (qe == null)
        {
            GameObject qeGO = new GameObject("QuizEngine");
            qe = qeGO.AddComponent<QuizEngine>();
        }
    }

    // --- Helpers ---
    private static Font GetLabFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }

    private static GameObject CreateWorldCanvas(string name, Transform parent, Vector3 localPos, Quaternion localRot, Vector2 size, float scale)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.transform.localScale = new Vector3(scale, scale, scale);

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 4f; // Sharp text in VR

        go.AddComponent<GraphicRaycaster>();
        go.AddComponent<TrackedDeviceGraphicRaycaster>(); // Allows VR laser pointer clicks

        return go;
    }

    private static GameObject CreateUIElement(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return go;
    }

    private static Image CreateImage(string name, Transform parent, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = CreateUIElement(name, parent, pos, size);
        Image img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private static Text CreateText(string name, Transform parent, Vector2 pos, Vector2 size, string text, Font font, int fontSize, FontStyle style, Color color, TextAnchor align)
    {
        GameObject go = CreateUIElement(name, parent, pos, size);
        Text t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.color = color;
        t.alignment = align;
        t.text = text;
        return t;
    }

    private static Button CreateActionButton(string name, Transform parent, Vector2 pos, Vector2 size, string text, Font font, int fontSize, Color baseColor)
    {
        GameObject go = CreateUIElement(name, parent, pos, size);
        Image img = go.AddComponent<Image>();
        img.color = baseColor;

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.highlightedColor = baseColor * 1.35f;
        cb.pressedColor = baseColor * 1.7f;
        btn.colors = cb;

        GameObject labelGO = CreateUIElement("Label", go.transform, Vector2.zero, size);
        Text labelText = labelGO.AddComponent<Text>();
        labelText.font = font;
        labelText.fontSize = fontSize;
        labelText.fontStyle = FontStyle.Bold;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = Color.white;
        labelText.text = text;

        return btn;
    }
}
#endif
