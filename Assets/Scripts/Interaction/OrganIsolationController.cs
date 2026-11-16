using System.Collections;
using UnityEngine;

public class OrganIsolationController : MonoBehaviour
{
    [Header("Isolation Configuration")]
    public Transform inspectionPoint;
    public float transitionSpeed = 3.5f;
    public Material ghostMaterial;

    [Header("References")]
    public Camera playerCamera;
    public OrganHighlighter highlighter;

    public bool IsInIsolationMode { get; private set; }
    public OrganInteractable IsolatedOrgan { get; private set; }

    private Vector3 organOriginalWorldPos;
    private Quaternion organOriginalWorldRot;
    private Vector3 organOriginalWorldScale;
    private Transform organOriginalParent;

    private Coroutine transitionCoroutine;

    private void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (inspectionPoint == null)
        {
            // Create an inspection point 1.6m in front of player eye height
            GameObject pt = new GameObject("InspectionFocalPoint");
            pt.transform.position = new Vector3(0f, 1.45f, 0.4f);
            inspectionPoint = pt.transform;
        }
    }

    public void EnterIsolation(OrganInteractable organ)
    {
        if (organ == null || IsInIsolationMode) return;

        IsInIsolationMode = true;
        IsolatedOrgan = organ;

        organOriginalParent = organ.transform.parent;
        organOriginalWorldPos = organ.transform.position;
        organOriginalWorldRot = organ.transform.rotation;
        organOriginalWorldScale = organ.transform.localScale;

        // Apply ghost/dimmed mode to all other organs
        OrganInteractable[] allOrgans = FindObjectsByType<OrganInteractable>(FindObjectsSortMode.None);
        foreach (var o in allOrgans)
        {
            if (o != organ && ghostMaterial != null)
            {
                o.SetGhostMode(ghostMaterial, true);
            }
        }

        // Hide outer body mannequin if system manager exists
        if (BodySystemManager.Instance != null)
        {
            BodySystemManager.Instance.SetSystemVisibility("Body", false);
        }

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(MoveOrganRoutine(organ.transform, inspectionPoint.position, inspectionPoint.rotation, organOriginalWorldScale * 1.5f));
    }

    public void ExitIsolation()
    {
        if (!IsInIsolationMode || IsolatedOrgan == null) return;

        // Restore other organs
        OrganInteractable[] allOrgans = FindObjectsByType<OrganInteractable>(FindObjectsSortMode.None);
        foreach (var o in allOrgans)
        {
            if (o != IsolatedOrgan && ghostMaterial != null)
            {
                o.SetGhostMode(ghostMaterial, false);
            }
        }

        // Restore body mannequin
        if (BodySystemManager.Instance != null)
        {
            BodySystemManager.Instance.SetSystemVisibility("Body", true);
        }

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(ReturnOrganRoutine(IsolatedOrgan.transform, organOriginalWorldPos, organOriginalWorldRot, organOriginalWorldScale));

        IsInIsolationMode = false;
        IsolatedOrgan = null;
    }

    private IEnumerator MoveOrganRoutine(Transform target, Vector3 destPos, Quaternion destRot, Vector3 destScale)
    {
        float elapsed = 0f;
        float duration = 0.8f;
        Vector3 startPos = target.position;
        Quaternion startRot = target.rotation;
        Vector3 startScale = target.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            target.position = Vector3.Lerp(startPos, destPos, t);
            target.rotation = Quaternion.Slerp(startRot, destRot, t);
            target.localScale = Vector3.Lerp(startScale, destScale, t);
            yield return null;
        }

        target.position = destPos;
        target.rotation = destRot;
        target.localScale = destScale;
    }

    private IEnumerator ReturnOrganRoutine(Transform target, Vector3 origPos, Quaternion origRot, Vector3 origScale)
    {
        float elapsed = 0f;
        float duration = 0.8f;
        Vector3 startPos = target.position;
        Quaternion startRot = target.rotation;
        Vector3 startScale = target.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            target.position = Vector3.Lerp(startPos, origPos, t);
            target.rotation = Quaternion.Slerp(startRot, origRot, t);
            target.localScale = Vector3.Lerp(startScale, origScale, t);
            yield return null;
        }

        target.position = origPos;
        target.rotation = origRot;
        target.localScale = origScale;
    }

    public void RotateIsolatedOrgan(float yawDelta, float pitchDelta)
    {
        if (IsolatedOrgan != null)
        {
            IsolatedOrgan.transform.Rotate(Vector3.up, yawDelta, Space.World);
            IsolatedOrgan.transform.Rotate(Vector3.right, pitchDelta, Space.World);
        }
    }

    public void ZoomIsolatedOrgan(float scroll)
    {
        if (IsolatedOrgan != null && Mathf.Abs(scroll) > 0.001f)
        {
            Vector3 s = IsolatedOrgan.transform.localScale + Vector3.one * scroll * 0.4f;
            float clamped = Mathf.Clamp(s.x, 0.2f, 3.5f);
            IsolatedOrgan.transform.localScale = Vector3.one * clamped;
        }
    }
}
