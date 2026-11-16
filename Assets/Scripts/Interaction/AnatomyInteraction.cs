using UnityEngine;

public class AnatomyInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public Camera playerCamera;
    public float interactionDistance = 15f;

    [Header("Controllers")]
    public OrganHighlighter highlighter;
    public OrganIsolationController isolationController;
    public ExplodedViewController explodedViewController;
    public OrganComparisonController comparisonController;
    public XRayViewController xrayViewController;
    public CameraInspectionController cameraInspectionController;
    public ImmersiveAnatomy.Learning.AnatomyLearningUI learningUI;

    private OrganInteractable hoveredOrgan;
    private OrganInteractable selectedOrgan;

    private void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (highlighter == null) highlighter = GetComponent<OrganHighlighter>();
        if (isolationController == null) isolationController = GetComponent<OrganIsolationController>();
        if (explodedViewController == null) explodedViewController = GetComponent<ExplodedViewController>();
        if (comparisonController == null) comparisonController = GetComponent<OrganComparisonController>();
        if (xrayViewController == null) xrayViewController = GetComponent<XRayViewController>();
        if (cameraInspectionController == null) cameraInspectionController = GetComponent<CameraInspectionController>();
        if (cameraInspectionController == null) cameraInspectionController = FindFirstObjectByType<CameraInspectionController>();
        if (learningUI == null) learningUI = GetComponent<ImmersiveAnatomy.Learning.AnatomyLearningUI>();
        if (learningUI == null) learningUI = FindFirstObjectByType<ImmersiveAnatomy.Learning.AnatomyLearningUI>();
    }

    private void Update()
    {
        HandleRaycastHover();

        if (Input.GetMouseButtonDown(0))
        {
            HandleSelection();
        }

        HandleKeyboardShortcuts();

        // Organ rotation & zooming
        if (isolationController != null && isolationController.IsInIsolationMode)
        {
            float rotSpeed = 75f * Time.deltaTime;
            if (Input.GetKey(KeyCode.Q)) isolationController.RotateIsolatedOrgan(-rotSpeed, 0);
            if (Input.GetKey(KeyCode.E)) isolationController.RotateIsolatedOrgan(rotSpeed, 0);
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            isolationController.ZoomIsolatedOrgan(scroll);
        }
        else if (comparisonController != null && comparisonController.IsInComparisonMode)
        {
            float rotSpeed = 70f * Time.deltaTime;
            if (Input.GetKey(KeyCode.Q)) comparisonController.RotateComparedOrgans(-rotSpeed);
            if (Input.GetKey(KeyCode.E)) comparisonController.RotateComparedOrgans(rotSpeed);
        }
        else if (selectedOrgan != null)
        {
            float rotSpeed = 65f * Time.deltaTime;
            if (Input.GetKey(KeyCode.Q)) selectedOrgan.transform.Rotate(Vector3.up, -rotSpeed, Space.World);
            if (Input.GetKey(KeyCode.E)) selectedOrgan.transform.Rotate(Vector3.up, rotSpeed, Space.World);
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                Vector3 scale = selectedOrgan.transform.localScale + Vector3.one * scroll * 0.4f;
                float newScale = Mathf.Clamp(scale.x, 0.2f, 3f);
                selectedOrgan.transform.localScale = Vector3.one * newScale;
            }
        }
    }

    private void HandleRaycastHover()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            OrganInteractable organ = hit.collider.GetComponentInParent<OrganInteractable>();
            if (organ != hoveredOrgan)
            {
                if (hoveredOrgan != null && highlighter != null) highlighter.OnHoverExit(hoveredOrgan);
                hoveredOrgan = organ;
                if (hoveredOrgan != null && highlighter != null) highlighter.OnHoverEnter(hoveredOrgan);
            }
        }
        else
        {
            if (hoveredOrgan != null)
            {
                if (highlighter != null) highlighter.OnHoverExit(hoveredOrgan);
                hoveredOrgan = null;
            }
        }
    }

    private void HandleSelection()
    {
        Vector2 mousePos = Input.mousePosition;
        // Don't select if clicking on GUI bounds
        if (mousePos.x < 245 && mousePos.y > Screen.height - 450) return;
        if (mousePos.x > Screen.width - 380 && mousePos.y > Screen.height - 490) return;
        if (learningUI != null && learningUI.isLearningUIOpen)
        {
            if (mousePos.x >= 255 && mousePos.x <= 810 && mousePos.y > Screen.height - 565) return;
        }

        if (hoveredOrgan != null)
        {
            if (comparisonController != null && comparisonController.IsInComparisonMode)
            {
                comparisonController.AddOrganToComparison(hoveredOrgan);
                if (highlighter != null) highlighter.SelectOrgan(hoveredOrgan);
                return;
            }

            selectedOrgan = hoveredOrgan;
            selectedOrgan.ShowInformation();
            if (highlighter != null) highlighter.SelectOrgan(selectedOrgan);
            if (learningUI != null) learningUI.NotifyOrganSelected(selectedOrgan);
        }
    }

    private void HandleKeyboardShortcuts()
    {
        // Learning Mode: L
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (learningUI != null) learningUI.ToggleLearningUI();
        }
        // 1-6 Body System visibility
        if (BodySystemManager.Instance != null)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) BodySystemManager.Instance.ToggleSystem("Skeletal");
            if (Input.GetKeyDown(KeyCode.Alpha2)) BodySystemManager.Instance.ToggleSystem("Cardiovascular");
            if (Input.GetKeyDown(KeyCode.Alpha3)) BodySystemManager.Instance.ToggleSystem("Respiratory");
            if (Input.GetKeyDown(KeyCode.Alpha4)) BodySystemManager.Instance.ToggleSystem("Nervous");
            if (Input.GetKeyDown(KeyCode.Alpha5)) BodySystemManager.Instance.ToggleSystem("Digestive");
            if (Input.GetKeyDown(KeyCode.Alpha6)) BodySystemManager.Instance.ToggleSystem("Body");
        }

        // Exploded View: X
        if (Input.GetKeyDown(KeyCode.X))
        {
            if (explodedViewController != null) explodedViewController.ToggleExplodedView();
        }

        // X-Ray View: Z
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (xrayViewController != null) xrayViewController.ToggleXRay();
        }

        // Comparison Mode: C
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (comparisonController != null) comparisonController.ToggleComparisonMode(selectedOrgan);
        }

        // Camera Focus & Orbit: F
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (cameraInspectionController != null)
            {
                Transform target = (isolationController != null && isolationController.IsInIsolationMode && isolationController.IsolatedOrgan != null)
                    ? isolationController.IsolatedOrgan.transform
                    : (selectedOrgan != null ? selectedOrgan.transform : null);
                if (target != null) cameraInspectionController.ToggleFocus(target);
            }
        }

        // Isolation: I
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleIsolationMode();
        }

        // Reset / Deselect: R
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetAllAnatomy();
        }
    }

    public void ToggleIsolationMode()
    {
        if (isolationController == null) return;

        if (isolationController.IsInIsolationMode)
        {
            isolationController.ExitIsolation();
        }
        else if (selectedOrgan != null)
        {
            isolationController.EnterIsolation(selectedOrgan);
        }
    }

    public void ResetAllAnatomy()
    {
        if (cameraInspectionController != null && cameraInspectionController.IsFocusMode)
        {
            cameraInspectionController.ExitFocusMode();
        }
        if (isolationController != null && isolationController.IsInIsolationMode)
        {
            isolationController.ExitIsolation();
        }
        if (comparisonController != null && comparisonController.IsInComparisonMode)
        {
            comparisonController.ExitComparisonMode();
        }
        if (explodedViewController != null && explodedViewController.IsExploded)
        {
            explodedViewController.ResetExplosion();
        }
        if (xrayViewController != null && xrayViewController.IsXRayActive)
        {
            xrayViewController.SetXRayActive(false);
        }
        if (BodySystemManager.Instance != null)
        {
            BodySystemManager.Instance.ShowAllSystems();
        }

        selectedOrgan = null;
        if (highlighter != null) highlighter.ClearSelection();
    }

    private void OnGUI()
    {
        // 1. TOP HEADER BANNER
        GUI.Box(new Rect(15, 12, Screen.width - 30, 42), "");
        GUI.Label(new Rect(25, 22, 590, 24), "<b>IMMERSIVE ANATOMY LAB</b>  |  Phase 5 Intelligent AI Anatomy Assistant (LLM + RAG + TTS)");

        string modeStatus = "<color=#00E5FF>EXPLORATION</color>";
        if (learningUI != null && learningUI.isLearningUIOpen)
            modeStatus = "<color=#00FF88>CLINICAL QUIZ & AI TUTOR</color>";
        else if (cameraInspectionController != null && cameraInspectionController.IsFocusMode)
            modeStatus = "<color=#33FF88>CAMERA FOCUS & ORBIT MODE</color>";
        else if (explodedViewController != null && explodedViewController.IsExploded)
            modeStatus = "<color=#FF9900>EXPLODED ANATOMY VIEW</color>";
        else if (isolationController != null && isolationController.IsInIsolationMode)
            modeStatus = "<color=#FFCC00>ORGAN ISOLATION MODE</color>";
        else if (comparisonController != null && comparisonController.IsInComparisonMode)
            modeStatus = "<color=#CC44FF>ORGAN COMPARISON MODE</color>";
        else if (xrayViewController != null && xrayViewController.IsXRayActive)
            modeStatus = "<color=#00FFFF>X-RAY FLUOROSCOPY MODE</color>";

        GUI.Label(new Rect(Screen.width - 350, 22, 330, 24), $"<b>ACTIVE MODE:</b> {modeStatus}");

        // 2. LEFT PANEL: ANATOMICAL SYSTEMS & ADVANCED VIEW MODES
        GUI.Box(new Rect(15, 62, 230, 435), "<b>INTERACTIVE CONTROLS</b>");
        
        // System Visibility Toggles
        if (BodySystemManager.Instance != null)
        {
            GUI.Label(new Rect(25, 86, 210, 20), "<b>Body Systems Visibility:</b>");
            string[] sysKeys = new string[] { "Body", "Skeletal", "Cardiovascular", "Respiratory", "Nervous", "Digestive" };
            string[] sysNums = new string[] { "[6]", "[1]", "[2]", "[3]", "[4]", "[5]" };
            for (int i = 0; i < sysKeys.Length; i++)
            {
                bool isVis = BodySystemManager.Instance.IsSystemVisible(sysKeys[i]);
                string label = $"{sysNums[i]} {sysKeys[i]}: {(isVis ? "<color=#00FF88>ON</color>" : "<color=#FF4444>OFF</color>")}";
                if (GUI.Button(new Rect(25, 108 + i * 26, 210, 22), label))
                {
                    BodySystemManager.Instance.ToggleSystem(sysKeys[i]);
                }
            }
        }

        // View Mode Toggles
        GUI.Label(new Rect(25, 272, 210, 20), "<b>Advanced View Modes:</b>");
        
        // Exploded View Button
        bool isExp = explodedViewController != null && explodedViewController.IsExploded;
        string expText = isExp ? "Assemble Anatomy [X]" : "Exploded View [X]";
        if (GUI.Button(new Rect(25, 294, 210, 24), expText))
        {
            if (explodedViewController != null) explodedViewController.ToggleExplodedView();
        }

        // X-Ray Mode Button
        bool isXRay = xrayViewController != null && xrayViewController.IsXRayActive;
        string xrayText = isXRay ? "Disable X-Ray [Z]" : "X-Ray Mode [Z]";
        if (GUI.Button(new Rect(25, 322, 210, 24), xrayText))
        {
            if (xrayViewController != null) xrayViewController.ToggleXRay();
        }

        // Comparison Mode Button
        bool isComp = comparisonController != null && comparisonController.IsInComparisonMode;
        string compText = isComp ? "Exit Comparison [C]" : "Compare Organs [C]";
        if (GUI.Button(new Rect(25, 350, 210, 24), compText))
        {
            if (comparisonController != null) comparisonController.ToggleComparisonMode(selectedOrgan);
        }

        // Learning Mode Button
        bool isLearn = learningUI != null && learningUI.isLearningUIOpen;
        string learnText = isLearn ? "Exit Learning [L]" : "<b>Quiz & AI Tutor [L]</b>";
        if (GUI.Button(new Rect(25, 378, 210, 24), learnText))
        {
            if (learningUI != null) learningUI.ToggleLearningUI();
        }

        // Reset Anatomy Button
        if (GUI.Button(new Rect(25, 406, 210, 24), "RESET ANATOMY [R]"))
        {
            ResetAllAnatomy();
        }

        // 3. RIGHT PANEL: CLINICAL INSPECTOR OR COMPARISON MATRIX
        int rightW = 360;
        int rightX = Screen.width - rightW - 15;

        if (comparisonController != null && comparisonController.IsInComparisonMode)
        {
            // COMPARISON MATRIX PANEL
            GUI.Box(new Rect(rightX, 62, rightW, 430), "<b>ORGAN COMPARISON MATRIX</b>");
            
            OrganInteractable a = comparisonController.OrganA;
            OrganInteractable b = comparisonController.OrganB;

            if (a == null)
            {
                GUI.Label(new Rect(rightX + 15, 95, rightW - 30, 40), "<i>Click any organ to place it on Pedestal A.</i>");
            }
            else
            {
                GUI.Label(new Rect(rightX + 15, 90, rightW - 30, 22), $"<b>Organ A:</b> <color=#00E5FF>{a.organName}</color> ({a.bodySystem})");
                GUI.Label(new Rect(rightX + 15, 110, rightW - 30, 20), $"Mass: <b>{a.normalWeight}g</b> | Supply: {a.arterialSupply}");
            }

            GUI.Box(new Rect(rightX + 15, 140, rightW - 30, 2), "");

            if (b == null)
            {
                GUI.Label(new Rect(rightX + 15, 150, rightW - 30, 40), "<i>Click a second organ to place on Pedestal B for side-by-side comparative analysis.</i>");
            }
            else
            {
                GUI.Label(new Rect(rightX + 15, 150, rightW - 30, 22), $"<b>Organ B:</b> <color=#FFCC00>{b.organName}</color> ({b.bodySystem})");
                GUI.Label(new Rect(rightX + 15, 170, rightW - 30, 20), $"Mass: <b>{b.normalWeight}g</b> | Supply: {b.arterialSupply}");

                // Comparative Differential
                if (a != null)
                {
                    float diff = Mathf.Abs(a.normalWeight - b.normalWeight);
                    string heavier = a.normalWeight >= b.normalWeight ? a.organName : b.organName;
                    GUI.Label(new Rect(rightX + 15, 200, rightW - 30, 22), $"<b>Mass Difference:</b> {diff:F0}g ({heavier} is heavier)");
                    
                    GUI.Label(new Rect(rightX + 15, 230, rightW - 30, 50), $"<b>{a.organName} Physiology:</b>\n{a.description}");
                    GUI.Label(new Rect(rightX + 15, 290, rightW - 30, 50), $"<b>{b.organName} Physiology:</b>\n{b.description}");
                    GUI.Label(new Rect(rightX + 15, 350, rightW - 30, 40), $"<b>Pathology Contrast:</b>\n{a.commonPathology} vs {b.commonPathology}");
                }
            }

            if (GUI.Button(new Rect(rightX + 15, 395, rightW - 30, 26), "Exit Comparison Mode [C]"))
            {
                comparisonController.ExitComparisonMode();
            }
        }
        else
        {
            // SINGLE ORGAN CLINICAL INSPECTOR
            GUI.Box(new Rect(rightX, 62, rightW, 465), "<b>CLINICAL ORGAN INSPECTOR</b>");

            if (selectedOrgan != null)
            {
                GUI.Label(new Rect(rightX + 15, 90, rightW - 30, 22), $"<b>Organ:</b> <color=#00E5FF>{selectedOrgan.organName}</color>");
                GUI.Label(new Rect(rightX + 15, 112, rightW - 30, 22), $"<b>System:</b> {selectedOrgan.bodySystem}  |  <b>Mass:</b> {selectedOrgan.normalWeight}g");
                GUI.Label(new Rect(rightX + 15, 136, rightW - 30, 48), $"<b>Physiology:</b>\n{selectedOrgan.description}");
                GUI.Label(new Rect(rightX + 15, 188, rightW - 30, 42), $"<b>Blood Supply:</b> {selectedOrgan.arterialSupply}");
                GUI.Label(new Rect(rightX + 15, 234, rightW - 30, 42), $"<b>Innervation:</b> {selectedOrgan.innervation}");
                GUI.Label(new Rect(rightX + 15, 280, rightW - 30, 42), $"<b>Clinical Notes:</b> {selectedOrgan.commonPathology}");

                bool inIso = isolationController != null && isolationController.IsInIsolationMode;
                string isoText = inIso ? "Exit Isolation [I]" : "Isolate Organ [I]";
                if (GUI.Button(new Rect(rightX + 15, 330, 160, 26), isoText))
                {
                    ToggleIsolationMode();
                }

                if (GUI.Button(new Rect(rightX + 185, 330, 160, 26), "Compare [C]"))
                {
                    if (comparisonController != null) comparisonController.StartComparisonMode(selectedOrgan);
                }

                bool isFocus = cameraInspectionController != null && cameraInspectionController.IsFocusMode;
                string focusText = isFocus ? "Exit Focus [F]" : "Focus Camera & Orbit [F]";
                if (GUI.Button(new Rect(rightX + 15, 362, rightW - 30, 26), focusText))
                {
                    if (cameraInspectionController != null)
                    {
                        Transform target = (isolationController != null && isolationController.IsInIsolationMode && isolationController.IsolatedOrgan != null)
                            ? isolationController.IsolatedOrgan.transform
                            : (selectedOrgan != null ? selectedOrgan.transform : null);
                        if (target != null) cameraInspectionController.ToggleFocus(target);
                    }
                }

                if (GUI.Button(new Rect(rightX + 15, 394, rightW - 30, 26), "Ask AI Tutor About Organ"))
                {
                    if (learningUI != null)
                    {
                        learningUI.isLearningUIOpen = true;
                        learningUI.activeTab = ImmersiveAnatomy.Learning.LearningTab.AITutor;
                        learningUI.NotifyOrganSelected(selectedOrgan);
                    }
                }

                if (GUI.Button(new Rect(rightX + 15, 426, rightW - 30, 24), "Deselect [R]"))
                {
                    ResetAllAnatomy();
                }
            }
            else
            {
                GUI.Label(new Rect(rightX + 15, 105, rightW - 30, 24), "<i>No organ currently selected.</i>");
                GUI.Label(new Rect(rightX + 15, 135, rightW - 30, 45), "Aim the center crosshair at any organ and click to inspect clinical anatomy.");
                GUI.Label(new Rect(rightX + 15, 185, rightW - 30, 45), "Press <b>[X]</b> to trigger the <b>Exploded Anatomy View</b>, smoothly separating organs along physiological axes.");
                GUI.Label(new Rect(rightX + 15, 240, rightW - 30, 45), "Press <b>[Z]</b> for <b>X-Ray Fluoroscopy</b> or <b>[C]</b> to compare two organs side-by-side.");
                GUI.Label(new Rect(rightX + 15, 295, rightW - 30, 45), "Press <b>[F]</b> to focus camera and right-click drag to orbit/inspect closely.");
                GUI.Label(new Rect(rightX + 15, 350, rightW - 30, 45), "Press <b>[L]</b> for the <b>Clinical Quiz, Case Simulation & AI Tutor</b>.");
            }
        }

        // 4. BOTTOM CONTROLS QUICK BAR
        GUI.Box(new Rect(15, Screen.height - 45, Screen.width - 30, 32), "");
        string hint = "<b>Phase 5 Controls:</b> WASD Move | Mouse Look | Click Select | Q/E Rotate | Scroll Zoom | F Focus/Orbit | L AI Tutor & Quiz | X Explode | Z X-Ray | C Compare | I Isolate | 1-6 Systems | R Reset";
        GUI.Label(new Rect(25, Screen.height - 39, Screen.width - 50, 24), hint);
    }
}
