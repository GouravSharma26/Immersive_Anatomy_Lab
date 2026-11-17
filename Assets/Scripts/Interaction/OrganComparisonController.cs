using System.Collections;
using UnityEngine;
using ImmersiveAnatomy.VR;

public class OrganComparisonController : MonoBehaviour
{
    public static OrganComparisonController Instance { get; private set; }

    [Header("Dual Comparison Pedestals")]
    public Transform leftPedestal;
    public Transform rightPedestal;
    public Transform inspectionFocalPoint;

    [Header("Snap Configuration")]
    public float snapDistance = 0.75f;

    [Header("Comparison State")]
    public bool IsInComparisonMode { get; private set; }
    public OrganInteractable OrganA { get; private set; }
    public OrganInteractable OrganB { get; private set; }
    public OrganInteractable ExtractedOrgan { get; private set; }

    public bool IsOrganAPlaced => OrganA != null;
    public bool IsOrganBPlaced => OrganB != null;
    public bool IsComparisonComplete => OrganA != null && OrganB != null;

    // Sockets and original transform data
    private Vector3 organAOriginalPos;
    private Quaternion organAOriginalRot;
    private Vector3 organAOriginalScale;

    private Vector3 organBOriginalPos;
    private Quaternion organBOriginalRot;
    private Vector3 organBOriginalScale;

    private Vector3 extractedOriginalPos;
    private Quaternion extractedOriginalRot;
    private Vector3 extractedOriginalScale;

    private AudioSource audioSource;
    private AudioClip snapClip;
    private AudioClip extractClip;
    private AudioClip resetClip;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(this);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        GenerateProceduralClips();
    }

    private void Start()
    {
        EnsurePedestals();
    }

    public void EnsurePedestals()
    {
        if (leftPedestal == null)
        {
            GameObject lp = GameObject.Find("LeftComparisonPedestal") ?? GameObject.Find("LeftPedestal");
            if (lp == null)
            {
                lp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                lp.name = "LeftComparisonPedestal";
                lp.transform.position = new Vector3(-0.85f, 0.45f, 0.75f);
                lp.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
            }
            if (lp.GetComponent<ComparisonPedestalTrigger>() == null)
            {
                var trig = lp.AddComponent<ComparisonPedestalTrigger>();
                trig.isLeftPedestal = true;
            }
            leftPedestal = lp.transform;
        }

        if (rightPedestal == null)
        {
            GameObject rp = GameObject.Find("RightComparisonPedestal") ?? GameObject.Find("RightPedestal");
            if (rp == null)
            {
                rp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rp.name = "RightComparisonPedestal";
                rp.transform.position = new Vector3(0.85f, 0.45f, 0.75f);
                rp.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
            }
            if (rp.GetComponent<ComparisonPedestalTrigger>() == null)
            {
                var trig = rp.AddComponent<ComparisonPedestalTrigger>();
                trig.isLeftPedestal = false;
            }
            rightPedestal = rp.transform;
        }

        if (inspectionFocalPoint == null)
        {
            GameObject ifp = GameObject.Find("InspectionFocalPoint");
            if (ifp == null)
            {
                ifp = new GameObject("InspectionFocalPoint");
                ifp.transform.position = new Vector3(0f, 1.30f, 0.60f);
            }
            inspectionFocalPoint = ifp.transform;
        }

        // Clean up any obsolete floating tablet that might obstruct the view
        GameObject tablet = GameObject.Find("VR_ClinicalTablet");
        if (tablet != null) tablet.SetActive(false);
    }

    private void Update()
    {
        if (!IsInComparisonMode) return;

        CheckProximitySnap();
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
        EnsurePedestals();
        IsInComparisonMode = true;

        if (initialOrgan != null)
        {
            AddOrganToComparison(initialOrgan);
        }
    }

    /// <summary>
    /// Smoothly extracts organ forward to front inspection stage for player to grab or auto-place
    /// </summary>
    public void ExtractOrganToFront(OrganInteractable organ)
    {
        if (organ == null) return;
        EnsurePedestals();
        IsInComparisonMode = true;

        // If this organ is already on a pedestal, don't re-extract
        if (organ == OrganA || organ == OrganB) return;

        // If another organ was extracted but not placed on a pedestal, return it to socket
        if (ExtractedOrgan != null && ExtractedOrgan != organ && ExtractedOrgan != OrganA && ExtractedOrgan != OrganB)
        {
            ReturnOrganToOriginalSocket(ExtractedOrgan, extractedOriginalPos, extractedOriginalRot, extractedOriginalScale);
        }

        ExtractedOrgan = organ;
        extractedOriginalPos = organ.transform.position;
        extractedOriginalRot = organ.transform.rotation;
        extractedOriginalScale = organ.transform.localScale;

        Vector3 targetPos = inspectionFocalPoint != null ? inspectionFocalPoint.position : new Vector3(0f, 1.30f, 0.55f);
        // Face forward toward the player
        Quaternion targetRot = Quaternion.Euler(0f, 180f, 0f);

        StartCoroutine(MoveRoutine(organ.transform, targetPos, targetRot, extractedOriginalScale * 1.15f, 0.55f));

        var rb = organ.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        PlayClip(extractClip, 0.8f);
    }

    /// <summary>
    /// Snaps an organ (either the current extracted organ or specified) onto Pedestal A
    /// </summary>
    public void SnapOrganToPedestalA(OrganInteractable organ = null)
    {
        if (organ == null) organ = ExtractedOrgan;
        if (organ == null) return;

        EnsurePedestals();
        IsInComparisonMode = true;

        // If Pedestal A already had an organ, return previous organ to its socket
        if (OrganA != null && OrganA != organ)
        {
            ReturnOrganToOriginalSocket(OrganA, organAOriginalPos, organAOriginalRot, organAOriginalScale);
        }

        // If organ was on Pedestal B, clear B
        if (OrganB == organ)
        {
            OrganB = null;
        }

        OrganA = organ;
        organAOriginalPos = (organ == ExtractedOrgan) ? extractedOriginalPos : organ.transform.position;
        organAOriginalRot = (organ == ExtractedOrgan) ? extractedOriginalRot : organ.transform.rotation;
        organAOriginalScale = (organ == ExtractedOrgan) ? extractedOriginalScale : organ.transform.localScale;

        if (ExtractedOrgan == organ) ExtractedOrgan = null;

        Vector3 targetPos = leftPedestal.position + Vector3.up * 0.18f;
        Quaternion targetRot = Quaternion.Euler(0f, 180f, 0f);

        StartCoroutine(MoveRoutine(OrganA.transform, targetPos, targetRot, organAOriginalScale * 1.25f, 0.45f));

        var rb = OrganA.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        PlayClip(snapClip, 0.9f);
        SyncWithVRComparisonBay();
    }

    /// <summary>
    /// Snaps an organ (either the current extracted organ or specified) onto Pedestal B
    /// </summary>
    public void SnapOrganToPedestalB(OrganInteractable organ = null)
    {
        if (organ == null) organ = ExtractedOrgan;
        if (organ == null) return;

        EnsurePedestals();
        IsInComparisonMode = true;

        // If Pedestal B already had an organ, return previous organ to its socket
        if (OrganB != null && OrganB != organ)
        {
            ReturnOrganToOriginalSocket(OrganB, organBOriginalPos, organBOriginalRot, organBOriginalScale);
        }

        // If organ was on Pedestal A, clear A
        if (OrganA == organ)
        {
            OrganA = null;
        }

        OrganB = organ;
        organBOriginalPos = (organ == ExtractedOrgan) ? extractedOriginalPos : organ.transform.position;
        organBOriginalRot = (organ == ExtractedOrgan) ? extractedOriginalRot : organ.transform.rotation;
        organBOriginalScale = (organ == ExtractedOrgan) ? extractedOriginalScale : organ.transform.localScale;

        if (ExtractedOrgan == organ) ExtractedOrgan = null;

        Vector3 targetPos = rightPedestal.position + Vector3.up * 0.18f;
        Quaternion targetRot = Quaternion.Euler(0f, 180f, 0f);

        StartCoroutine(MoveRoutine(OrganB.transform, targetPos, targetRot, organBOriginalScale * 1.25f, 0.45f));

        var rb = OrganB.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        PlayClip(snapClip, 0.9f);
        SyncWithVRComparisonBay();
    }

    /// <summary>
    /// Multi-stage smart add:
    /// 1. If nothing in front and Pedestal A is empty -> extract organ to front.
    /// 2. If organ already in front -> snap to Pedestal A (or B if A is full).
    /// 3. If Pedestal A is filled and Pedestal B is empty -> extract second organ to front.
    /// </summary>
    public bool AddOrganToComparison(OrganInteractable organ)
    {
        if (organ == null) return false;
        EnsurePedestals();
        IsInComparisonMode = true;

        if (OrganA == null)
        {
            if (ExtractedOrgan == organ)
            {
                SnapOrganToPedestalA(organ);
            }
            else
            {
                ExtractOrganToFront(organ);
            }
            return true;
        }
        else if (OrganB == null && organ != OrganA)
        {
            if (ExtractedOrgan == organ)
            {
                SnapOrganToPedestalB(organ);
            }
            else
            {
                ExtractOrganToFront(organ);
            }
            return true;
        }
        else if (organ != OrganA && organ != OrganB)
        {
            ExtractOrganToFront(organ);
            return true;
        }

        return false;
    }

    private void CheckProximitySnap()
    {
        if (leftPedestal == null || rightPedestal == null) return;

        // Check proximity for extracted organ
        if (ExtractedOrgan != null)
        {
            float distA = Vector3.Distance(ExtractedOrgan.transform.position, leftPedestal.position);
            float distB = Vector3.Distance(ExtractedOrgan.transform.position, rightPedestal.position);

            if (distA < snapDistance && OrganA != ExtractedOrgan)
            {
                SnapOrganToPedestalA(ExtractedOrgan);
            }
            else if (distB < snapDistance && OrganB != ExtractedOrgan)
            {
                SnapOrganToPedestalB(ExtractedOrgan);
            }
        }
    }

    public void SwapPedestals()
    {
        if (OrganA == null && OrganB == null) return;

        var tempOrg = OrganA;
        var tempPos = organAOriginalPos;
        var tempRot = organAOriginalRot;
        var tempScale = organAOriginalScale;

        OrganA = OrganB;
        organAOriginalPos = organBOriginalPos;
        organAOriginalRot = organBOriginalRot;
        organAOriginalScale = organBOriginalScale;

        OrganB = tempOrg;
        organBOriginalPos = tempPos;
        organBOriginalRot = tempRot;
        organBOriginalScale = tempScale;

        if (OrganA != null)
        {
            Vector3 posA = leftPedestal.position + Vector3.up * 0.18f;
            StartCoroutine(MoveRoutine(OrganA.transform, posA, Quaternion.Euler(0f, 180f, 0f), organAOriginalScale * 1.25f, 0.45f));
        }

        if (OrganB != null)
        {
            Vector3 posB = rightPedestal.position + Vector3.up * 0.18f;
            StartCoroutine(MoveRoutine(OrganB.transform, posB, Quaternion.Euler(0f, 180f, 0f), organBOriginalScale * 1.25f, 0.45f));
        }

        PlayClip(snapClip, 0.8f);
        SyncWithVRComparisonBay();
    }

    public void ReturnAllOrgansToSockets()
    {
        if (ExtractedOrgan != null)
        {
            ReturnOrganToOriginalSocket(ExtractedOrgan, extractedOriginalPos, extractedOriginalRot, extractedOriginalScale);
            ExtractedOrgan = null;
        }

        if (OrganA != null)
        {
            ReturnOrganToOriginalSocket(OrganA, organAOriginalPos, organAOriginalRot, organAOriginalScale);
            OrganA = null;
        }

        if (OrganB != null)
        {
            ReturnOrganToOriginalSocket(OrganB, organBOriginalPos, organBOriginalRot, organBOriginalScale);
            OrganB = null;
        }

        PlayClip(resetClip, 0.8f);
        SyncWithVRComparisonBay();
    }

    public void ExitComparisonMode()
    {
        ReturnAllOrgansToSockets();
        IsInComparisonMode = false;
    }

    public void RotateComparedOrgans(float deltaYaw)
    {
        if (OrganA != null) OrganA.transform.Rotate(Vector3.up, deltaYaw, Space.World);
        if (OrganB != null) OrganB.transform.Rotate(Vector3.up, deltaYaw, Space.World);
        if (ExtractedOrgan != null) ExtractedOrgan.transform.Rotate(Vector3.up, deltaYaw, Space.World);
    }

    private void ReturnOrganToOriginalSocket(OrganInteractable organ, Vector3 targetPos, Quaternion targetRot, Vector3 targetScale)
    {
        if (organ == null) return;

        var grabbable = organ.GetComponent<VRGrabbableOrgan>();
        if (grabbable != null)
        {
            grabbable.SmoothReturnToSocket();
        }
        else
        {
            StartCoroutine(MoveRoutine(organ.transform, targetPos, targetRot, targetScale, 0.5f));
        }
    }

    private void SyncWithVRComparisonBay()
    {
        if (VRComparisonBay.Instance != null)
        {
            VRGrabbableOrgan grabA = OrganA != null ? OrganA.GetComponent<VRGrabbableOrgan>() : null;
            VRGrabbableOrgan grabB = OrganB != null ? OrganB.GetComponent<VRGrabbableOrgan>() : null;

            VRComparisonBay.Instance.SetComparisonOrgans(OrganA, grabA, OrganB, grabB);
        }
    }

    private IEnumerator MoveRoutine(Transform target, Vector3 destPos, Quaternion destRot, Vector3 destScale, float duration = 0.5f)
    {
        if (target == null) yield break;

        float elapsed = 0f;
        Vector3 startPos = target.position;
        Quaternion startRot = target.rotation;
        Vector3 startScale = target.localScale;

        while (elapsed < duration)
        {
            if (target == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            target.position = Vector3.Lerp(startPos, destPos, t);
            target.rotation = Quaternion.Slerp(startRot, destRot, t);
            target.localScale = Vector3.Lerp(startScale, destScale, t);
            yield return null;
        }

        if (target != null)
        {
            target.position = destPos;
            target.rotation = destRot;
            target.localScale = destScale;
        }
    }

    private void GenerateProceduralClips()
    {
        extractClip = CreateProceduralChime(587.33f, 783.99f, 0.16f); // D5 -> G5
        snapClip = CreateProceduralChime(880f, 1174.66f, 0.18f);      // A5 -> D6
        resetClip = CreateProceduralChime(659.25f, 440f, 0.18f);       // E5 -> A4
    }

    private AudioClip CreateProceduralChime(float startFreq, float endFreq, float duration)
    {
        int sampleRate = 44100;
        int totalSamples = (int)(sampleRate * duration);
        float[] samples = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / sampleRate;
            float p = t / duration;
            float freq = Mathf.Lerp(startFreq, endFreq, p);
            float envelope = Mathf.Sin(p * Mathf.PI);
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.35f;
        }

        AudioClip clip = AudioClip.Create("Chime_" + startFreq, totalSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void PlayClip(AudioClip clip, float volume = 1f)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }
}
