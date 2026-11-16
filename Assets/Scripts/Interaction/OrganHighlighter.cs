using UnityEngine;

public class OrganHighlighter : MonoBehaviour
{
    [Header("Highlight Materials")]
    public Material hoverMaterial;
    public Material selectMaterial;
    public Material ghostMaterial;

    private OrganInteractable currentHovered;
    private OrganInteractable currentSelected;

    public void OnHoverEnter(OrganInteractable organ)
    {
        if (organ == null || organ == currentSelected) return;
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
