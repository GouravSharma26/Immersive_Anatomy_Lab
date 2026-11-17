using UnityEngine;

public class OrganHighlighter : MonoBehaviour
{
    [Header("Highlight Materials")]
    public Material hoverMaterial;
    public Material selectMaterial;
    public Material ghostMaterial;

    public OrganInteractable CurrentHovered => currentHovered;
    public OrganInteractable CurrentSelected => currentSelected;

    private OrganInteractable currentHovered;
    private OrganInteractable currentSelected;

    private void Awake()
    {
        EnsureDefaultMaterials();
    }

    private void EnsureDefaultMaterials()
    {
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        if (hoverMaterial == null)
        {
            hoverMaterial = new Material(urpShader);
            hoverMaterial.name = "Hover_CyanGlow";
            hoverMaterial.color = new Color(0.2f, 0.85f, 1f, 0.95f);
            if (hoverMaterial.HasProperty("_EmissionColor"))
            {
                hoverMaterial.EnableKeyword("_EMISSION");
                hoverMaterial.SetColor("_EmissionColor", new Color(0f, 0.6f, 0.95f) * 1.8f);
            }
        }

        if (selectMaterial == null)
        {
            selectMaterial = new Material(urpShader);
            selectMaterial.name = "Select_VividGold";
            selectMaterial.color = new Color(1f, 0.85f, 0.2f, 0.98f);
            if (selectMaterial.HasProperty("_EmissionColor"))
            {
                selectMaterial.EnableKeyword("_EMISSION");
                selectMaterial.SetColor("_EmissionColor", new Color(1f, 0.75f, 0.1f) * 2.2f);
            }
        }
    }

    public void OnHoverEnter(OrganInteractable organ)
    {
        if (organ == null || organ == currentSelected) return;
        EnsureDefaultMaterials();

        if (currentHovered != null && currentHovered != currentSelected)
        {
            currentHovered.SetHighlight(null, false);
        }
        currentHovered = organ;
        currentHovered.SetHighlight(hoverMaterial, true);
    }

    public void OnHoverExit(OrganInteractable organ)
    {
        if (organ == null || organ == currentSelected) return;
        if (currentHovered == organ)
        {
            currentHovered.SetHighlight(null, false);
            currentHovered = null;
        }
    }

    public void SelectOrgan(OrganInteractable organ)
    {
        EnsureDefaultMaterials();

        // Unhighlight previous selection
        if (currentSelected != null)
        {
            currentSelected.SetHighlight(null, false);
        }

        currentSelected = organ;

        if (currentSelected != null)
        {
            if (currentHovered == currentSelected) currentHovered = null;
            currentSelected.SetHighlight(selectMaterial != null ? selectMaterial : hoverMaterial, true);
        }
    }

    public void ClearSelection()
    {
        if (currentSelected != null)
        {
            currentSelected.SetHighlight(null, false);
            currentSelected = null;
        }
        if (currentHovered != null)
        {
            currentHovered.SetHighlight(null, false);
            currentHovered = null;
        }
    }
}
