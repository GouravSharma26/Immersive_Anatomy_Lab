using System.Collections;
using UnityEngine;

public class OrganComparisonController : MonoBehaviour
{
    [Header("Dual Comparison Pedestals")]
    public Transform leftPedestal;
    public Transform rightPedestal;

    [Header("Comparison State")]
    public bool IsInComparisonMode { get; private set; }
    public OrganInteractable OrganA { get; private set; }
    public OrganInteractable OrganB { get; private set; }

    private Vector3 organAOriginalPos;
    private Quaternion organAOriginalRot;
    private Vector3 organAOriginalScale;

    private Vector3 organBOriginalPos;
    private Quaternion organBOriginalRot;
    private Vector3 organBOriginalScale;

    private void Start()
    {
        if (leftPedestal == null)
        {
            GameObject lp = new GameObject("LeftComparisonPedestal");
            lp.transform.position = new Vector3(-0.45f, 1.50f, 0.40f);
            leftPedestal = lp.transform;
        }
        if (rightPedestal == null)
        {
            GameObject rp = new GameObject("RightComparisonPedestal");
            rp.transform.position = new Vector3(0.45f, 1.50f, 0.40f);
            rightPedestal = rp.transform;
        }
    }

    public void ToggleComparisonMode(OrganInteractable initialOrgan = null)
    {
        if (IsInComparisonMode)
        {
            ExitComparisonMode();
        }
        else
        {
            StartComparisonMode(initialOrgan);
        }
    }

    public void StartComparisonMode(OrganInteractable initialOrgan = null)
    {
        IsInComparisonMode = true;
        OrganA = null;
        OrganB = null;

        if (initialOrgan != null)
        {
            AddOrganToComparison(initialOrgan);
        }
    }

    public bool AddOrganToComparison(OrganInteractable organ)
    {
        if (!IsInComparisonMode || organ == null) return false;

        if (OrganA == null)
        {
            OrganA = organ;
            organAOriginalPos = organ.transform.position;
            organAOriginalRot = organ.transform.rotation;
            organAOriginalScale = organ.transform.localScale;
            StartCoroutine(MoveRoutine(OrganA.transform, leftPedestal.position, leftPedestal.rotation, organAOriginalScale * 1.3f));
            return true;
        }
        else if (OrganB == null && organ != OrganA)
        {
            OrganB = organ;
            organBOriginalPos = organ.transform.position;
            organBOriginalRot = organ.transform.rotation;
            organBOriginalScale = organ.transform.localScale;
            StartCoroutine(MoveRoutine(OrganB.transform, rightPedestal.position, rightPedestal.rotation, organBOriginalScale * 1.3f));
            return true;
        }
        return false;
    }

    public void ExitComparisonMode()
    {
        if (!IsInComparisonMode) return;

        if (OrganA != null)
        {
            StartCoroutine(MoveRoutine(OrganA.transform, organAOriginalPos, organAOriginalRot, organAOriginalScale));
        }
        if (OrganB != null)
        {
            StartCoroutine(MoveRoutine(OrganB.transform, organBOriginalPos, organBOriginalRot, organBOriginalScale));
        }

        OrganA = null;
        OrganB = null;
        IsInComparisonMode = false;
    }

    public void RotateComparedOrgans(float deltaYaw)
    {
        if (OrganA != null) OrganA.transform.Rotate(Vector3.up, deltaYaw, Space.World);
        if (OrganB != null) OrganB.transform.Rotate(Vector3.up, deltaYaw, Space.World);
    }

    private IEnumerator MoveRoutine(Transform target, Vector3 destPos, Quaternion destRot, Vector3 destScale)
    {
        float elapsed = 0f;
        float duration = 0.75f;
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
}
