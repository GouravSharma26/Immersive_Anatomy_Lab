#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ImmersiveAnatomy.Editor
{
    public static class Phase7EnvironmentBuilder
    {
        private const string SCENE_PATH = "Assets/Scenes/VR_MainLab.unity";
        private const string MAT_DIR = "Assets/Art/Phase7/Materials";
        private const string PREFAB_DIR = "Assets/Prefabs/Phase7";
        private const string FLAG_PATH = "Temp/run_phase71.flag";

        [InitializeOnLoadMethod]
        private static void OnEditorLoad()
        {
            if (File.Exists(FLAG_PATH))
            {
                try { File.Delete(FLAG_PATH); } catch { }
                EditorApplication.delayCall += () =>
                {
                    ExecuteEnvironmentTransformation();
                };
            }
        }

        [MenuItem("Tools/Phase 7/Transform Laboratory Environment (Phase 7.1)")]
        public static void ExecuteEnvironmentTransformation()
        {
            Debug.Log("[Phase 7.1] Starting Laboratory Environment Transformation...");

            // 1. Ensure Folder Structure
            EnsureFolders();

            // 2. Create / Update PBR Materials
            var mats = BuildPBRMaterials();

            // 3. Open Scene
            Scene scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[Phase 7.1] Failed to open scene at {SCENE_PATH}");
                return;
            }

            // 4. Transform Scene Environment
            TransformEnvironment(mats);

            // 5. Mark Dirty and Save
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[Phase 7.1] Laboratory Environment Transformation COMPLETE and saved to " + SCENE_PATH);
        }

        private static void EnsureFolders()
        {
            string[] dirs = new string[]
            {
                "Assets/Art",
                "Assets/Art/Phase7",
                "Assets/Art/Phase7/Environment",
                "Assets/Art/Phase7/Furniture",
                "Assets/Art/Phase7/Props",
                "Assets/Art/Phase7/Textures",
                "Assets/Art/Phase7/Materials",
                "Assets/Art/Phase7/Lighting",
                "Assets/Prefabs",
                "Assets/Prefabs/Phase7",
                "Assets/Prefabs/Phase7/Environment",
                "Assets/Prefabs/Phase7/Furniture"
            };

            foreach (var d in dirs)
            {
                if (!AssetDatabase.IsValidFolder(d))
                {
                    string parent = Path.GetDirectoryName(d).Replace('\\', '/');
                    string child = Path.GetFileName(d);
                    AssetDatabase.CreateFolder(parent, child);
                }
            }
            AssetDatabase.Refresh();
        }

        public class Phase7Materials
        {
            public Material floorPolished;
            public Material wallClinical;
            public Material wallGraphite;
            public Material glassPartition;
            public Material brushedMetal;
            public Material steelChrome;
            public Material ceramicSurface;
            public Material ledCyan;
            public Material ledWhite;
            public Material consoleSleek;
        }

        public static Phase7Materials BuildPBRMaterials()
        {
            var p = new Phase7Materials();

            // 1. M7_Floor_Polished - Deep slate reflective epoxy
            p.floorPolished = CreateOrUpdateMaterial("M7_Floor_Polished", new Color(0.06f, 0.08f, 0.12f), metallic: 0.25f, smoothness: 0.94f);

            // 2. M7_Wall_Clinical - Sterile off-white clinical composite
            p.wallClinical = CreateOrUpdateMaterial("M7_Wall_Clinical", new Color(0.88f, 0.90f, 0.92f), metallic: 0.05f, smoothness: 0.35f);

            // 3. M7_Wall_Graphite - Dark acoustic wainscoting
            p.wallGraphite = CreateOrUpdateMaterial("M7_Wall_Graphite", new Color(0.11f, 0.13f, 0.17f), metallic: 0.15f, smoothness: 0.45f);

            // 4. M7_Glass_Partition - Translucent tinted architectural safety glass
            p.glassPartition = CreateOrUpdateMaterial("M7_Glass_Partition", new Color(0.08f, 0.22f, 0.30f, 0.25f), metallic: 0.10f, smoothness: 0.98f, transparent: true);

            // 5. M7_Brushed_Metal - Anodized surgical aluminum & conduits
            p.brushedMetal = CreateOrUpdateMaterial("M7_Brushed_Metal", new Color(0.55f, 0.58f, 0.64f), metallic: 0.85f, smoothness: 0.68f);

            // 6. M7_Steel_Chrome - High specular surgical stainless steel (Table & Pedestals)
            p.steelChrome = CreateOrUpdateMaterial("M7_Steel_Chrome", new Color(0.82f, 0.85f, 0.88f), metallic: 0.96f, smoothness: 0.90f);

            // 7. M7_Ceramic_Surface - Sterile white lab counter surfaces
            p.ceramicSurface = CreateOrUpdateMaterial("M7_Ceramic_Surface", new Color(0.95f, 0.96f, 0.97f), metallic: 0.05f, smoothness: 0.82f);

            // 8. M7_LED_Cyan - Telemetry bioluminescent glow
            p.ledCyan = CreateOrUpdateMaterial("M7_LED_Cyan", new Color(0f, 0.9f, 1f), metallic: 0.0f, smoothness: 0.5f, emissionColor: new Color(0f, 0.9f, 1f) * 2.8f);

            // 9. M7_LED_White - Daylight 5500K linear lighting
            p.ledWhite = CreateOrUpdateMaterial("M7_LED_White", new Color(0.98f, 0.99f, 1f), metallic: 0.0f, smoothness: 0.5f, emissionColor: new Color(0.98f, 0.99f, 1f) * 3.2f);

            // 10. M7_Console_Sleek - Dark equipment chassis
            p.consoleSleek = CreateOrUpdateMaterial("M7_Console_Sleek", new Color(0.14f, 0.16f, 0.20f), metallic: 0.45f, smoothness: 0.75f);

            AssetDatabase.SaveAssets();
            return p;
        }

        private static Material CreateOrUpdateMaterial(string name, Color baseColor, float metallic, float smoothness, bool transparent = false, Color? emissionColor = null)
        {
            string path = $"{MAT_DIR}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }

            mat.color = baseColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

            if (transparent)
            {
                mat.SetFloat("_Surface", 1); // Transparent in URP
                mat.SetFloat("_Blend", 0);   // Alpha blend
                mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)RenderQueue.Transparent;
            }
            else
            {
                mat.SetFloat("_Surface", 0); // Opaque in URP
                mat.SetInt("_SrcBlend", (int)BlendMode.One);
                mat.SetInt("_DstBlend", (int)BlendMode.Zero);
                mat.SetInt("_ZWrite", 1);
                mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)RenderQueue.Geometry;
            }

            if (emissionColor.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissionColor.Value);
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void TransformEnvironment(Phase7Materials mats)
        {
            // --- 1. Root Container for new architectural assets ---
            GameObject envRoot = GameObject.Find("Phase7_Environment");
            if (envRoot != null) UnityEngine.Object.DestroyImmediate(envRoot);
            envRoot = new GameObject("Phase7_Environment");

            // --- 2. Upgrade Existing Floor ---
            GameObject floor = GameObject.Find("Floor");
            if (floor != null)
            {
                floor.GetComponent<Renderer>().sharedMaterial = mats.floorPolished;
                // Add perimeter subtle bevel trim under floor
                GameObject floorBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floorBorder.name = "Floor_Perimeter_Trim";
                floorBorder.transform.SetParent(envRoot.transform, false);
                floorBorder.transform.position = new Vector3(0f, -0.05f, 0f);
                floorBorder.transform.localScale = new Vector3(16.4f, 0.1f, 16.4f);
                floorBorder.GetComponent<Renderer>().sharedMaterial = mats.wallGraphite;
                if (floorBorder.GetComponent<Collider>() != null) UnityEngine.Object.DestroyImmediate(floorBorder.GetComponent<Collider>());
            }

            // --- 3. Upgrade Existing Walls with Clinical Material ---
            string[] wallNames = new string[] { "BackWall", "FrontWall", "LeftWall", "RightWall" };
            foreach (var wn in wallNames)
            {
                GameObject w = GameObject.Find(wn);
                if (w != null)
                {
                    w.GetComponent<Renderer>().sharedMaterial = mats.wallClinical;
                }
            }

            // --- 4. Add Graphite Baseboards & Architectural Pilasters ---
            GameObject archGroup = new GameObject("Architecture");
            archGroup.transform.SetParent(envRoot.transform, false);

            // Wainscoting / Baseboards (Height 1.1m around perimeter)
            CreateArchBox(archGroup.transform, "Baseboard_Back", new Vector3(0f, 0.55f, 7.42f), new Vector3(15.9f, 1.1f, 0.08f), mats.wallGraphite);
            CreateArchBox(archGroup.transform, "Baseboard_Front", new Vector3(0f, 0.55f, -7.42f), new Vector3(15.9f, 1.1f, 0.08f), mats.wallGraphite);
            CreateArchBox(archGroup.transform, "Baseboard_Left", new Vector3(-7.42f, 0.55f, 0f), new Vector3(0.08f, 1.1f, 15.9f), mats.wallGraphite);
            CreateArchBox(archGroup.transform, "Baseboard_Right", new Vector3(7.42f, 0.55f, 0f), new Vector3(0.08f, 1.1f, 15.9f), mats.wallGraphite);

            // Horizontal Cyan Accent Trims (At Y = 1.12m)
            CreateArchBox(archGroup.transform, "AccentStrip_Back", new Vector3(0f, 1.12f, 7.41f), new Vector3(15.8f, 0.03f, 0.05f), mats.ledCyan);
            CreateArchBox(archGroup.transform, "AccentStrip_Front", new Vector3(0f, 1.12f, -7.41f), new Vector3(15.8f, 0.03f, 0.05f), mats.ledCyan);
            CreateArchBox(archGroup.transform, "AccentStrip_Left", new Vector3(-7.41f, 1.12f, 0f), new Vector3(0.05f, 0.03f, 15.8f), mats.ledCyan);
            CreateArchBox(archGroup.transform, "AccentStrip_Right", new Vector3(7.41f, 1.12f, 0f), new Vector3(0.05f, 0.03f, 15.8f), mats.ledCyan);

            // Architectural Wall Pilasters (Vertical brushed metal dividing columns)
            float[] colX = new float[] { -4.0f, 0.0f, 4.0f };
            foreach (float cx in colX)
            {
                CreateArchBox(archGroup.transform, $"Pilaster_Back_{cx}", new Vector3(cx, 3f, 7.40f), new Vector3(0.35f, 6.0f, 0.08f), mats.brushedMetal);
                CreateArchBox(archGroup.transform, $"Pilaster_Front_{cx}", new Vector3(cx, 3f, -7.40f), new Vector3(0.35f, 6.0f, 0.08f), mats.brushedMetal);
            }
            float[] colZ = new float[] { -3.5f, 0.0f, 3.5f };
            foreach (float cz in colZ)
            {
                CreateArchBox(archGroup.transform, $"Pilaster_Left_{cz}", new Vector3(-7.40f, 3f, cz), new Vector3(0.08f, 6.0f, 0.35f), mats.brushedMetal);
                CreateArchBox(archGroup.transform, $"Pilaster_Right_{cz}", new Vector3(7.40f, 3f, cz), new Vector3(0.08f, 6.0f, 0.35f), mats.brushedMetal);
            }

            // --- 5. Station Glass Partitions with Brushed Metal Posts ---
            GameObject glassGroup = new GameObject("GlassPartitions");
            glassGroup.transform.SetParent(envRoot.transform, false);

            // Framing Quiz Station (-5.0m, Z: -0.5m to 3.2m)
            CreateGlassPartition(glassGroup.transform, "Glass_Quiz_Rear", new Vector3(-4.8f, 1.5f, 3.2f), new Vector3(2.4f, 2.8f, 0.06f), mats);
            CreateGlassPartition(glassGroup.transform, "Glass_Quiz_Front", new Vector3(-4.8f, 1.5f, -0.4f), new Vector3(2.4f, 2.8f, 0.06f), mats);

            // Framing Dr. Nova AI Pod (4.8m, Z: -0.5m to 3.2m)
            CreateGlassPartition(glassGroup.transform, "Glass_Nova_Rear", new Vector3(4.8f, 1.5f, 3.2f), new Vector3(2.4f, 2.8f, 0.06f), mats);
            CreateGlassPartition(glassGroup.transform, "Glass_Nova_Front", new Vector3(4.8f, 1.5f, -0.4f), new Vector3(2.4f, 2.8f, 0.06f), mats);

            // --- 6. Upgrade Central Examination Station & Table ---
            GameObject table = GameObject.Find("DissectionTable");
            if (table != null)
            {
                // Top surgical surface gets stainless steel chrome
                table.GetComponent<Renderer>().sharedMaterial = mats.steelChrome;

                // Add architectural pedestal plinth below table
                GameObject tablePlinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tablePlinth.name = "Table_Plinth_Base";
                tablePlinth.transform.SetParent(envRoot.transform, false);
                tablePlinth.transform.position = new Vector3(0f, 0.12f, 1.5f);
                tablePlinth.transform.localScale = new Vector3(2.1f, 0.24f, 1.4f);
                tablePlinth.GetComponent<Renderer>().sharedMaterial = mats.wallGraphite;
                if (tablePlinth.GetComponent<Collider>() != null) UnityEngine.Object.DestroyImmediate(tablePlinth.GetComponent<Collider>());

                // Perimeter cyan LED inset under table
                GameObject tableGlow = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tableGlow.name = "Table_Plinth_Glow";
                tableGlow.transform.SetParent(envRoot.transform, false);
                tableGlow.transform.position = new Vector3(0f, 0.245f, 1.5f);
                tableGlow.transform.localScale = new Vector3(2.14f, 0.02f, 1.44f);
                tableGlow.GetComponent<Renderer>().sharedMaterial = mats.ledCyan;
                if (tableGlow.GetComponent<Collider>() != null) UnityEngine.Object.DestroyImmediate(tableGlow.GetComponent<Collider>());
            }

            // Holographic Projector Concentric Floor Rings (as seen in concept image)
            CreateHoloFloorProjector(envRoot.transform, new Vector3(0f, 0.008f, 1.5f), mats);

            // Overhead Surgical Examination Ring (Floating halo illuminator at Y=4.4m)
            CreateOverheadSurgicalHalo(envRoot.transform, new Vector3(0f, 4.4f, 1.5f), mats);

            // --- 7. Perimeter Research Furniture & Diagnostic Equipment ---
            GameObject furnGroup = new GameObject("LaboratoryFurniture");
            furnGroup.transform.SetParent(envRoot.transform, false);

            // Left Research Workbench (near Quiz Kiosk, X = -6.8m, Z = -3.0m)
            CreateLabWorkbench(furnGroup.transform, "Workbench_Diagnostics_L", new Vector3(-6.5f, 0f, -2.8f), Quaternion.Euler(0f, 90f, 0f), mats);

            // Right Research Workbench (near Dr. Nova, X = 6.8m, Z = -2.8f)
            CreateLabWorkbench(furnGroup.transform, "Workbench_Diagnostics_R", new Vector3(6.5f, 0f, -2.8f), Quaternion.Euler(0f, -90f, 0f), mats);

            // Equipment Rack Cabinets in Back Corners
            CreateTelemetryRack(furnGroup.transform, "Telemetry_Rack_BackLeft", new Vector3(-6.6f, 0f, 6.6f), Quaternion.Euler(0f, 45f, 0f), mats);
            CreateTelemetryRack(furnGroup.transform, "Telemetry_Rack_BackRight", new Vector3(6.6f, 0f, 6.6f), Quaternion.Euler(0f, -45f, 0f), mats);

            // --- 8. Ceiling Architecture & Linear Strip Lights ---
            GameObject ceilingGroup = new GameObject("CeilingArchitecture");
            ceilingGroup.transform.SetParent(envRoot.transform, false);

            // Ceiling Cap
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling_Modular_Plates";
            ceiling.transform.SetParent(ceilingGroup.transform, false);
            ceiling.transform.position = new Vector3(0f, 6.05f, 0f);
            ceiling.transform.localScale = new Vector3(16f, 0.1f, 16f);
            ceiling.GetComponent<Renderer>().sharedMaterial = mats.wallClinical;
            if (ceiling.GetComponent<Collider>() != null) UnityEngine.Object.DestroyImmediate(ceiling.GetComponent<Collider>());

            // Longitudinal Ceiling Girders (Y = 5.9m)
            float[] girderX = new float[] { -4.5f, -1.8f, 1.8f, 4.5f };
            foreach (float gx in girderX)
            {
                CreateArchBox(ceilingGroup.transform, $"Girder_{gx}", new Vector3(gx, 5.85f, 0f), new Vector3(0.35f, 0.35f, 15.8f), mats.brushedMetal);

                // Recessed linear LED daylight strips along underside of girders
                CreateArchBox(ceilingGroup.transform, $"Ceiling_LED_Strip_{gx}", new Vector3(gx, 5.66f, 0f), new Vector3(0.12f, 0.02f, 14.5f), mats.ledWhite);
            }

            // --- 9. Reflection Probes for PBR Specular Realism ---
            GameObject probeGO = GameObject.Find("Lab_MainReflectionProbe");
            if (probeGO != null) UnityEngine.Object.DestroyImmediate(probeGO);
            probeGO = new GameObject("Lab_MainReflectionProbe");
            probeGO.transform.SetParent(envRoot.transform, false);
            probeGO.transform.position = new Vector3(0f, 1.8f, 0f);

            ReflectionProbe probe = probeGO.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.size = new Vector3(17f, 7f, 17f);
            probe.boxProjection = true;
            probe.intensity = 1.0f;
            probe.clearFlags = ReflectionProbeClearFlags.SolidColor;
            probe.backgroundColor = new Color(0.04f, 0.06f, 0.10f);

            // --- 10. Secondary Fill Reflection Probe for Central Dissection Area ---
            GameObject centralProbeGO = new GameObject("Lab_CentralDissection_ReflectionProbe");
            centralProbeGO.transform.SetParent(envRoot.transform, false);
            centralProbeGO.transform.position = new Vector3(0f, 1.3f, 1.5f);

            ReflectionProbe cProbe = centralProbeGO.AddComponent<ReflectionProbe>();
            cProbe.mode = ReflectionProbeMode.Realtime;
            cProbe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            cProbe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            cProbe.size = new Vector3(5f, 4f, 4.5f);
            cProbe.boxProjection = true;
            cProbe.intensity = 1.15f;

            Debug.Log("[Phase 7.1] Upgraded room architecture, PBR materials, surgical halo, and reflection probes.");
        }

        private static GameObject CreateArchBox(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;

            Collider c = go.GetComponent<Collider>();
            if (c != null) UnityEngine.Object.DestroyImmediate(c);
            return go;
        }

        private static void CreateGlassPartition(Transform parent, string name, Vector3 pos, Vector3 size, Phase7Materials mats)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = pos;

            // Glass pane
            GameObject pane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pane.name = "GlassPane";
            pane.transform.SetParent(root.transform, false);
            pane.transform.localPosition = Vector3.zero;
            pane.transform.localScale = size;
            pane.GetComponent<Renderer>().sharedMaterial = mats.glassPartition;
            if (pane.GetComponent<Collider>() != null) UnityEngine.Object.DestroyImmediate(pane.GetComponent<Collider>());

            // Top & bottom steel support rails
            GameObject topRail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topRail.name = "TopRail";
            topRail.transform.SetParent(root.transform, false);
            topRail.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
            topRail.transform.localScale = new Vector3(size.x + 0.05f, 0.06f, size.z + 0.04f);
            topRail.GetComponent<Renderer>().sharedMaterial = mats.brushedMetal;
            if (topRail.GetComponent<Collider>() != null) UnityEngine.Object.DestroyImmediate(topRail.GetComponent<Collider>());

            GameObject bottomRail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bottomRail.name = "BottomRail";
            bottomRail.transform.SetParent(root.transform, false);
            bottomRail.transform.localPosition = new Vector3(0f, -size.y * 0.5f, 0f);
            bottomRail.transform.localScale = new Vector3(size.x + 0.05f, 0.06f, size.z + 0.04f);
            bottomRail.GetComponent<Renderer>().sharedMaterial = mats.brushedMetal;
            if (bottomRail.GetComponent<Collider>() != null) UnityEngine.Object.DestroyImmediate(bottomRail.GetComponent<Collider>());

            // Cyan LED edge line along bottom rail
            GameObject ledStrip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ledStrip.name = "LEDEdge";
            ledStrip.transform.SetParent(root.transform, false);
            ledStrip.transform.localPosition = new Vector3(0f, -size.y * 0.5f + 0.035f, 0f);
            ledStrip.transform.localScale = new Vector3(size.x, 0.015f, size.z + 0.05f);
            ledStrip.GetComponent<Renderer>().sharedMaterial = mats.ledCyan;
            if (ledStrip.GetComponent<Collider>() != null) UnityEngine.Object.DestroyImmediate(ledStrip.GetComponent<Collider>());
        }

        private static void CreateHoloFloorProjector(Transform parent, Vector3 pos, Phase7Materials mats)
        {
            GameObject holo = new GameObject("HoloProjectorRings");
            holo.transform.SetParent(parent, false);
            holo.transform.position = pos;

            // Outer ring
            GameObject outerRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            outerRing.name = "OuterRing";
            outerRing.transform.SetParent(holo.transform, false);
            outerRing.transform.localPosition = Vector3.zero;
            outerRing.transform.localScale = new Vector3(3.2f, 0.002f, 2.5f);
            outerRing.GetComponent<Renderer>().sharedMaterial = mats.ledCyan;
            if (outerRing.GetComponent<Collider>() != null) UnityEngine.Object.DestroyImmediate(outerRing.GetComponent<Collider>());

            // Inner mask cylinder (to make ring hollow)
            GameObject innerDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            innerDisc.name = "InnerDisc";
            innerDisc.transform.SetParent(holo.transform, false);
            innerDisc.transform.localPosition = new Vector3(0f, 0.001f, 0f);
            innerDisc.transform.localScale = new Vector3(2.8f, 0.003f, 2.1f);
            innerDisc.GetComponent<Renderer>().sharedMaterial = mats.floorPolished;
            if (innerDisc.GetComponent<Collider>() != null) UnityEngine.Object.DestroyImmediate(innerDisc.GetComponent<Collider>());

            // Inner focused ring
            GameObject centerRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            centerRing.name = "CenterRing";
            centerRing.transform.SetParent(holo.transform, false);
            centerRing.transform.localPosition = new Vector3(0f, 0.002f, 0f);
            centerRing.transform.localScale = new Vector3(2.4f, 0.002f, 1.8f);
            centerRing.GetComponent<Renderer>().sharedMaterial = mats.ledCyan;
            if (centerRing.GetComponent<Collider>() != null) UnityEngine.Object.DestroyImmediate(centerRing.GetComponent<Collider>());

            // Center base disc
            GameObject centerBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            centerBase.name = "CenterBase";
            centerBase.transform.SetParent(holo.transform, false);
            centerBase.transform.localPosition = new Vector3(0f, 0.003f, 0f);
            centerBase.transform.localScale = new Vector3(2.1f, 0.003f, 1.5f);
            centerBase.GetComponent<Renderer>().sharedMaterial = mats.floorPolished;
            if (centerBase.GetComponent<Collider>() != null) UnityEngine.Object.DestroyImmediate(centerBase.GetComponent<Collider>());
        }

        private static void CreateOverheadSurgicalHalo(Transform parent, Vector3 pos, Phase7Materials mats)
        {
            GameObject halo = new GameObject("OverheadSurgicalHalo");
            halo.transform.SetParent(parent, false);
            halo.transform.position = pos;

            // Outer Surgical Housing Ring
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "HaloHousing";
            ring.transform.SetParent(halo.transform, false);
            ring.transform.localPosition = Vector3.zero;
            ring.transform.localScale = new Vector3(2.6f, 0.08f, 1.8f);
            ring.GetComponent<Renderer>().sharedMaterial = mats.steelChrome;
            if (ring.GetComponent<Collider>() != null) UnityEngine.Object.DestroyImmediate(ring.GetComponent<Collider>());

            // Inner Ring Emissive Light
            GameObject lightDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lightDisc.name = "HaloEmissiveLight";
            lightDisc.transform.SetParent(halo.transform, false);
            lightDisc.transform.localPosition = new Vector3(0f, -0.045f, 0f);
            lightDisc.transform.localScale = new Vector3(2.35f, 0.02f, 1.55f);
            lightDisc.GetComponent<Renderer>().sharedMaterial = mats.ledWhite;
            if (lightDisc.GetComponent<Collider>() != null) UnityEngine.Object.DestroyImmediate(lightDisc.GetComponent<Collider>());

            // Outer Cyan Halo Accent
            GameObject cyanHalo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cyanHalo.name = "HaloCyanAccent";
            cyanHalo.transform.SetParent(halo.transform, false);
            cyanHalo.transform.localPosition = new Vector3(0f, 0.035f, 0f);
            cyanHalo.transform.localScale = new Vector3(2.65f, 0.02f, 1.85f);
            cyanHalo.GetComponent<Renderer>().sharedMaterial = mats.ledCyan;
            if (cyanHalo.GetComponent<Collider>() != null) UnityEngine.Object.DestroyImmediate(cyanHalo.GetComponent<Collider>());

            // Mounting Stanchions to Ceiling (Y = 4.4m to 6.0m)
            GameObject stanchionL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stanchionL.name = "MountStanchion_L";
            stanchionL.transform.SetParent(halo.transform, false);
            stanchionL.transform.localPosition = new Vector3(-0.9f, 0.8f, 0f);
            stanchionL.transform.localScale = new Vector3(0.06f, 0.8f, 0.06f);
            stanchionL.GetComponent<Renderer>().sharedMaterial = mats.brushedMetal;
            if (stanchionL.GetComponent<Collider>() != null) UnityEngine.Object.DestroyImmediate(stanchionL.GetComponent<Collider>());

            GameObject stanchionR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stanchionR.name = "MountStanchion_R";
            stanchionR.transform.SetParent(halo.transform, false);
            stanchionR.transform.localPosition = new Vector3(0.9f, 0.8f, 0f);
            stanchionR.transform.localScale = new Vector3(0.06f, 0.8f, 0.06f);
            stanchionR.GetComponent<Renderer>().sharedMaterial = mats.brushedMetal;
            if (stanchionR.GetComponent<Collider>() != null) UnityEngine.Object.DestroyImmediate(stanchionR.GetComponent<Collider>());

            // Direct Focused Surgical Light Beam
            GameObject lightObj = new GameObject("SurgicalSpotlight");
            lightObj.transform.SetParent(halo.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            lightObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            Light spot = lightObj.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = new Color(0.95f, 0.98f, 1.0f);
            spot.intensity = 3.5f;
            spot.range = 7f;
            spot.spotAngle = 55f;
            spot.shadows = LightShadows.Soft;
        }

        private static void CreateLabWorkbench(Transform parent, string name, Vector3 pos, Quaternion rot, Phase7Materials mats)
        {
            GameObject bench = new GameObject(name);
            bench.transform.SetParent(parent, false);
            bench.transform.position = pos;
            bench.transform.rotation = rot;

            // Ceramic Countertop
            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.name = "Countertop";
            top.transform.SetParent(bench.transform, false);
            top.transform.localPosition = new Vector3(0f, 0.88f, 0f);
            top.transform.localScale = new Vector3(3.2f, 0.06f, 0.85f);
            top.GetComponent<Renderer>().sharedMaterial = mats.ceramicSurface;

            // Brushed Metal Frame & Cabinet Base
            GameObject baseUnit = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseUnit.name = "CabinetBase";
            baseUnit.transform.SetParent(bench.transform, false);
            baseUnit.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            baseUnit.transform.localScale = new Vector3(3.1f, 0.84f, 0.78f);
            baseUnit.GetComponent<Renderer>().sharedMaterial = mats.wallGraphite;

            // Front Drawer Trims
            GameObject drawerTrim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            drawerTrim.name = "DrawerTrim";
            drawerTrim.transform.SetParent(bench.transform, false);
            drawerTrim.transform.localPosition = new Vector3(0f, 0.78f, 0.40f);
            drawerTrim.transform.localScale = new Vector3(3.05f, 0.03f, 0.02f);
            drawerTrim.GetComponent<Renderer>().sharedMaterial = mats.brushedMetal;
            if (drawerTrim.GetComponent<Collider>() != null) UnityEngine.Object.DestroyImmediate(drawerTrim.GetComponent<Collider>());

            // Dual Diagnostic Monitors on Top
            CreateDiagnosticMonitor(bench.transform, "DiagMonitor_1", new Vector3(-0.75f, 1.25f, 0.05f), Quaternion.Euler(0f, 15f, 0f), mats);
            CreateDiagnosticMonitor(bench.transform, "DiagMonitor_2", new Vector3(0.75f, 1.25f, 0.05f), Quaternion.Euler(0f, -15f, 0f), mats);
        }

        private static void CreateDiagnosticMonitor(Transform parent, string name, Vector3 localPos, Quaternion localRot, Phase7Materials mats)
        {
            GameObject mon = new GameObject(name);
            mon.transform.SetParent(parent, false);
            mon.transform.localPosition = localPos;
            mon.transform.localRotation = localRot;

            // Stand
            GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stand.name = "StandArm";
            stand.transform.SetParent(mon.transform, false);
            stand.transform.localPosition = new Vector3(0f, -0.15f, 0f);
            stand.transform.localScale = new Vector3(0.04f, 0.18f, 0.04f);
            stand.GetComponent<Renderer>().sharedMaterial = mats.brushedMetal;
            if (stand.GetComponent<Collider>() != null) UnityEngine.Object.DestroyImmediate(stand.GetComponent<Collider>());

            // Screen Frame
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "ScreenFrame";
            frame.transform.SetParent(mon.transform, false);
            frame.transform.localPosition = Vector3.zero;
            frame.transform.localScale = new Vector3(0.85f, 0.50f, 0.04f);
            frame.GetComponent<Renderer>().sharedMaterial = mats.consoleSleek;
            if (frame.GetComponent<Collider>() != null) UnityEngine.Object.DestroyImmediate(frame.GetComponent<Collider>());

            // Emissive Telemetry Display Face
            GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screen.name = "DisplayFace";
            screen.transform.SetParent(mon.transform, false);
            screen.transform.localPosition = new Vector3(0f, 0f, 0.021f);
            screen.transform.localScale = new Vector3(0.80f, 0.45f, 0.005f);
            screen.GetComponent<Renderer>().sharedMaterial = mats.ledCyan;
            if (screen.GetComponent<Collider>() != null) UnityEngine.Object.DestroyImmediate(screen.GetComponent<Collider>());
        }

        private static void CreateTelemetryRack(Transform parent, string name, Vector3 pos, Quaternion rot, Phase7Materials mats)
        {
            GameObject rack = new GameObject(name);
            rack.transform.SetParent(parent, false);
            rack.transform.position = pos;
            rack.transform.rotation = rot;

            // Main Tower Enclosure
            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tower.name = "TowerChassis";
            tower.transform.SetParent(rack.transform, false);
            tower.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            tower.transform.localScale = new Vector3(0.9f, 2.5f, 0.8f);
            tower.GetComponent<Renderer>().sharedMaterial = mats.wallGraphite;

            // Front Glass Door
            GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "FrontGlassDoor";
            door.transform.SetParent(rack.transform, false);
            door.transform.localPosition = new Vector3(0f, 1.25f, 0.41f);
            door.transform.localScale = new Vector3(0.82f, 2.4f, 0.02f);
            door.GetComponent<Renderer>().sharedMaterial = mats.glassPartition;
            if (door.GetComponent<Collider>() != null) UnityEngine.Object.DestroyImmediate(door.GetComponent<Collider>());

            // Blinking status LED rows
            for (int i = 0; i < 5; i++)
            {
                float y = 0.5f + (i * 0.38f);
                GameObject ledRow = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ledRow.name = $"LED_Rack_Row_{i}";
                ledRow.transform.SetParent(rack.transform, false);
                ledRow.transform.localPosition = new Vector3(0f, y, 0.38f);
                ledRow.transform.localScale = new Vector3(0.75f, 0.025f, 0.02f);
                ledRow.GetComponent<Renderer>().sharedMaterial = (i % 2 == 0) ? mats.ledCyan : mats.ledWhite;
                if (ledRow.GetComponent<Collider>() != null) UnityEngine.Object.DestroyImmediate(ledRow.GetComponent<Collider>());
            }
        }
    }
}
#endif
