using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplodedViewController : MonoBehaviour
{
    public static ExplodedViewController Instance { get; private set; }

    [System.Serializable]
    public class ExplodedPart
    {
        public string partName;
        public Transform partTransform;
        public Vector3 assembledLocalPos;
        public Vector3 explosionOffset;
        [HideInInspector] public bool isInitialized;
    }

    [Header("Exploded View Configuration")]
    [Range(0f, 1f)]
    public float explosionProgress = 0f;
    public float animationDuration = 1.0f;
    public List<ExplodedPart> parts = new List<ExplodedPart>();

    public bool IsExploded => explosionProgress > 0.05f;

    private Coroutine animCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        InitializeParts();
    }

    public void InitializeParts()
    {
        foreach (var p in parts)
        {
            if (p.partTransform != null && !p.isInitialized)
            {
                p.assembledLocalPos = p.partTransform.localPosition;
                p.isInitialized = true;
            }
        }
    }

    public void RegisterPart(string name, Transform t, Vector3 offset)
    {
        var existing = parts.Find(p => p.partName.Equals(name, System.StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            existing.partTransform = t;
            existing.assembledLocalPos = t.localPosition;
            existing.explosionOffset = offset;
            existing.isInitialized = true;
        }
        else
        {
            parts.Add(new ExplodedPart
            {
                partName = name,
                partTransform = t,
                assembledLocalPos = t.localPosition,
                explosionOffset = offset,
                isInitialized = true
            });
        }
    }

    public void SetExplosionProgress(float progress)
    {
        explosionProgress = Mathf.Clamp01(progress);
        foreach (var p in parts)
        {
            if (p.partTransform != null)
            {
                // Only move if not in isolation
                OrganInteractable organ = p.partTransform.GetComponent<OrganInteractable>();
                p.partTransform.localPosition = p.assembledLocalPos + p.explosionOffset * explosionProgress;
            }
        }
    }

    public void ToggleExplodedView()
    {
        float target = IsExploded ? 0f : 1f;
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(AnimateExplosionRoutine(target));
    }

    public void ResetExplosion()
    {
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(AnimateExplosionRoutine(0f));
    }

    private IEnumerator AnimateExplosionRoutine(float targetProgress)
    {
        float start = explosionProgress;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / animationDuration);
            SetExplosionProgress(Mathf.Lerp(start, targetProgress, t));
            yield return null;
        }

        SetExplosionProgress(targetProgress);
        animCoroutine = null;
    }
}
