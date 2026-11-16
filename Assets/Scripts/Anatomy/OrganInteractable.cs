using UnityEngine;

public class OrganInteractable : MonoBehaviour
{
    [Header("Organ Identity")]
    public string organName = "Unknown Organ";
    public string bodySystem = "Unknown";
    public float normalWeight = 0f; // in grams

    [Header("Clinical Information")]
    [TextArea(2, 4)]
    public string description;
    [TextArea(2, 4)]
    public string arterialSupply = "Vascular supply details";
    [TextArea(2, 4)]
    public string innervation = "Nerve innervation details";
    [TextArea(2, 4)]
    public string clinicalFunction = "Physiological function";
    [TextArea(2, 4)]
    public string commonPathology = "Common medical conditions";

    [Header("Isolation Transform")]
    [HideInInspector] public Vector3 originalLocalPosition;
    [HideInInspector] public Quaternion originalLocalRotation;
    [HideInInspector] public Vector3 originalLocalScale;

    private Renderer[] organRenderers;
    private Material[][] originalMaterials;
    private bool isHighlighted = false;

    private void Awake()
    {
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
        originalLocalScale = transform.localScale;

        organRenderers = GetComponentsInChildren<Renderer>();
        if (organRenderers != null && organRenderers.Length > 0)
        {
            originalMaterials = new Material[organRenderers.Length][];
            for (int i = 0; i < organRenderers.Length; i++)
            {
                originalMaterials[i] = organRenderers[i].sharedMaterials;
            }
        }
    }

    public void SetHighlight(Material highlightMaterial, bool enable)
    {
        if (organRenderers == null || highlightMaterial == null) return;
        isHighlighted = enable;

        for (int i = 0; i < organRenderers.Length; i++)
        {
            if (organRenderers[i] == null) continue;
            if (enable)
            {
                Material[] mats = new Material[originalMaterials[i].Length];
                for (int m = 0; m < mats.Length; m++)
                {
                    mats[m] = highlightMaterial;
                }
                organRenderers[i].materials = mats;
            }
            else
            {
                organRenderers[i].materials = originalMaterials[i];
            }
        }
    }

    public void SetGhostMode(Material ghostMaterial, bool enable)
    {
        if (organRenderers == null || ghostMaterial == null) return;
        for (int i = 0; i < organRenderers.Length; i++)
        {
            if (organRenderers[i] == null) continue;
            if (enable)
            {
                Material[] mats = new Material[originalMaterials[i].Length];
                for (int m = 0; m < mats.Length; m++) mats[m] = ghostMaterial;
                organRenderers[i].materials = mats;
            }
            else
            {
                organRenderers[i].materials = originalMaterials[i];
            }
        }
    }

    public void ResetTransform()
    {
        transform.localPosition = originalLocalPosition;
        transform.localRotation = originalLocalRotation;
        transform.localScale = originalLocalScale;
    }

    public void ShowInformation()
    {
        Debug.Log($"[OrganInteractable] Selected: {organName} ({bodySystem}) - Weight: {normalWeight}g");
    }
}
