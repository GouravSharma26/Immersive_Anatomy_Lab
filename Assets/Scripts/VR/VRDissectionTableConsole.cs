using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ImmersiveAnatomy.VR
{
    public class VRDissectionTableConsole : MonoBehaviour
    {
        public static VRDissectionTableConsole Instance { get; private set; }

        [Header("Controllers")]
        public ExplodedViewController explodedViewController;
        public XRayViewController xrayViewController;
        public OrganIsolationController isolationController;

        [Header("Console Buttons")]
        public Button explodeBtn;
        public Button xrayBtn;
        public Button resetAnatomyBtn;
        public Text explodeBtnText;
        public Text xrayBtnText;

        [Header("System Toggle Buttons")]
        public Button skeletalBtn;
        public Button cardioBtn;
        public Button respBtn;
        public Button nervousBtn;
        public Button digestiveBtn;
        public Button showAllBtn;

        [Header("Holographic Organ Card (Side Display)")]
        public GameObject organCardRoot;
        public Text cardOrganNameText;
        public Text cardSystemText;
        public Text cardPhysiologyText;
        public Text cardVascularText;
        public Text cardPathologyText;
        public Button returnToSocketBtn;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip clickClip;
        public AudioClip resetClip;

        private VRGrabbableOrgan activeExaminedOrgan;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);

            if (audioSource == null) audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            FindControllers();
            InitButtons();
            UpdateCardDefault();
        }

        private void FindControllers()
        {
            if (explodedViewController == null) explodedViewController = FindFirstObjectByType<ExplodedViewController>();
            if (xrayViewController == null) xrayViewController = FindFirstObjectByType<XRayViewController>();
            if (isolationController == null) isolationController = FindFirstObjectByType<OrganIsolationController>();
        }

        private void InitButtons()
        {
            if (explodeBtn != null)
            {
                explodeBtn.onClick.AddListener(ToggleExplodedView);
            }

            if (xrayBtn != null)
            {
                xrayBtn.onClick.AddListener(ToggleXRayView);
            }

            if (resetAnatomyBtn != null)
            {
                resetAnatomyBtn.onClick.AddListener(ResetAnatomy);
            }

            if (skeletalBtn != null) skeletalBtn.onClick.AddListener(() => ToggleSystem("Skeletal"));
            if (cardioBtn != null) cardioBtn.onClick.AddListener(() => ToggleSystem("Cardiovascular"));
            if (respBtn != null) respBtn.onClick.AddListener(() => ToggleSystem("Respiratory"));
            if (nervousBtn != null) nervousBtn.onClick.AddListener(() => ToggleSystem("Nervous"));
            if (digestiveBtn != null) digestiveBtn.onClick.AddListener(() => ToggleSystem("Digestive"));

            if (showAllBtn != null)
            {
                showAllBtn.onClick.AddListener(() =>
                {
                    if (BodySystemManager.Instance != null)
                    {
                        BodySystemManager.Instance.ShowAllSystems();
                        PlaySound(clickClip, 0.8f);
                    }
                });
            }

            if (returnToSocketBtn != null)
            {
                returnToSocketBtn.onClick.AddListener(() =>
                {
                    if (activeExaminedOrgan != null)
                    {
                        activeExaminedOrgan.SmoothReturnToSocket();
                        PlaySound(resetClip, 0.9f);
                    }
                });
            }
        }

        public void ToggleExplodedView()
        {
            if (explodedViewController == null) FindControllers();
            if (explodedViewController != null)
            {
                explodedViewController.ToggleExplodedView();
                if (explodeBtnText != null)
                {
                    explodeBtnText.text = explodedViewController.IsExploded ? "Assembly View" : "Exploded View";
                }
                PlaySound(clickClip, 0.8f);
            }
        }

        public void ToggleXRayView()
        {
            if (xrayViewController == null) FindControllers();
            if (xrayViewController != null)
            {
                xrayViewController.ToggleXRay();
                if (xrayBtnText != null)
                {
                    xrayBtnText.text = xrayViewController.IsXRayActive ? "Normal Opacity" : "X-Ray Mode";
                }
                PlaySound(clickClip, 0.8f);
            }
        }

        public void ResetAnatomy()
        {
            if (explodedViewController == null) FindControllers();
            if (explodedViewController != null && explodedViewController.IsExploded)
            {
                explodedViewController.ResetExplosion();
            }

            if (xrayViewController != null && xrayViewController.IsXRayActive)
            {
                xrayViewController.SetXRayActive(false);
            }

            if (isolationController != null && isolationController.IsInIsolationMode)
            {
                isolationController.ExitIsolation();
            }

            if (BodySystemManager.Instance != null)
            {
                BodySystemManager.Instance.ShowAllSystems();
            }

            // Snap all organs back to socket
            VRGrabbableOrgan[] allOrgans = FindObjectsByType<VRGrabbableOrgan>(FindObjectsSortMode.None);
            foreach (var organ in allOrgans)
            {
                organ.SmoothReturnToSocket();
            }

            if (explodeBtnText != null) explodeBtnText.text = "Exploded View";
            if (xrayBtnText != null) xrayBtnText.text = "X-Ray Mode";

            PlaySound(resetClip, 1f);
            UpdateCardDefault();
        }

        private void ToggleSystem(string systemName)
        {
            if (BodySystemManager.Instance != null)
            {
                BodySystemManager.Instance.ToggleSystem(systemName);
                PlaySound(clickClip, 0.8f);
            }
        }

        public void DisplayOrganMetrics(OrganInteractable data, VRGrabbableOrgan grabbable)
        {
            activeExaminedOrgan = grabbable;

            if (data == null)
            {
                UpdateCardDefault();
                return;
            }

            if (cardOrganNameText != null) cardOrganNameText.text = data.organName;
            if (cardSystemText != null) cardSystemText.text = $"System: <color=#00FFAA>{data.bodySystem}</color>  |  Mass: <color=#FFDD44>{data.normalWeight} g</color>";
            if (cardPhysiologyText != null) cardPhysiologyText.text = $"<b>Physiology:</b> {data.clinicalFunction}";
            if (cardVascularText != null) cardVascularText.text = $"<b>Blood Supply:</b> {data.arterialSupply}";
            if (cardPathologyText != null) cardPathologyText.text = $"<b>Clinical Pathology:</b> {data.commonPathology}";

            if (returnToSocketBtn != null) returnToSocketBtn.gameObject.SetActive(true);
        }

        public void UpdateCardDefault()
        {
            activeExaminedOrgan = null;
            if (cardOrganNameText != null) cardOrganNameText.text = "Central Anatomy Specimen";
            if (cardSystemText != null) cardSystemText.text = "Grab an organ with your VR controller grip to inspect live metrics.";
            if (cardPhysiologyText != null) cardPhysiologyText.text = "<b>Exam Tip:</b> Point your controller laser at any organ to highlight, or middle-click (grip) to extract and inspect in 3D.";
            if (cardVascularText != null) cardVascularText.text = "<b>Sockets:</b> Releasing near the body will magnetically snap the organ into its correct anatomical socket.";
            if (cardPathologyText != null) cardPathologyText.text = "<b>Stations:</b> Visit the Left Kiosk for Quizzes & Cases, or the Right Pod for Dr. Nova AI.";
            if (returnToSocketBtn != null) returnToSocketBtn.gameObject.SetActive(false);
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
