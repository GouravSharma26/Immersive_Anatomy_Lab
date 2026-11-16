#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class SetupLabScene
{
    private const string SETUP_KEY_V3 = "ImmersiveAnatomyLab_Phase3_SetupDone_v4_auto";

    static SetupLabScene()
    {
        EditorApplication.delayCall += CheckAndSetup;
    }

    [MenuItem("Tools/Build Phase 3 Advanced Interaction _F12")]
    public static void RunSetupManual()
    {
        BuildMainLabScene(true);
        FrameAnatomy();
    }

    [MenuItem("Tools/Frame Anatomy In Scene")]
    public static void FrameAnatomy()
    {
        GameObject heart = GameObject.Find("Heart");
        if (heart != null)
        {
            Selection.activeGameObject = heart;
        }

        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.LookAt(new Vector3(0f, 1.7f, 1.8f), Quaternion.Euler(12f, 180f, 0f), 2.8f);
            SceneView.lastActiveSceneView.Repaint();
        }

        ExpandHierarchy();
    }

    [MenuItem("Tools/Expand Hierarchy")]
    public static void ExpandHierarchy()
    {
        var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
        if (type == null) return;
        var window = EditorWindow.GetWindow(type);
        if (window == null) return;

        var expMethod = type.GetMethod("SetExpandedRecursive", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (expMethod == null)
        {
            expMethod = type.GetMethod("SetExpanded", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new System.Type[] { typeof(int), typeof(bool) }, null);
        }

        string[] objNames = new string[] { "MainLab", "Environment", "Player", "Anatomy", "HumanBody", "SkeletalSystem", "Heart", "Lungs", "Brain", "DigestiveSystem", "Systems", "UI" };
        foreach (var name in objNames)
        {
            GameObject go = GameObject.Find(name);
            if (go != null && expMethod != null)
            {
                expMethod.Invoke(window, new object[] { go.GetInstanceID(), true });
            }
        }
        window.Repaint();
    }

    private static void CheckAndSetup()
    {
        if (!EditorPrefs.GetBool(SETUP_KEY_V3, false))
        {
            BuildMainLabScene(true);
            FrameAnatomy();
            EditorPrefs.SetBool(SETUP_KEY_V3, true);
        }
    }

    public static Material CreateMaterial(string subfolder, string name, Color color, float smoothness = 0.5f, float metallic = 0.1f, bool transparent = false, Color? emissionColor = null)
    {
        string folder = string.IsNullOrEmpty(subfolder) ? "Assets/Materials" : $"Assets/Materials/{subfolder}";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            string[] parts = subfolder.Split('/');
            string current = "Assets/Materials";
            foreach (var p in parts)
            {
                string next = $"{current}/{p}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, p);
                current = next;
            }
        }

        string path = $"{folder}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Diffuse");

            mat = new Material(shader);
            mat.name = name;

            if (transparent)
            {
                if (mat.HasProperty("_Surface"))
                {
                    mat.SetFloat("_Surface", 1); // 1 = Transparent
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                }
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);

            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

            if (emissionColor.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissionColor.Value);
            }

            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (emissionColor.HasValue && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionColor.Value);
            }
            EditorUtility.SetDirty(mat);
        }
        return mat;
    }

    private static GameObject InstantiateMeshAsset(string relativePath, string goName, Material mat)
    {
        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(relativePath);
        GameObject instance;
        if (modelPrefab != null)
        {
            instance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
            instance.name = goName;
        }
        else
        {
            instance = new GameObject(goName);
        }

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            if (mat != null) r.material = mat;
            MeshFilter mf = r.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                MeshCollider mc = r.gameObject.GetComponent<MeshCollider>();
                if (mc == null) mc = r.gameObject.AddComponent<MeshCollider>();
                mc.convex = true;
            }
        }
        return instance;
    }

    public static void BuildMainLabScene(bool forceRecreate)
    {
        string scenePath = "Assets/Scenes/MainLab.unity";
        Debug.Log("[SetupLabScene] Building MainLab Phase 3 Advanced Interaction Scene...");

        // 1. Materials setup
        Material floorMat = CreateMaterial("", "M_Floor", new Color(0.18f, 0.20f, 0.24f), 0.8f, 0.1f);
        Material wallMat = CreateMaterial("", "M_Wall", new Color(0.88f, 0.90f, 0.92f), 0.2f, 0.0f);
        Material tableMat = CreateMaterial("", "M_Table", new Color(0.10f, 0.12f, 0.15f), 0.75f, 0.5f);

        // Body & Organs Materials
        Material bodyTranslucentMat = CreateMaterial("Body", "M_Body_Translucent", new Color(0.72f, 0.86f, 1.0f, 0.22f), 0.6f, 0.05f, true);
        Material heartMat = CreateMaterial("Organs", "M_Heart_Realistic", new Color(0.82f, 0.12f, 0.16f), 0.70f, 0.12f);
        Material lungsMat = CreateMaterial("Organs", "M_Lungs_Realistic", new Color(0.92f, 0.58f, 0.62f), 0.45f, 0.02f);
        Material brainMat = CreateMaterial("Organs", "M_Brain_Realistic", new Color(0.92f, 0.76f, 0.72f), 0.52f, 0.02f);
        Material liverMat = CreateMaterial("Organs", "M_Liver_Realistic", new Color(0.55f, 0.18f, 0.14f), 0.65f, 0.10f);
        Material stomachMat = CreateMaterial("Organs", "M_Stomach_Realistic", new Color(0.85f, 0.65f, 0.48f), 0.50f, 0.05f);
        Material skeletonMat = CreateMaterial("Organs", "M_Skeleton_Realistic", new Color(0.94f, 0.92f, 0.85f), 0.35f, 0.05f);

        // Highlights & X-Ray Materials
        Material hoverMat = CreateMaterial("Highlights", "M_Highlight_Hover", new Color(0.2f, 0.9f, 1.0f, 0.85f), 0.8f, 0.2f, false, new Color(0.1f, 0.7f, 1.0f) * 1.5f);
        Material selectMat = CreateMaterial("Highlights", "M_Highlight_Select", new Color(1.0f, 0.85f, 0.2f, 0.9f), 0.8f, 0.2f, false, new Color(1.0f, 0.8f, 0.1f) * 1.8f);
        Material ghostMat = CreateMaterial("Highlights", "M_Ghost_Dimmed", new Color(0.35f, 0.45f, 0.55f, 0.12f), 0.2f, 0.0f, true);
        Material xrayMat = CreateMaterial("Highlights", "M_XRay_Fluoroscopy", new Color(0.15f, 0.75f, 1.0f, 0.20f), 0.8f, 0.1f, true, new Color(0.1f, 0.6f, 0.9f) * 1.2f);

        // 2. Create blank scene
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Directional Light
        GameObject dirLightObj = new GameObject("Directional Light");
        Light dirLight = dirLightObj.AddComponent<Light>();
        dirLight.type = LightType.Directional;
        dirLight.intensity = 1.3f;
        dirLight.color = new Color(1.0f, 0.98f, 0.95f);
        dirLightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Fill Light
        GameObject fillLightObj = new GameObject("Lab Fill Light");
        Light fillLight = fillLightObj.AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.intensity = 0.55f;
        fillLight.color = new Color(0.75f, 0.88f, 1.0f);
        fillLightObj.transform.rotation = Quaternion.Euler(30f, 150f, 0f);

        // 3. Player
        GameObject playerObj = new GameObject("Player");
        playerObj.transform.position = new Vector3(0f, 0f, -5f);
        CharacterController cc = playerObj.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0f, 0.9f, 0f);

        DesktopPlayerController playerCtrl = playerObj.AddComponent<DesktopPlayerController>();

        GameObject camObj = new GameObject("Main Camera");
        camObj.transform.SetParent(playerObj.transform, false);
        camObj.transform.localPosition = new Vector3(0f, 1.55f, 0f);
        camObj.transform.localRotation = Quaternion.identity;
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 1000f;
        camObj.AddComponent<AudioListener>();

        playerCtrl.playerCamera = camObj.transform;

        // 4. Environment
        GameObject envObj = new GameObject("Environment");

        GameObject floorObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floorObj.name = "Floor";
        floorObj.transform.SetParent(envObj.transform);
        floorObj.transform.position = Vector3.zero;
        floorObj.transform.localScale = new Vector3(5f, 1f, 5f);
        if (floorMat != null) floorObj.GetComponent<Renderer>().material = floorMat;

        GameObject backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backWall.name = "BackWall";
        backWall.transform.SetParent(envObj.transform);
        backWall.transform.position = new Vector3(0f, 2.5f, 10f);
        backWall.transform.localScale = new Vector3(20f, 5f, 0.3f);
        if (wallMat != null) backWall.GetComponent<Renderer>().material = wallMat;

        GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftWall.name = "LeftWall";
        leftWall.transform.SetParent(envObj.transform);
        leftWall.transform.position = new Vector3(-10f, 2.5f, 0f);
        leftWall.transform.localScale = new Vector3(0.3f, 5f, 20f);
        if (wallMat != null) leftWall.GetComponent<Renderer>().material = wallMat;

        GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightWall.name = "RightWall";
        rightWall.transform.SetParent(envObj.transform);
        rightWall.transform.position = new Vector3(10f, 2.5f, 0f);
        rightWall.transform.localScale = new Vector3(0.3f, 5f, 20f);
        if (wallMat != null) rightWall.GetComponent<Renderer>().material = wallMat;

        GameObject labTable = GameObject.CreatePrimitive(PrimitiveType.Cube);
        labTable.name = "LabTable";
        labTable.transform.SetParent(envObj.transform);
        labTable.transform.position = new Vector3(0f, 0.8f, 2f);
        labTable.transform.localScale = new Vector3(5f, 0.3f, 3f);
        if (tableMat != null) labTable.GetComponent<Renderer>().material = tableMat;

        // 5. Anatomy Systems Root
        GameObject anatomyObj = new GameObject("Anatomy");

        // A. Human Body Translucent Silhouette
        GameObject humanBody = InstantiateMeshAsset("Assets/Models/HumanBody/human_body.obj", "HumanBody", bodyTranslucentMat);
        humanBody.transform.SetParent(anatomyObj.transform, false);
        humanBody.transform.position = new Vector3(0f, 1.7f, 2.0f);
        foreach (var c in humanBody.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);

        // B. Skeletal System
        GameObject skeleton = InstantiateMeshAsset("Assets/Models/BodySystems/skeletal_system.obj", "SkeletalSystem", skeletonMat);
        skeleton.transform.SetParent(anatomyObj.transform, false);
        skeleton.transform.position = new Vector3(0f, 1.7f, 2.0f);

        // C. Cardiovascular System (Heart)
        GameObject heart = InstantiateMeshAsset("Assets/Models/Organs/heart.obj", "Heart", heartMat);
        heart.transform.SetParent(anatomyObj.transform, false);
        heart.transform.position = new Vector3(-0.08f, 1.82f, 1.88f);
        OrganInteractable heartInfo = heart.AddComponent<OrganInteractable>();
        heartInfo.organName = "Human Heart";
        heartInfo.bodySystem = "Cardiovascular";
        heartInfo.normalWeight = 310f;
        heartInfo.description = "Four-chambered muscular organ powering systemic and pulmonary circulation.";
        heartInfo.arterialSupply = "Left & Right Coronary Arteries (arising from aortic sinuses).";
        heartInfo.innervation = "Cardiac plexus (Vagus nerve [CN X] parasympathetic & T1-T4 sympathetic).";
        heartInfo.clinicalFunction = "Pumps ~70 mL/beat, maintaining ~5 L/min cardiac output.";
        heartInfo.commonPathology = "Coronary artery disease, myocardial infarction, arrhythmias.";

        // D. Respiratory System (Lungs & Trachea)
        GameObject lungs = InstantiateMeshAsset("Assets/Models/Organs/lungs.obj", "Lungs", lungsMat);
        lungs.transform.SetParent(anatomyObj.transform, false);
        lungs.transform.position = new Vector3(0f, 1.95f, 1.85f);
        OrganInteractable lungsInfo = lungs.AddComponent<OrganInteractable>();
        lungsInfo.organName = "Lungs & Trachea";
        lungsInfo.bodySystem = "Respiratory";
        lungsInfo.normalWeight = 1050f;
        lungsInfo.description = "Primary gas-exchange organs. Right lung has 3 lobes; left lung has 2 lobes with cardiac notch.";
        lungsInfo.arterialSupply = "Bronchial arteries (nutritive) & Pulmonary arteries (gas exchange).";
        lungsInfo.innervation = "Pulmonary plexus (Vagus CN X & sympathetic chains).";
        lungsInfo.clinicalFunction = "Alveolar gas diffusion: uptakes atmospheric O2 and eliminates CO2.";
        lungsInfo.commonPathology = "Pneumonia, COPD, asthma, pulmonary embolism.";

        // E. Nervous System (Brain)
        GameObject brain = InstantiateMeshAsset("Assets/Models/Organs/brain.obj", "Brain", brainMat);
        brain.transform.SetParent(anatomyObj.transform, false);
        brain.transform.position = new Vector3(0f, 2.75f, 1.90f);
        OrganInteractable brainInfo = brain.AddComponent<OrganInteractable>();
        brainInfo.organName = "Human Brain";
        brainInfo.bodySystem = "Nervous";
        brainInfo.normalWeight = 1400f;
        brainInfo.description = "Central command organ of the nervous system: cerebrum, cerebellum, and brainstem.";
        brainInfo.arterialSupply = "Circle of Willis (internal carotid and vertebral-basilar system).";
        brainInfo.innervation = "12 pairs of cranial nerves and 31 pairs of spinal segments.";
        brainInfo.clinicalFunction = "Cognition, motor planning, sensory processing, autonomic regulation.";
        brainInfo.commonPathology = "Stroke (ischemic/hemorrhagic), traumatic brain injury, Alzheimer's.";

        // F. Digestive System (Liver & Stomach)
        GameObject digestiveSys = new GameObject("DigestiveSystem");
        digestiveSys.transform.SetParent(anatomyObj.transform, false);
        digestiveSys.transform.position = new Vector3(0f, 1.7f, 2.0f);

        GameObject liver = InstantiateMeshAsset("Assets/Models/Organs/liver.obj", "Liver", liverMat);
        liver.transform.SetParent(digestiveSys.transform, false);
        liver.transform.position = new Vector3(0.28f, 1.48f, 1.88f);
        OrganInteractable liverInfo = liver.AddComponent<OrganInteractable>();
        liverInfo.organName = "Human Liver";
        liverInfo.bodySystem = "Digestive / Metabolic";
        liverInfo.normalWeight = 1550f;
        liverInfo.description = "Largest internal visceral organ, divided into right and left lobes by falciform ligament.";
        liverInfo.arterialSupply = "Dual supply: Hepatic artery proper (25%) & Hepatic portal vein (75%).";
        liverInfo.innervation = "Hepatic plexus (Celiac ganglia and Vagus nerve).";
        liverInfo.clinicalFunction = "Bile secretion for fat emulsification, glycogen storage, protein synthesis, detoxification.";
        liverInfo.commonPathology = "Cirrhosis, viral hepatitis, NAFLD/NASH, hepatocellular carcinoma.";

        GameObject stomach = InstantiateMeshAsset("Assets/Models/Organs/stomach.obj", "Stomach", stomachMat);
        stomach.transform.SetParent(digestiveSys.transform, false);
        stomach.transform.position = new Vector3(-0.12f, 1.35f, 1.90f);
        OrganInteractable stomachInfo = stomach.AddComponent<OrganInteractable>();
        stomachInfo.organName = "Stomach & Intestines";
        stomachInfo.bodySystem = "Digestive";
        stomachInfo.normalWeight = 1200f;
        stomachInfo.description = "J-shaped muscular organ and enteric tract for chemical digestion and nutrient absorption.";
        stomachInfo.arterialSupply = "Celiac trunk & Mesenteric arteries.";
        stomachInfo.innervation = "Enteric nervous system with Vagus nerve parasympathetic input.";
        stomachInfo.clinicalFunction = "Secretes gastric acid (HCl) and pepsin; peristaltic churning into chyme.";
        stomachInfo.commonPathology = "Peptic ulcer disease, GERD, gastritis, Crohn's disease.";

        // 6. Prefabs
        SavePrefabIfPossible(heart, "Assets/Prefabs/Organs/Heart.prefab");
        SavePrefabIfPossible(lungs, "Assets/Prefabs/Organs/Lungs.prefab");
        SavePrefabIfPossible(brain, "Assets/Prefabs/Organs/Brain.prefab");
        SavePrefabIfPossible(liver, "Assets/Prefabs/Organs/Liver.prefab");
        SavePrefabIfPossible(stomach, "Assets/Prefabs/Organs/Stomach.prefab");
        SavePrefabIfPossible(skeleton, "Assets/Prefabs/BodySystems/SkeletalSystem.prefab");
        SavePrefabIfPossible(humanBody, "Assets/Prefabs/BodySystems/HumanBody.prefab");

        // 7. Systems GameObject Configuration
        GameObject systemsObj = new GameObject("Systems");
        BodySystemManager sysMgr = systemsObj.AddComponent<BodySystemManager>();
        sysMgr.RegisterSystem("Body", humanBody);
        sysMgr.RegisterSystem("Skeletal", skeleton);
        sysMgr.RegisterSystem("Cardiovascular", heart);
        sysMgr.RegisterSystem("Respiratory", lungs);
        sysMgr.RegisterSystem("Nervous", brain);
        sysMgr.RegisterSystem("Digestive", digestiveSys);

        OrganHighlighter highlighter = systemsObj.AddComponent<OrganHighlighter>();
        highlighter.hoverMaterial = hoverMat;
        highlighter.selectMaterial = selectMat;
        highlighter.ghostMaterial = ghostMat;

        OrganIsolationController isolationCtrl = systemsObj.AddComponent<OrganIsolationController>();
        isolationCtrl.playerCamera = cam;
        isolationCtrl.highlighter = highlighter;
        isolationCtrl.ghostMaterial = ghostMat;

        GameObject focalPt = new GameObject("InspectionFocalPoint");
        focalPt.transform.position = new Vector3(0f, 1.50f, 0.2f);
        isolationCtrl.inspectionPoint = focalPt.transform;

        // Phase 3: Exploded View Controller
        ExplodedViewController explodedView = systemsObj.AddComponent<ExplodedViewController>();
        explodedView.RegisterPart("Brain", brain.transform, new Vector3(0f, 0.65f, 0.15f));
        explodedView.RegisterPart("Heart", heart.transform, new Vector3(-0.25f, 0.05f, 0.60f));
        explodedView.RegisterPart("Lungs", lungs.transform, new Vector3(0f, 0.10f, 0.40f));
        explodedView.RegisterPart("DigestiveSystem", digestiveSys.transform, new Vector3(0.35f, -0.15f, 0.50f));
        explodedView.RegisterPart("SkeletalSystem", skeleton.transform, new Vector3(0f, 0f, 0.25f));
        explodedView.RegisterPart("HumanBody", humanBody.transform, new Vector3(0f, 0f, -0.45f));

        // Phase 3: Organ Comparison Controller
        OrganComparisonController comparisonCtrl = systemsObj.AddComponent<OrganComparisonController>();
        GameObject leftPedestal = new GameObject("LeftComparisonPedestal");
        leftPedestal.transform.position = new Vector3(-0.45f, 1.50f, 0.40f);
        comparisonCtrl.leftPedestal = leftPedestal.transform;

        GameObject rightPedestal = new GameObject("RightComparisonPedestal");
        rightPedestal.transform.position = new Vector3(0.45f, 1.50f, 0.40f);
        comparisonCtrl.rightPedestal = rightPedestal.transform;

        // Phase 3: X-Ray View Controller
        XRayViewController xrayCtrl = systemsObj.AddComponent<XRayViewController>();
        xrayCtrl.xrayMaterial = xrayMat;
        xrayCtrl.bodyMannequin = humanBody;
        xrayCtrl.skeletalSystem = skeleton;

        // Phase 3: Camera Focus and Orbit Inspection Controller
        CameraInspectionController camInspectCtrl = systemsObj.AddComponent<CameraInspectionController>();
        camInspectCtrl.targetCamera = cam;
        camInspectCtrl.playerController = playerCtrl;

        // Core Anatomy Interaction Coordinator
        AnatomyInteraction anatomyInteraction = systemsObj.AddComponent<AnatomyInteraction>();
        anatomyInteraction.playerCamera = cam;
        anatomyInteraction.interactionDistance = 15f;
        anatomyInteraction.highlighter = highlighter;
        anatomyInteraction.isolationController = isolationCtrl;
        anatomyInteraction.explodedViewController = explodedView;
        anatomyInteraction.comparisonController = comparisonCtrl;
        anatomyInteraction.xrayViewController = xrayCtrl;
        anatomyInteraction.cameraInspectionController = camInspectCtrl;

        // 8. UI GameObject
        GameObject uiObj = new GameObject("UI");

        // Save Scene
        EditorSceneManager.SaveScene(newScene, scenePath);
        Debug.Log($"[SetupLabScene] Phase 3 MainLab scene successfully saved to {scenePath}");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void SavePrefabIfPossible(GameObject go, string path)
    {
        string dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        try
        {
            PrefabUtility.SaveAsPrefabAssetAndConnect(go, path, InteractionMode.AutomatedAction);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SetupLabScene] Could not save prefab {path}: {ex.Message}");
        }
    }
}
#endif
