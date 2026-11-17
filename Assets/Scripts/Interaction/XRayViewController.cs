using System.Collections.Generic;
using UnityEngine;

public class XRayViewController : MonoBehaviour
{
    public enum MedicalVisualizationMode
    {
        Normal = 0,
        XRay = 1,
        Vascular = 2,
        ThoracicTranslucent = 3,
        FluoroscopySweep = 4
    }

    [Header("X-Ray Material Configuration (Legacy Compatibility)")]
    public Material xrayMaterial;
    public GameObject bodyMannequin;
    public GameObject skeletalSystem;

    [Header("Phase 7.3 Advanced Visualization Materials")]
    public Material radiographicBoneMaterial;
    public Material radiographicTissueMaterial;
    public Material vascularArterialMaterial;
    public Material vascularVenousMaterial;
    public Material thoracicGlassMaterial;

    [Header("Organ System References (Auto-discovered if unassigned)")]
    public GameObject cardiovascularSystem;
    public GameObject respiratorySystem;
    public GameObject nervousSystem;
    public GameObject digestiveSystem;

    public bool IsXRayActive { get; private set; }
    public MedicalVisualizationMode CurrentMode { get; private set; } = MedicalVisualizationMode.Normal;

    public event System.Action<MedicalVisualizationMode> OnModeChanged;

    private class RendererCache
    {
        public Renderer renderer;
        public Material[] originalMaterials;
        public string systemTag; // "Skeleton", "Body", "Cardiovascular", "Respiratory", "Digestive", "Nervous", "Other"
    }

    private List<RendererCache> cachedRenderers = new List<RendererCache>();
    private bool isInitialized = false;

    // Fluoroscopy Sweep dynamic animation
    private float sweepTime = 0f;
    private MaterialPropertyBlock sweepPropBlock;
    private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        sweepPropBlock = new MaterialPropertyBlock();
    }

    private void Start()
    {
        InitializeCache();
    }

    public void InitializeCache()
    {
        if (isInitialized) return;

        // Auto-discover body and skeleton if unassigned
        if (bodyMannequin == null)
        {
            GameObject found = GameObject.Find("HumanBody");
            if (found == null) found = GameObject.Find("Mannequin");
            if (found != null) bodyMannequin = found;
        }

        if (skeletalSystem == null)
        {
            GameObject found = GameObject.Find("Skeleton");
            if (found == null) found = GameObject.Find("SkeletalSystem");
            if (found != null) skeletalSystem = found;
        }

        // Auto-discover organ systems if unassigned
        BodySystemManager bsm = FindFirstObjectByType<BodySystemManager>();
        if (bsm != null && bsm.systems != null)
        {
            foreach (var sys in bsm.systems)
            {
                if (sys.rootObject == null) continue;
                string sName = sys.systemName.ToLower();
                if (sName.Contains("cardio") || sName.Contains("heart")) cardiovascularSystem = sys.rootObject;
                else if (sName.Contains("resp") || sName.Contains("lung")) respiratorySystem = sys.rootObject;
                else if (sName.Contains("nerv") || sName.Contains("brain")) nervousSystem = sys.rootObject;
                else if (sName.Contains("digest") || sName.Contains("stomach") || sName.Contains("liver")) digestiveSystem = sys.rootObject;
            }
        }

        cachedRenderers.Clear();

        // 1. Body Mannequin
        if (bodyMannequin != null)
        {
            RegisterRenderers(bodyMannequin, "Body");
        }

        // 2. Skeletal System
        if (skeletalSystem != null)
        {
            RegisterRenderers(skeletalSystem, "Skeleton");
        }

        // 3. Organ systems
        if (cardiovascularSystem != null) RegisterRenderers(cardiovascularSystem, "Cardiovascular");
        if (respiratorySystem != null) RegisterRenderers(respiratorySystem, "Respiratory");
        if (nervousSystem != null) RegisterRenderers(nervousSystem, "Nervous");
        if (digestiveSystem != null) RegisterRenderers(digestiveSystem, "Digestive");

        // 4. Any other OrganInteractables not already registered
        OrganInteractable[] allOrgans = FindObjectsByType<OrganInteractable>(FindObjectsSortMode.None);
        foreach (var organ in allOrgans)
        {
            string sysTag = "Other";
            string sName = (organ.bodySystem + " " + organ.organName).ToLower();
            if (sName.Contains("cardio") || sName.Contains("heart")) sysTag = "Cardiovascular";
            else if (sName.Contains("resp") || sName.Contains("lung")) sysTag = "Respiratory";
            else if (sName.Contains("nerv") || sName.Contains("brain")) sysTag = "Nervous";
            else if (sName.Contains("digest") || sName.Contains("stomach") || sName.Contains("liver")) sysTag = "Digestive";
            else if (sName.Contains("skelet") || sName.Contains("bone")) sysTag = "Skeleton";

            RegisterRenderers(organ.gameObject, sysTag);
        }

        isInitialized = true;
    }

    private void RegisterRenderers(GameObject root, string systemTag)
    {
        if (root == null) return;
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r == null) continue;
            // Avoid duplicate registrations
            if (cachedRenderers.Exists(c => c.renderer == r)) continue;

            cachedRenderers.Add(new RendererCache
            {
                renderer = r,
                originalMaterials = r.sharedMaterials,
                systemTag = systemTag
            });
        }
    }

    private void Update()
    {
        // Fluoroscopy sweep dynamic modulation
        if (CurrentMode == MedicalVisualizationMode.FluoroscopySweep)
        {
            AnimateFluoroscopySweep();
        }
    }

    private void AnimateFluoroscopySweep()
    {
        sweepTime += Time.deltaTime * 1.5f;
        float pulse = Mathf.PingPong(sweepTime, 1.0f);
        Color glow = Color.Lerp(new Color(0.1f, 0.25f, 0.35f, 0.4f), new Color(0.6f, 0.9f, 1.0f, 0.85f), Mathf.SmoothStep(0f, 1f, pulse));

        foreach (var entry in cachedRenderers)
        {
            if (entry.renderer == null) continue;
            if (entry.systemTag == "Skeleton")
            {
                entry.renderer.GetPropertyBlock(sweepPropBlock);
                sweepPropBlock.SetColor(EmissionColorProp, glow * 0.7f);
                entry.renderer.SetPropertyBlock(sweepPropBlock);
            }
        }
    }

    // Legacy backwards-compatible entry points
    public void ToggleXRay()
    {
        if (CurrentMode == MedicalVisualizationMode.XRay)
        {
            SetMode(MedicalVisualizationMode.Normal);
        }
        else
        {
            SetMode(MedicalVisualizationMode.XRay);
        }
    }

    public void SetXRayActive(bool active)
    {
        SetMode(active ? MedicalVisualizationMode.XRay : MedicalVisualizationMode.Normal);
    }

    public void ToggleVascularMode()
    {
        SetMode(CurrentMode == MedicalVisualizationMode.Vascular ? MedicalVisualizationMode.Normal : MedicalVisualizationMode.Vascular);
    }

    public void ToggleThoracicMode()
    {
        SetMode(CurrentMode == MedicalVisualizationMode.ThoracicTranslucent ? MedicalVisualizationMode.Normal : MedicalVisualizationMode.ThoracicTranslucent);
    }

    public void ToggleFluoroscopySweep()
    {
        SetMode(CurrentMode == MedicalVisualizationMode.FluoroscopySweep ? MedicalVisualizationMode.Normal : MedicalVisualizationMode.FluoroscopySweep);
    }

    public void ResetToNormal()
    {
        SetMode(MedicalVisualizationMode.Normal);
    }

    public void SetMode(MedicalVisualizationMode mode)
    {
        if (!isInitialized) InitializeCache();

        CurrentMode = mode;
        IsXRayActive = (mode == MedicalVisualizationMode.XRay || mode == MedicalVisualizationMode.FluoroscopySweep);
        OnModeChanged?.Invoke(mode);

        switch (mode)
        {
            case MedicalVisualizationMode.Normal:
                ApplyNormalMode();
                break;
            case MedicalVisualizationMode.XRay:
                ApplyXRayMode();
                break;
            case MedicalVisualizationMode.Vascular:
                ApplyVascularMode();
                break;
            case MedicalVisualizationMode.ThoracicTranslucent:
                ApplyThoracicTranslucentMode();
                break;
            case MedicalVisualizationMode.FluoroscopySweep:
                ApplyFluoroscopySweepMode();
                break;
        }

        Debug.Log($"[MedicalVisualizationController] Switched to mode: {mode}");
    }

    private void ApplyNormalMode()
    {
        foreach (var entry in cachedRenderers)
        {
            if (entry.renderer != null && entry.originalMaterials != null)
            {
                entry.renderer.sharedMaterials = entry.originalMaterials;
                entry.renderer.SetPropertyBlock(null);
            }
        }
    }

    private void ApplyXRayMode()
    {
        Material boneMat = radiographicBoneMaterial != null ? radiographicBoneMaterial : xrayMaterial;
        Material tissueMat = radiographicTissueMaterial != null ? radiographicTissueMaterial : xrayMaterial;

        foreach (var entry in cachedRenderers)
        {
            if (entry.renderer == null) continue;

            if (entry.systemTag == "Skeleton")
            {
                AssignUniformMaterial(entry.renderer, boneMat);
            }
            else
            {
                AssignUniformMaterial(entry.renderer, tissueMat);
            }
        }
    }

    private void ApplyVascularMode()
    {
        Material arterialMat = vascularArterialMaterial != null ? vascularArterialMaterial : xrayMaterial;
        Material venousMat = vascularVenousMaterial != null ? vascularVenousMaterial : xrayMaterial;
        Material contextMat = thoracicGlassMaterial != null ? thoracicGlassMaterial : (radiographicTissueMaterial != null ? radiographicTissueMaterial : xrayMaterial);

        foreach (var entry in cachedRenderers)
        {
            if (entry.renderer == null) continue;

            if (entry.systemTag == "Cardiovascular")
            {
                AssignUniformMaterial(entry.renderer, arterialMat);
            }
            else if (entry.systemTag == "Respiratory")
            {
                AssignUniformMaterial(entry.renderer, venousMat);
            }
            else
            {
                // Skeleton, body, and viscera rendered in subtle transparent silhouette
                AssignUniformMaterial(entry.renderer, contextMat);
            }
        }
    }

    private void ApplyThoracicTranslucentMode()
    {
        Material glassMat = thoracicGlassMaterial != null ? thoracicGlassMaterial : xrayMaterial;

        foreach (var entry in cachedRenderers)
        {
            if (entry.renderer == null) continue;

            if (entry.systemTag == "Skeleton" || entry.systemTag == "Body")
            {
                // Rib cage and body silhouette become clear transparent glass
                AssignUniformMaterial(entry.renderer, glassMat);
            }
            else
            {
                // Thoracic organs (Heart, Lungs, etc.) retain their full rich anatomical PBR materials
                if (entry.originalMaterials != null)
                {
                    entry.renderer.sharedMaterials = entry.originalMaterials;
                }
            }
        }
    }

    private void ApplyFluoroscopySweepMode()
    {
        ApplyXRayMode();
        sweepTime = 0f;
    }

    private void AssignUniformMaterial(Renderer r, Material mat)
    {
        if (r == null || mat == null) return;
        int matCount = r.sharedMaterials != null ? r.sharedMaterials.Length : 1;
        if (matCount <= 1)
        {
            r.material = mat;
        }
        else
        {
            Material[] mats = new Material[matCount];
            for (int i = 0; i < matCount; i++) mats[i] = mat;
            r.materials = mats;
        }
    }
}
