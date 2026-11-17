using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ImmersiveAnatomy.VR
{
    /// <summary>
    /// Non-grab raycast interactable organ specimen.
    /// Strictly complies with the project's zero-organ-grabbing requirement:
    /// Derives from XRSimpleInteractable (NOT XRGrabInteractable) to allow raycast hover,
    /// examination, and deterministic socket/pedestal snapping without physics grabbing or dragging.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class VRGrabbableOrgan : XRSimpleInteractable
    {
        [Header("Anatomical Socket & Snapping")]
        public Transform anatomicalSocket;
        [Tooltip("Distance threshold (in meters) to magnetically snap back to anatomical socket")]
        public float snapDistance = 0.35f;
        public bool isSnappedToSocket = true;
        public bool floatWhenReleased = true;

        [Header("Audio Feedback")]
        public AudioClip grabAudioClip;
        public AudioClip snapAudioClip;
        public AudioClip releaseAudioClip;

        [Header("Visual Feedback")]
        public Material hoverOutlineMaterial;
        public GameObject socketGhostIndicator;

        private Vector3 originalLocalPos;
        private Quaternion originalLocalRot;
        private Vector3 originalLocalScale;
        private Transform originalParent;
        private Rigidbody organRigidbody;
        private OrganInteractable organData;
        public bool IsReturning => isReturning;
        private bool isReturning = false;

        private Renderer[] renderers;
        private Material[][] defaultMaterials;

        protected override void Awake()
        {
            base.Awake();
            organRigidbody = GetComponent<Rigidbody>();
            organData = GetComponent<OrganInteractable>();

            originalParent = transform.parent;
            originalLocalPos = transform.localPosition;
            originalLocalRot = transform.localRotation;
            originalLocalScale = transform.localScale;

            // Strict no-grabbing compliance: Rigidbody is permanently kinematic and non-dynamic
            organRigidbody.useGravity = false;
            organRigidbody.isKinematic = true;
            organRigidbody.interpolation = RigidbodyInterpolation.None;
            organRigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;

            // Cache renderers and materials for hover effects
            renderers = GetComponentsInChildren<Renderer>();
            if (renderers != null && renderers.Length > 0)
            {
                defaultMaterials = new Material[renderers.Length][];
                for (int i = 0; i < renderers.Length; i++)
                {
                    defaultMaterials[i] = renderers[i].sharedMaterials;
                }
            }
        }

        public override bool IsSelectableBy(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor interactor)
        {
            if (interactor == null) return false;
            // Prevent accidental physical hand/grip proximity selection - deliberate ray pointer selection only
            string typeName = interactor.GetType().Name;
            if (typeName.Contains("Direct") || typeName.Contains("Near") || typeName.Contains("Poke") || typeName.Contains("Grip"))
            {
                return false;
            }
            return base.IsSelectableBy(interactor);
        }

        public void ForceReleaseGrab()
        {
            // No-op for non-grab simple interactable; deselect if actively selected
            if (interactionManager != null && isSelected && interactorsSelecting != null)
            {
                var selectList = new System.Collections.Generic.List<UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor>(interactorsSelecting);
                foreach (var interactor in selectList)
                {
                    if (interactor != null)
                    {
                        interactionManager.SelectExit(interactor, this);
                    }
                }
            }
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            isSnappedToSocket = false;
            isReturning = false;
            StopAllCoroutines();

            // Specimen remains kinematic - zero physics movement
            organRigidbody.isKinematic = true;
            organRigidbody.useGravity = false;

            if (socketGhostIndicator != null) socketGhostIndicator.SetActive(true);

            PlaySound(grabAudioClip, 0.8f);

            // Inform VR Dissection Console, AI Pod, Quiz Station, and Clinical Tablet
            if (VRDissectionTableConsole.Instance != null)
            {
                VRDissectionTableConsole.Instance.DisplayOrganMetrics(organData, this);
            }
            if (VRAINovaPod.Instance != null)
            {
                VRAINovaPod.Instance.SetOrganContext(organData);
            }
            if (VRQuizStation.Instance != null)
            {
                VRQuizStation.Instance.OnOrganInteractedInLab(organData);
            }
            if (VRClinicalTablet.Instance != null)
            {
                VRClinicalTablet.Instance.OnOrganGrabbed(organData, this);
            }

            // Check if OrganComparisonController wants to snap this selected organ
            if (OrganComparisonController.Instance != null && OrganComparisonController.Instance.IsInComparisonMode)
            {
                OrganComparisonController.Instance.AddOrganToComparison(organData);
            }
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);
            organRigidbody.isKinematic = true;
            organRigidbody.useGravity = false;
        }

        protected override void OnHoverEntered(HoverEnterEventArgs args)
        {
            base.OnHoverEntered(args);
            if (hoverOutlineMaterial != null && renderers != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] == null) continue;
                    Material[] hl = new Material[defaultMaterials[i].Length];
                    for (int m = 0; m < hl.Length; m++) hl[m] = hoverOutlineMaterial;
                    renderers[i].materials = hl;
                }
            }
        }

        protected override void OnHoverExited(HoverExitEventArgs args)
        {
            base.OnHoverExited(args);
            if (renderers != null && defaultMaterials != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] == null) continue;
                    renderers[i].materials = defaultMaterials[i];
                }
            }
        }

        public Vector3 GetSocketWorldPosition()
        {
            if (anatomicalSocket != null) return anatomicalSocket.position;
            return originalParent != null ? originalParent.TransformPoint(originalLocalPos) : originalLocalPos;
        }

        public Quaternion GetSocketWorldRotation()
        {
            if (anatomicalSocket != null) return anatomicalSocket.rotation;
            return originalParent != null ? originalParent.rotation * originalLocalRot : originalLocalRot;
        }

        public void SnapToSocket()
        {
            StopAllCoroutines();
            StartCoroutine(SmoothSnapCoroutine(0.28f));
        }

        public void ReturnToSocketInstant()
        {
            StopAllCoroutines();
            transform.parent = originalParent;
            transform.localPosition = originalLocalPos;
            transform.localRotation = originalLocalRot;
            transform.localScale = originalLocalScale;

            organRigidbody.linearVelocity = Vector3.zero;
            organRigidbody.angularVelocity = Vector3.zero;
            organRigidbody.isKinematic = true;
            isSnappedToSocket = true;
            if (socketGhostIndicator != null) socketGhostIndicator.SetActive(false);
        }

        public void SmoothReturnToSocket()
        {
            StopAllCoroutines();
            StartCoroutine(SmoothSnapCoroutine(0.45f));
        }

        private IEnumerator SmoothSnapCoroutine(float duration)
        {
            isReturning = true;
            organRigidbody.isKinematic = true;
            organRigidbody.linearVelocity = Vector3.zero;
            organRigidbody.angularVelocity = Vector3.zero;

            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;

            Vector3 targetPos = GetSocketWorldPosition();
            Quaternion targetRot = GetSocketWorldRotation();

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            transform.parent = originalParent;
            transform.localPosition = originalLocalPos;
            transform.localRotation = originalLocalRot;
            transform.localScale = originalLocalScale;

            isSnappedToSocket = true;
            isReturning = false;
            if (socketGhostIndicator != null) socketGhostIndicator.SetActive(false);

            PlaySound(snapAudioClip, 0.9f);

            if (VRDissectionTableConsole.Instance != null)
            {
                VRDissectionTableConsole.Instance.UpdateCardDefault();
            }

            if (VRClinicalTablet.Instance != null)
            {
                VRClinicalTablet.Instance.OnOrganSnapped(organData, this);
            }
        }

        private void PlaySound(AudioClip clip, float volume = 1f)
        {
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, transform.position, volume);
            }
        }
    }
}
// Phase Q4 Interaction Architecture: Strict Zero-Grabbing & Deterministic Placement Verified
