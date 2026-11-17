using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ImmersiveAnatomy.VR
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class VRGrabbableOrgan : XRGrabInteractable
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

            // Configure Rigidbody for smooth 6-DOF VR Grabbing
            organRigidbody.useGravity = false;
            organRigidbody.isKinematic = true;
            organRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            organRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // Configure XRI grab settings
            movementType = MovementType.VelocityTracking;
            throwOnDetach = false; // Zero loss - medical organs float rather than throwing across the room

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

        private void Update()
        {
            if (isReturning) return;

            // When organ is actively being held, continuously check proximity to comparison pillars for auto-snap!
            if (isSelected)
            {
                CheckHeldProximitySnap();
            }
        }

        private void CheckHeldProximitySnap()
        {
            Transform leftPed = OrganComparisonController.Instance != null ? OrganComparisonController.Instance.leftPedestal : (VRComparisonBay.Instance != null ? VRComparisonBay.Instance.leftPedestalMount : null);
            Transform rightPed = OrganComparisonController.Instance != null ? OrganComparisonController.Instance.rightPedestal : (VRComparisonBay.Instance != null ? VRComparisonBay.Instance.rightPedestalMount : null);

            float snapDist = 0.95f;

            if (leftPed != null && Vector3.Distance(transform.position, leftPed.position) < snapDist)
            {
                ForceReleaseGrab();
                if (OrganComparisonController.Instance != null)
                {
                    OrganComparisonController.Instance.SnapOrganToPedestalA(organData);
                }
                else if (VRComparisonBay.Instance != null)
                {
                    transform.position = leftPed.position + Vector3.up * 0.18f;
                    organRigidbody.linearVelocity = Vector3.zero;
                    organRigidbody.angularVelocity = Vector3.zero;
                    organRigidbody.isKinematic = true;
                    VRComparisonBay.Instance.PlaceOrganOnPedestal(organData, this, true);
                }
            }
            else if (rightPed != null && Vector3.Distance(transform.position, rightPed.position) < snapDist)
            {
                ForceReleaseGrab();
                if (OrganComparisonController.Instance != null)
                {
                    OrganComparisonController.Instance.SnapOrganToPedestalB(organData);
                }
                else if (VRComparisonBay.Instance != null)
                {
                    transform.position = rightPed.position + Vector3.up * 0.18f;
                    organRigidbody.linearVelocity = Vector3.zero;
                    organRigidbody.angularVelocity = Vector3.zero;
                    organRigidbody.isKinematic = true;
                    VRComparisonBay.Instance.PlaceOrganOnPedestal(organData, this, false);
                }
            }
        }

        public void ForceReleaseGrab()
        {
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

            organRigidbody.isKinematic = false;
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
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);

            Vector3 targetSocketWorld = GetSocketWorldPosition();
            float dist = Vector3.Distance(transform.position, targetSocketWorld);

            if (dist <= snapDistance)
            {
                SnapToSocket();
            }
            else
            {
                // Check Comparison Pedestal A
                Transform leftPed = OrganComparisonController.Instance != null ? OrganComparisonController.Instance.leftPedestal : (VRComparisonBay.Instance != null ? VRComparisonBay.Instance.leftPedestalMount : null);
                Transform rightPed = OrganComparisonController.Instance != null ? OrganComparisonController.Instance.rightPedestal : (VRComparisonBay.Instance != null ? VRComparisonBay.Instance.rightPedestalMount : null);

                if (leftPed != null && Vector3.Distance(transform.position, leftPed.position) < 0.75f)
                {
                    if (OrganComparisonController.Instance != null)
                    {
                        OrganComparisonController.Instance.SnapOrganToPedestalA(organData);
                    }
                    else if (VRComparisonBay.Instance != null)
                    {
                        transform.position = leftPed.position + Vector3.up * 0.18f;
                        organRigidbody.linearVelocity = Vector3.zero;
                        organRigidbody.angularVelocity = Vector3.zero;
                        organRigidbody.isKinematic = true;
                        VRComparisonBay.Instance.PlaceOrganOnPedestal(organData, this, true);
                    }
                }
                else if (rightPed != null && Vector3.Distance(transform.position, rightPed.position) < 0.75f)
                {
                    if (OrganComparisonController.Instance != null)
                    {
                        OrganComparisonController.Instance.SnapOrganToPedestalB(organData);
                    }
                    else if (VRComparisonBay.Instance != null)
                    {
                        transform.position = rightPed.position + Vector3.up * 0.18f;
                        organRigidbody.linearVelocity = Vector3.zero;
                        organRigidbody.angularVelocity = Vector3.zero;
                        organRigidbody.isKinematic = true;
                        VRComparisonBay.Instance.PlaceOrganOnPedestal(organData, this, false);
                    }
                }
                else if (floatWhenReleased)
                {
                    // Float stably in midair for 360-degree surgical examination
                    organRigidbody.linearVelocity = Vector3.zero;
                    organRigidbody.angularVelocity = Vector3.zero;
                    organRigidbody.isKinematic = true;
                    PlaySound(releaseAudioClip, 0.5f);
                }
                else
                {
                    organRigidbody.isKinematic = false;
                    organRigidbody.useGravity = true;
                }
            }
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
// Phase 6 XR Organ Grab & Socket Snapping Verified
