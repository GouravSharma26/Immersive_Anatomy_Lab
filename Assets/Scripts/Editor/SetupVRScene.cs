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
        Debug.Log("[Phase 6] Starting OpenXR & VR Scene Configuration...");
        ConfigureOpenXRSettings();
        BuildVRMainLabScene();
        Debug.Log("[Phase 6] VR OpenXR Scene successfully built and configured!");
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
        Debug.Log("[Phase 6] Configuring OpenXR Settings for Standalone Windows...");

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

        // Assign OpenXR Loader
        XRPackageMetadataStore.AssignLoader(manager, "UnityEngine.XR.OpenXR.OpenXRLoader", BuildTargetGroup.Standalone);

        // Enable Oculus Touch Profile in OpenXR Settings
        OpenXRSettings openXRSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
        if (openXRSettings != null)
        {
            var features = openXRSettings.GetFeatures();
            foreach (var feat in features)
            {
                if (feat != null && feat.GetType().Name.Contains("OculusTouch"))
                {
                    feat.enabled = true;
                    Debug.Log($"[Phase 6] Enabled OpenXR Feature: {feat.name}");
                }
            }
            EditorUtility.SetDirty(openXRSettings);
        }

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(generalSettings);
        EditorUtility.SetDirty(buildTargetSettings);
        AssetDatabase.SaveAssets();
        Debug.Log("[Phase 6] OpenXR Configuration complete.");
    }

    public static void BuildVRMainLabScene()
    {
        Scene scene = EditorSceneManager.OpenScene(VR_SCENE_PATH, OpenSceneMode.Single);
        Debug.Log($"[Phase 6] Opened {VR_SCENE_PATH} for VR configuration.");

        // 1. Remove or disable desktop Player to prevent camera & input conflicts
        GameObject desktopPlayer = GameObject.Find("Player");
        if (desktopPlayer != null)
        {
            Debug.Log("[Phase 6] Removing desktop Player controller from VR scene.");
            UnityEngine.Object.DestroyImmediate(desktopPlayer);
        }

        // Also check if any loose Camera exists
        Camera[] looseCams = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var c in looseCams)
        {
            if (c.gameObject.name == "Main Camera" && c.transform.parent == null)
            {
                UnityEngine.Object.DestroyImmediate(c.gameObject);
            }
        }

        // 2. Remove desktop UI canvas so it doesn't float as a screen-space overlay in VR
        GameObject desktopUI = GameObject.Find("UI");
        if (desktopUI != null)
        {
            UnityEngine.Object.DestroyImmediate(desktopUI);
        }

        // 3. Setup Teleportation on Floor
        SetupTeleportationFloor();

        // 4. Instantiate or configure XR Origin Rig
        GameObject xrRig = SetupXROriginRig();

        // 5. Instantiate XR Device Simulator for PC testing without physical headset
        SetupXRDeviceSimulator();

        // 6. Equip Organs with VRGrabbableOrgan
        SetupGrabbableOrgans();

        // 7. Create Diegetic World-Space VR Clinical Tablet
        SetupVRClinicalTablet();

        // 8. Setup VRLabManager
        SetupVRLabManager();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Phase 6] Saved {VR_SCENE_PATH} with full OpenXR & VR integration.");
    }

    private static void SetupTeleportationFloor()
    {
        GameObject floor = GameObject.Find("Floor");
        if (floor != null)
        {
            TeleportationArea teleArea = floor.GetComponent<TeleportationArea>();
            if (teleArea == null) teleArea = floor.AddComponent<TeleportationArea>();
            teleArea.teleportationProvider = null; // Assigned by XRI
        }

        GameObject table = GameObject.Find("LabTable") ?? GameObject.Find("Pedestal");
        if (table != null)
        {
            // Ensure table has collider
            if (table.GetComponent<Collider>() == null) table.AddComponent<BoxCollider>();
        }
    }

    private static GameObject SetupXROriginRig()
    {
        GameObject existingRig = GameObject.Find("XR Origin (XR Rig)") ?? GameObject.Find("XR Origin (VR)");
        if (existingRig != null)
        {
            UnityEngine.Object.DestroyImmediate(existingRig);
        }

        string prefabPath = "Assets/Samples/XR Interaction Toolkit/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
        GameObject rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        GameObject rigInstance = null;

        if (rigPrefab != null)
        {
            rigInstance = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
            rigInstance.name = "XR Origin (XR Rig)";
            Debug.Log("[Phase 6] Instantiated XR Origin (XR Rig) from Starter Assets prefab.");
        }
        else
        {
            Debug.LogWarning("[Phase 6] Starter Assets XR Origin prefab not found, creating programmatically.");
            rigInstance = new GameObject("XR Origin (VR)");
            var origin = rigInstance.AddComponent<Unity.XR.CoreUtils.XROrigin>();
            var mgr = rigInstance.AddComponent<XRInteractionManager>();

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

        // Position rig in front of mannequin examination table
        rigInstance.transform.position = new Vector3(0f, 0f, -0.6f);
        rigInstance.transform.rotation = Quaternion.identity;

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
            Debug.Log("[Phase 6] Instantiated XR Device Simulator for PC testing without physical headset.");
        }
        else
        {
            Debug.LogWarning("[Phase 6] XR Device Simulator prefab not found at " + simPrefabPath);
        }
    }

    private static void SetupGrabbableOrgans()
    {
        OrganInteractable[] allOrgans = UnityEngine.Object.FindObjectsByType<OrganInteractable>(FindObjectsSortMode.None);
        Debug.Log($"[Phase 6] Equipping {allOrgans.Length} organs with VRGrabbableOrgan components...");

        Material ghostMat = SetupLabScene.CreateMaterial("Lab", "OrganSocketGhost", new Color(0.1f, 0.7f, 1f, 0.25f), 0.9f, 0.1f, true, new Color(0f, 0.4f, 0.8f, 0.3f));
        Material hoverMat = SetupLabScene.CreateMaterial("Lab", "VR_OrganHover", new Color(0.2f, 0.85f, 1f, 0.9f), 0.7f, 0.2f, false, new Color(0.1f, 0.5f, 0.9f, 0.5f));

        foreach (var organ in allOrgans)
        {
            GameObject go = organ.gameObject;

            // Ensure Rigidbody
            Rigidbody rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // Ensure Collider
            Collider col = go.GetComponent<Collider>();
            if (col == null)
            {
                MeshFilter mf = go.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    MeshCollider mc = go.AddComponent<MeshCollider>();
                    mc.convex = true;
                    col = mc;
                }
                else
                {
                    BoxCollider bc = go.AddComponent<BoxCollider>();
                    col = bc;
                }
            }

            // Add VRGrabbableOrgan
            VRGrabbableOrgan grabbable = go.GetComponent<VRGrabbableOrgan>();
            if (grabbable == null) grabbable = go.AddComponent<VRGrabbableOrgan>();

            grabbable.snapDistance = 0.35f;
            grabbable.floatWhenReleased = true;
            grabbable.hoverOutlineMaterial = hoverMat;

            // Create Ghost indicator at socket
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

    private static void SetupVRClinicalTablet()
    {
        GameObject existingTablet = GameObject.Find("VR_ClinicalTablet");
        if (existingTablet != null) UnityEngine.Object.DestroyImmediate(existingTablet);

        GameObject tabletRoot = new GameObject("VR_ClinicalTablet");
        // Place ergonomically to the left of user facing inward
        tabletRoot.transform.position = new Vector3(-0.88f, 1.32f, 1.15f);
        tabletRoot.transform.rotation = Quaternion.Euler(14f, 32f, 0f);

        // Physical backplate
        GameObject backplate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backplate.name = "TabletBackplate";
        backplate.transform.SetParent(tabletRoot.transform, false);
        backplate.transform.localPosition = new Vector3(0f, 0f, 0.01f);
        backplate.transform.localScale = new Vector3(0.96f, 0.66f, 0.02f);

        Material tabletMat = SetupLabScene.CreateMaterial("Lab", "TabletFrameMat", new Color(0.06f, 0.09f, 0.14f), 0.85f, 0.8f, false, new Color(0.04f, 0.25f, 0.45f, 0.5f));
        backplate.GetComponent<Renderer>().sharedMaterial = tabletMat;

        // World Space Canvas
        GameObject canvasGO = new GameObject("TabletCanvas");
        canvasGO.transform.SetParent(tabletRoot.transform, false);
        canvasGO.transform.localPosition = Vector3.zero;
        canvasGO.transform.localRotation = Quaternion.identity;
        canvasGO.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(900f, 600f);

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        canvasGO.AddComponent<TrackedDeviceGraphicRaycaster>(); // Allows VR Laser Pointer clicks!

        // Font
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // Background
        GameObject bgGO = CreateUIElement("Background", canvasGO.transform, new Vector2(0f, 0f), new Vector2(900f, 600f));
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.04f, 0.07f, 0.12f, 0.94f);

        // Header Bar
        GameObject headerGO = CreateUIElement("HeaderBar", canvasGO.transform, new Vector2(0f, 260f), new Vector2(880f, 60f));
        Image headerImg = headerGO.AddComponent<Image>();
        headerImg.color = new Color(0.08f, 0.16f, 0.26f, 1f);

        GameObject titleGO = CreateUIElement("Title", headerGO.transform, new Vector2(-120f, 8f), new Vector2(600f, 30f));
        Text titleText = titleGO.AddComponent<Text>();
        titleText.font = font;
        titleText.fontSize = 20;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = new Color(0.4f, 0.85f, 1f);
        titleText.text = "DR. NOVA AI & CLINICAL SYSTEM (VR)";

        GameObject subGO = CreateUIElement("Subtitle", headerGO.transform, new Vector2(-120f, -14f), new Vector2(600f, 20f));
        Text subText = subGO.AddComponent<Text>();
        subText.font = font;
        subText.fontSize = 12;
        subText.color = new Color(0.7f, 0.85f, 0.95f);
        subText.text = "OpenXR 6-DOF Spatial Manipulation  |  Guyton & Gray's RAG Online";

        // Navigation Tabs Bar
        GameObject tabsGO = CreateUIElement("TabsBar", canvasGO.transform, new Vector2(0f, 205f), new Vector2(880f, 40f));
        VRClinicalTablet tablet = tabletRoot.AddComponent<VRClinicalTablet>();

        Button btnMetrics = CreateTabButton("TabMetrics", tabsGO.transform, new Vector2(-330f, 0f), new Vector2(160f, 34f), "Organ Metrics", font, () => tablet.SetTab(VRClinicalTablet.TabletTab.OrganMetrics));
        Button btnAI = CreateTabButton("TabAI", tabsGO.transform, new Vector2(-160f, 0f), new Vector2(160f, 34f), "Dr. Nova AI", font, () => tablet.SetTab(VRClinicalTablet.TabletTab.AITutor));
        Button btnLab = CreateTabButton("TabLab", tabsGO.transform, new Vector2(10f, 0f), new Vector2(160f, 34f), "Lab Controls", font, () => tablet.SetTab(VRClinicalTablet.TabletTab.LabControls));
        Button btnReset = CreateTabButton("TabReset", tabsGO.transform, new Vector2(180f, 0f), new Vector2(160f, 34f), "Reset Anatomy", font, () => tablet.ResetAllAnatomy());

        // --- Panel 1: Organ Metrics Panel ---
        GameObject metricsPanel = CreateUIElement("OrganMetricsPanel", canvasGO.transform, new Vector2(0f, -40f), new Vector2(880f, 440f));
        tablet.organMetricsPanel = metricsPanel;

        GameObject mNameGO = CreateUIElement("OrganName", metricsPanel.transform, new Vector2(-80f, 180f), new Vector2(680f, 35f));
        Text mNameText = mNameGO.AddComponent<Text>();
        mNameText.font = font;
        mNameText.fontSize = 24;
        mNameText.fontStyle = FontStyle.Bold;
        mNameText.color = new Color(0.95f, 0.85f, 0.3f);
        mNameText.text = "Heart & Great Vessels";
        tablet.organNameText = mNameText;

        GameObject mSysGO = CreateUIElement("OrganSystem", metricsPanel.transform, new Vector2(-80f, 145f), new Vector2(680f, 25f));
        Text mSysText = mSysGO.AddComponent<Text>();
        mSysText.font = font;
        mSysText.fontSize = 15;
        mSysText.color = new Color(0.4f, 0.9f, 0.7f);
        mSysText.text = "System: Cardiovascular | Mass: 310 g";
        tablet.organSystemText = mSysText;

        GameObject mPhysGO = CreateUIElement("OrganPhys", metricsPanel.transform, new Vector2(0f, 85f), new Vector2(840f, 75f));
        Text mPhysText = mPhysGO.AddComponent<Text>();
        mPhysText.font = font;
        mPhysText.fontSize = 14;
        mPhysText.color = Color.white;
        mPhysText.text = "Physiology: Four-chambered muscular organ generating systemic and pulmonary perfusion. Left ventricle generates 120 mmHg systolic pressure to supply systemic circulation.";
        tablet.organPhysiologyText = mPhysText;

        GameObject mVascGO = CreateUIElement("OrganVasc", metricsPanel.transform, new Vector2(0f, 0f), new Vector2(840f, 65f));
        Text mVascText = mVascGO.AddComponent<Text>();
        mVascText.font = font;
        mVascText.fontSize = 14;
        mVascText.color = new Color(0.85f, 0.9f, 1f);
        mVascText.text = "Vascular Supply: Left Coronary Artery (LAD + Cx) and Right Coronary Artery (PDA) originating from the aortic sinuses of Valsalva.";
        tablet.organVascularText = mVascText;

        GameObject mPathGO = CreateUIElement("OrganPath", metricsPanel.transform, new Vector2(0f, -75f), new Vector2(840f, 65f));
        Text mPathText = mPathGO.AddComponent<Text>();
        mPathText.font = font;
        mPathText.fontSize = 14;
        mPathText.color = new Color(1f, 0.75f, 0.75f);
        mPathText.text = "Clinical Pathology: Myocardial infarction (LAD occlusion = anterior STEMI), aortic stenosis, ventricular septal defect, infective endocarditis.";
        tablet.organPathologyText = mPathText;

        Button returnBtn = CreateActionButton("ReturnOrganBtn", metricsPanel.transform, new Vector2(0f, -165f), new Vector2(320f, 45f), "Magnetically Return to Socket", font, new Color(0.12f, 0.45f, 0.75f));
        tablet.returnOrganButton = returnBtn;

        // --- Panel 2: Dr. Nova AI Tutor Panel ---
        GameObject aiPanel = CreateUIElement("AITutorPanel", canvasGO.transform, new Vector2(0f, -40f), new Vector2(880f, 440f));
        tablet.aiTutorPanel = aiPanel;
        aiPanel.SetActive(false);

        GameObject statusGO = CreateUIElement("TutorStatus", aiPanel.transform, new Vector2(0f, 185f), new Vector2(840f, 30f));
        Text statusText = statusGO.AddComponent<Text>();
        statusText.font = font;
        statusText.fontSize = 16;
        statusText.fontStyle = FontStyle.Bold;
        statusText.color = new Color(0.35f, 0.9f, 0.6f);
        statusText.text = "Dr. Nova (AI Clinical Assistant - Live RAG Online)";
        tablet.tutorStatusText = statusText;

        GameObject resGO = CreateUIElement("TutorResponse", aiPanel.transform, new Vector2(0f, 75f), new Vector2(840f, 160f));
        Text resText = resGO.AddComponent<Text>();
        resText.font = font;
        resText.fontSize = 14;
        resText.color = Color.white;
        resText.text = "Select a clinical inquiry below or speak to Dr. Nova. Live RAG citations and Windows PCM text-to-speech audio are streamed directly to your VR headset.";
        tablet.tutorResponseText = resText;

        GameObject srcGO = CreateUIElement("TutorSources", aiPanel.transform, new Vector2(0f, -20f), new Vector2(840f, 30f));
        Text srcText = srcGO.AddComponent<Text>();
        srcText.font = font;
        srcText.fontSize = 12;
        srcText.color = new Color(0.6f, 0.8f, 0.95f);
        srcText.text = "Sources: Guyton & Hall (14th Ed), Gray's Anatomy (4th Ed)";
        tablet.tutorSourcesText = srcText;

        // Quick inquiry buttons
        Button q1Btn = CreateActionButton("AskAnginaBtn", aiPanel.transform, new Vector2(-280f, -80f), new Vector2(260f, 38f), "Why Arm Angina?", font, new Color(0.15f, 0.28f, 0.45f));
        tablet.askAnginaButton = q1Btn;

        Button q2Btn = CreateActionButton("AskPortalBtn", aiPanel.transform, new Vector2(0f, -80f), new Vector2(260f, 38f), "Hepatic Portal Flow", font, new Color(0.15f, 0.28f, 0.45f));
        tablet.askPortalButton = q2Btn;

        Button q3Btn = CreateActionButton("AskCarinaBtn", aiPanel.transform, new Vector2(280f, -80f), new Vector2(260f, 38f), "Tracheal Carina", font, new Color(0.15f, 0.28f, 0.45f));
        tablet.askCarinaButton = q3Btn;

        Button listenBtn = CreateActionButton("ListenTTSBtn", aiPanel.transform, new Vector2(0f, -150f), new Vector2(300f, 44f), "Listen [TTS Audio Stream]", font, new Color(0.18f, 0.55f, 0.35f));
        tablet.listenTTSButton = listenBtn;

        // --- Panel 3: Lab Controls Panel ---
        GameObject labPanel = CreateUIElement("LabControlsPanel", canvasGO.transform, new Vector2(0f, -40f), new Vector2(880f, 440f));
        tablet.labControlsPanel = labPanel;
        labPanel.SetActive(false);

        GameObject labTitleGO = CreateUIElement("LabTitle", labPanel.transform, new Vector2(0f, 175f), new Vector2(840f, 35f));
        Text labTitleText = labTitleGO.AddComponent<Text>();
        labTitleText.font = font;
        labTitleText.fontSize = 20;
        labTitleText.fontStyle = FontStyle.Bold;
        labTitleText.color = new Color(0.4f, 0.85f, 1f);
        labTitleText.text = "Advanced Spatial Anatomy Modes";

        Button expBtn = CreateActionButton("ExplodeBtn", labPanel.transform, new Vector2(-220f, 70f), new Vector2(360f, 60f), "Toggle Exploded View", font, new Color(0.2f, 0.4f, 0.65f));
        tablet.explodeButton = expBtn;

        Button xrayBtn = CreateActionButton("XrayBtn", labPanel.transform, new Vector2(220f, 70f), new Vector2(360f, 60f), "Toggle X-Ray Fluoroscopy", font, new Color(0.2f, 0.4f, 0.65f));
        tablet.xrayButton = xrayBtn;

        Button rstAllBtn = CreateActionButton("ResetAllBtn", labPanel.transform, new Vector2(0f, -40f), new Vector2(400f, 55f), "Reset All Organs to Body", font, new Color(0.7f, 0.25f, 0.25f));
        tablet.resetAllButton = rstAllBtn;

        // Connect LLM Client if present
        LLMAnatomyClient llm = UnityEngine.Object.FindFirstObjectByType<LLMAnatomyClient>();
        if (llm == null)
        {
            GameObject llmGO = new GameObject("LLMAnatomyClient");
            llm = llmGO.AddComponent<LLMAnatomyClient>();
        }

        Debug.Log("[Phase 6] VR Clinical Tablet successfully created and configured.");
    }

    private static void SetupVRLabManager()
    {
        GameObject mgrGO = GameObject.Find("VRLabManager");
        if (mgrGO == null) mgrGO = new GameObject("VRLabManager");
        VRLabManager mgr = mgrGO.GetComponent<VRLabManager>();
        if (mgr == null) mgr = mgrGO.AddComponent<VRLabManager>();
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

    private static Button CreateTabButton(string name, Transform parent, Vector2 pos, Vector2 size, string text, Font font, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = CreateUIElement(name, parent, pos, size);
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.12f, 0.22f, 0.35f, 1f);

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.2f, 0.45f, 0.7f, 1f);
        cb.pressedColor = new Color(0.1f, 0.6f, 0.9f, 1f);
        btn.colors = cb;
        if (onClick != null) btn.onClick.AddListener(onClick);

        GameObject labelGO = CreateUIElement("Label", go.transform, Vector2.zero, size);
        Text labelText = labelGO.AddComponent<Text>();
        labelText.font = font;
        labelText.fontSize = 14;
        labelText.fontStyle = FontStyle.Bold;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = Color.white;
        labelText.text = text;

        return btn;
    }

    private static Button CreateActionButton(string name, Transform parent, Vector2 pos, Vector2 size, string text, Font font, Color baseColor)
    {
        GameObject go = CreateUIElement(name, parent, pos, size);
        Image img = go.AddComponent<Image>();
        img.color = baseColor;

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.highlightedColor = baseColor * 1.3f;
        cb.pressedColor = baseColor * 1.6f;
        btn.colors = cb;

        GameObject labelGO = CreateUIElement("Label", go.transform, Vector2.zero, size);
        Text labelText = labelGO.AddComponent<Text>();
        labelText.font = font;
        labelText.fontSize = 15;
        labelText.fontStyle = FontStyle.Bold;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = Color.white;
        labelText.text = text;

        return btn;
    }
}
#endif
