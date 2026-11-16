using UnityEngine;

public class XRayViewController : MonoBehaviour
{
    [Header("X-Ray Material Configuration")]
    public Material xrayMaterial;
    public GameObject bodyMannequin;
    public GameObject skeletalSystem;

    public bool IsXRayActive { get; private set; }

    private Material[] originalBodyMaterials;
    private Material[] originalSkeletonMaterials;
    private Renderer[] bodyRenderers;
    private Renderer[] skeletonRenderers;

    private void Start()
    {
        if (bodyMannequin != null)
        {
            bodyRenderers = bodyMannequin.GetComponentsInChildren<Renderer>();
            if (bodyRenderers != null && bodyRenderers.Length > 0)
            {
                originalBodyMaterials = new Material[bodyRenderers.Length];
                for (int i = 0; i < bodyRenderers.Length; i++) originalBodyMaterials[i] = bodyRenderers[i].sharedMaterial;
            }
        }

        if (skeletalSystem != null)
        {
            skeletonRenderers = skeletalSystem.GetComponentsInChildren<Renderer>();
            if (skeletonRenderers != null && skeletonRenderers.Length > 0)
            {
                originalSkeletonMaterials = new Material[skeletonRenderers.Length];
                for (int i = 0; i < skeletonRenderers.Length; i++) originalSkeletonMaterials[i] = skeletonRenderers[i].sharedMaterial;
            }
        }
    }

    public void ToggleXRay()
    {
        SetXRayActive(!IsXRayActive);
    }

    public void SetXRayActive(bool active)
    {
        IsXRayActive = active;

        if (bodyRenderers != null && xrayMaterial != null)
        {
            for (int i = 0; i < bodyRenderers.Length; i++)
            {
                if (bodyRenderers[i] != null)
                {
                    bodyRenderers[i].material = active ? xrayMaterial : originalBodyMaterials[i];
                }
            }
        }

        if (skeletonRenderers != null && xrayMaterial != null)
        {
            for (int i = 0; i < skeletonRenderers.Length; i++)
            {
                if (skeletonRenderers[i] != null)
                {
                    skeletonRenderers[i].material = active ? xrayMaterial : originalSkeletonMaterials[i];
                }
            }
        }
    }
}
