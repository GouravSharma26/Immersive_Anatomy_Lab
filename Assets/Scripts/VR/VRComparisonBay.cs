using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ImmersiveAnatomy.VR
{
    public class VRComparisonBay : MonoBehaviour
    {
        public static VRComparisonBay Instance { get; private set; }

        [Header("Pedestals")]
        public Transform leftPedestalMount;
        public Transform rightPedestalMount;

        [Header("Comparison Board UI")]
        public Text titleText;
        public Text organANameText;
        public Text organBNameText;
        public Text metricsComparisonText;
        public Button clearPedestalsBtn;
        public Button swapPedestalsBtn;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip placeClip;
        public AudioClip resetClip;

        private OrganInteractable organA;
        private OrganInteractable organB;
        private VRGrabbableOrgan grabbableA;
        private VRGrabbableOrgan grabbableB;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);

            if (audioSource == null) audioSource = GetComponent<AudioSource>();

            // Ensure forward-facing rotation so wall board is never seen from behind
            if (transform.position.z < -3f)
            {
                transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }
        }

        private void Start()
        {
            if (clearPedestalsBtn != null) clearPedestalsBtn.onClick.AddListener(ClearPedestals);
            if (swapPedestalsBtn != null) swapPedestalsBtn.onClick.AddListener(SwapPedestals);

            DisplayDefaultComparison();
        }

        public void SetComparisonOrgans(OrganInteractable a, VRGrabbableOrgan grabA, OrganInteractable b, VRGrabbableOrgan grabB)
        {
            organA = a;
            grabbableA = grabA;
            organB = b;
            grabbableB = grabB;

            UpdateComparisonBoard();
        }

        public void PlaceOrganOnPedestal(OrganInteractable organ, VRGrabbableOrgan grabbable, bool isLeft)
        {
            if (isLeft)
            {
                organA = organ;
                grabbableA = grabbable;
            }
            else
            {
                organB = organ;
                grabbableB = grabbable;
            }

            PlaySound(placeClip, 0.8f);
            UpdateComparisonBoard();
        }

        public void ClearPedestals()
        {
            if (OrganComparisonController.Instance != null)
            {
                OrganComparisonController.Instance.ReturnAllOrgansToSockets();
            }
            else
            {
                if (grabbableA != null) grabbableA.SmoothReturnToSocket();
                if (grabbableB != null) grabbableB.SmoothReturnToSocket();

                organA = null;
                organB = null;
                grabbableA = null;
                grabbableB = null;

                PlaySound(resetClip, 0.9f);
                DisplayDefaultComparison();
            }
        }

        public void SwapPedestals()
        {
            if (OrganComparisonController.Instance != null)
            {
                OrganComparisonController.Instance.SwapPedestals();
            }
            else
            {
                var tempOrg = organA;
                organA = organB;
                organB = tempOrg;

                var tempGrab = grabbableA;
                grabbableA = grabbableB;
                grabbableB = tempGrab;

                PlaySound(placeClip, 0.8f);
                UpdateComparisonBoard();
            }
        }

        private void DisplayDefaultComparison()
        {
            if (organANameText != null) organANameText.text = "Left Pedestal: [Empty]";
            if (organBNameText != null) organBNameText.text = "Right Pedestal: [Empty]";
            if (metricsComparisonText != null)
            {
                metricsComparisonText.text = "<b>Pathology & Organ Comparison Bay</b>\n\nPlace any two extracted organs onto the left and right pedestals to view side-by-side comparative anatomy, mass ratio, vascular supply, and pathological correlates.\n\n<i>Tip: You can also examine them in midair and bring them here.</i>";
            }
        }

        private void UpdateComparisonBoard()
        {
            string nameA = organA != null ? organA.organName : "[Empty]";
            string nameB = organB != null ? organB.organName : "[Empty]";

            if (organANameText != null) organANameText.text = $"Specimen A: <color=#00E5FF>{nameA}</color>";
            if (organBNameText != null) organBNameText.text = $"Specimen B: <color=#FF88FF>{nameB}</color>";

            if (organA != null && organB != null)
            {
                string text = $"<b>COMPARATIVE ANATOMICAL ANALYSIS:</b>\n\n" +
                              $"<b>Mass:</b> {organA.organName} ({organA.normalWeight}g) vs {organB.organName} ({organB.normalWeight}g)  |  <b>Ratio:</b> {(float)organA.normalWeight / Mathf.Max(1f, organB.normalWeight):F2}x\n\n" +
                              $"<b>Vascular Architecture:</b>\n" +
                              $"• {organA.organName}: {organA.arterialSupply}\n" +
                              $"• {organB.organName}: {organB.arterialSupply}\n\n" +
                              $"<b>Autonomic & Somatic Innervation:</b>\n" +
                              $"• {organA.organName}: {organA.innervation}\n" +
                              $"• {organB.organName}: {organB.innervation}\n\n" +
                              $"<b>Clinical Pathologies:</b>\n" +
                              $"• {organA.organName}: {organA.commonPathology}\n" +
                              $"• {organB.organName}: {organB.commonPathology}";

                if (metricsComparisonText != null) metricsComparisonText.text = text;
            }
            else if (organA != null)
            {
                if (metricsComparisonText != null)
                {
                    metricsComparisonText.text = $"<b>Specimen A ({organA.organName}) Loaded.</b>\n\nNow place another organ on the Right Pedestal to initiate comparative analysis.\n\n<b>Mass:</b> {organA.normalWeight} g\n<b>System:</b> {organA.bodySystem}\n<b>Physiology:</b> {organA.clinicalFunction}";
                }
            }
            else if (organB != null)
            {
                if (metricsComparisonText != null)
                {
                    metricsComparisonText.text = $"<b>Specimen B ({organB.organName}) Loaded.</b>\n\nPlace another organ on the Left Pedestal to initiate comparative analysis.\n\n<b>Mass:</b> {organB.normalWeight} g\n<b>System:</b> {organB.bodySystem}\n<b>Physiology:</b> {organB.clinicalFunction}";
                }
            }
            else
            {
                DisplayDefaultComparison();
            }
        }

        private void PlaySound(AudioClip clip, float volume = 1f)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip, volume);
            }
        }
    }
}
