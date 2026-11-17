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

    [Header("Display Settings")]
    public bool showDesktopHUD = true;

    public enum LabArea
    {
        Exploration = 0,
        Quiz = 1,
        ClinicalCases = 2,
        AITutor = 3
    }

    [Header("Active Lab Area")]
    public LabArea currentArea = LabArea.Exploration;

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

    private AudioSource interactionAudioSource;

    private void PlaySelectTone(float frequency = 880f, float duration = 0.12f)
    {
        if (interactionAudioSource == null)
        {
            interactionAudioSource = GetComponent<AudioSource>();
            if (interactionAudioSource == null) interactionAudioSource = gameObject.AddComponent<AudioSource>();
            interactionAudioSource.playOnAwake = false;
        }
        int sampleRate = 44100;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Sin(Mathf.PI * (t / duration));
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.35f;
        }
        AudioClip clip = AudioClip.Create("OrganSelectChime", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        interactionAudioSource.PlayOneShot(clip);
    }

    private void HandleRaycastHover()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (playerCamera == null) return;

        Vector2 mousePos = Input.mousePosition;
        // Check if mouse is hovering over docked UI panels (left, right, top tabs, bottom bar)
        bool overLeftUI = mousePos.x < 380 && mousePos.y > 55 && mousePos.y < Screen.height - 55;
        bool overRightUI = mousePos.x > Screen.width - 380 && mousePos.y > 55 && mousePos.y < Screen.height - 55;
        bool overTopTabs = mousePos.y >= Screen.height - 55;
        bool overBottomBar = mousePos.y <= 55;

        Ray ray;
        // If mouse is within main central 3D viewport, raycast directly from mouse cursor!
        if (!overLeftUI && !overRightUI && !overTopTabs && !overBottomBar &&
            mousePos.x >= 0 && mousePos.x <= Screen.width && mousePos.y >= 0 && mousePos.y <= Screen.height)
        {
            ray = playerCamera.ScreenPointToRay(mousePos);
        }
        else
        {
            // Center crosshair fallback
            ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
        }

        RaycastHit[] hits = Physics.RaycastAll(ray, interactionDistance);
        OrganInteractable foundOrgan = null;
        float bestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit.collider == null || hit.collider.isTrigger) continue;
            OrganInteractable organ = hit.collider.GetComponentInParent<OrganInteractable>();
            if (organ != null)
            {
                // Prefer internal organs over external body silhouette
                bool isBodyShell = organ.organName.IndexOf("Body", System.StringComparison.OrdinalIgnoreCase) >= 0;
                float effectiveDist = hit.distance + (isBodyShell ? 2.5f : 0f);
                if (effectiveDist < bestDist)
                {
                    bestDist = effectiveDist;
                    foundOrgan = organ;
                }
            }
        }

        if (foundOrgan != hoveredOrgan)
        {
            if (hoveredOrgan != null && highlighter != null) highlighter.OnHoverExit(hoveredOrgan);
            hoveredOrgan = foundOrgan;
            if (hoveredOrgan != null && highlighter != null) highlighter.OnHoverEnter(hoveredOrgan);
        }
    }

    private void HandleSelection()
    {
        Vector2 mousePos = Input.mousePosition;
        // Don't select if clicking on docked UI panels
        if (mousePos.x < 380 && mousePos.y > 55 && mousePos.y < Screen.height - 55) return;
        if (mousePos.x > Screen.width - 380 && mousePos.y > 55 && mousePos.y < Screen.height - 55) return;
        if (mousePos.y >= Screen.height - 55 || mousePos.y <= 55) return;

        if (hoveredOrgan != null)
        {
            SelectOrgan(hoveredOrgan);
        }
    }

    public void SelectOrgan(OrganInteractable organ)
    {
        if (organ == null) return;

        selectedOrgan = organ;
        selectedOrgan.ShowInformation();

        PlaySelectTone(880f, 0.15f);

        if (highlighter != null) highlighter.SelectOrgan(selectedOrgan);

        // Forward to Comparison Mode if active
        if (comparisonController != null && comparisonController.IsInComparisonMode)
        {
            comparisonController.AddOrganToComparison(selectedOrgan);
            return;
        }

        // Forward to Quiz Engine if in Quiz or Clinical Cases mode
        if (learningUI != null && learningUI.quizEngine != null)
        {
            if (currentArea == LabArea.Quiz || currentArea == LabArea.ClinicalCases)
            {
                learningUI.quizEngine.Submit3DOrganSelection(selectedOrgan);
            }
        }

        // Forward to AI Tutor if in Teaching mode
        if (learningUI != null)
        {
            learningUI.NotifyOrganSelected(selectedOrgan);
        }
    }

    private void HandleKeyboardShortcuts()
    {
        // Quick Area Switch: L key toggles between Exploration and Quiz
        if (Input.GetKeyDown(KeyCode.L))
        {
            currentArea = (currentArea == LabArea.Quiz) ? LabArea.Exploration : LabArea.Quiz;
            if (currentArea == LabArea.Quiz && learningUI != null && learningUI.quizEngine != null && learningUI.quizEngine.State == ImmersiveAnatomy.Learning.QuizState.Idle)
            {
                learningUI.quizEngine.StartQuizSession(5);
            }
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
        if (!showDesktopHUD) return;

        // 1. TOP HEADER BANNER WITH DEDICATED AREA TABS
        GUI.Box(new Rect(15, 10, Screen.width - 30, 46), "");
        GUI.Label(new Rect(25, 20, 200, 24), "<b>IMMERSIVE ANATOMY LAB</b>");

        // Clean Area Switcher Tabs across Top
        int tabStartX = 230;
        int tabW = 160;
        string[] areaTabs = new string[] { "[1] Systems & View", "[2] Quiz Area", "[3] Clinical Cases", "[4] Dr. Nova AI" };
        for (int i = 0; i < areaTabs.Length; i++)
        {
            bool isCurrent = ((int)currentArea == i);
            string tabLabel = isCurrent ? $"<b><color=#00FFAA>{areaTabs[i]}</color></b>" : areaTabs[i];
            if (GUI.Button(new Rect(tabStartX + i * (tabW + 6), 16, tabW, 32), tabLabel))
            {
                currentArea = (LabArea)i;
                if (currentArea == LabArea.Quiz && learningUI != null && learningUI.quizEngine != null && learningUI.quizEngine.State == ImmersiveAnatomy.Learning.QuizState.Idle)
                {
                    learningUI.quizEngine.StartQuizSession(5);
                }
                else if (currentArea == LabArea.ClinicalCases && learningUI != null && learningUI.quizEngine != null)
                {
                    learningUI.quizEngine.StartClinicalCase(0);
                }
            }
        }

        string modeStatus = "<color=#00E5FF>EXPLORATION</color>";
        if (currentArea == LabArea.Quiz)
            modeStatus = "<color=#00FF88>QUIZ ACTIVE</color>";
        else if (currentArea == LabArea.ClinicalCases)
            modeStatus = "<color=#FFCC00>CLINICAL CASE</color>";
        else if (currentArea == LabArea.AITutor)
            modeStatus = "<color=#00FFFF>DR. NOVA AI</color>";
        else if (cameraInspectionController != null && cameraInspectionController.IsFocusMode)
            modeStatus = "<color=#33FF88>CAMERA FOCUS</color>";
        else if (explodedViewController != null && explodedViewController.IsExploded)
            modeStatus = "<color=#FF9900>EXPLODED VIEW</color>";
        else if (isolationController != null && isolationController.IsInIsolationMode)
            modeStatus = "<color=#FFCC00>ORGAN ISOLATED</color>";
        else if (comparisonController != null && comparisonController.IsInComparisonMode)
            modeStatus = "<color=#CC44FF>ORGAN COMPARISON</color>";
        else if (xrayViewController != null && xrayViewController.IsXRayActive)
            modeStatus = "<color=#00FFFF>X-RAY FLUOROSCOPY</color>";

        GUI.Label(new Rect(Screen.width - 240, 22, 220, 24), $"<b>ACTIVE:</b> {modeStatus}");

        // 2. DEDICATED LEFT PANEL (Cleanly docked on left, keeping center 3D body 100% visible)
        int leftW = 360;
        int leftH = Screen.height - 120;
        Rect leftRect = new Rect(15, 62, leftW, leftH);

        if (currentArea == LabArea.Exploration)
        {
            GUI.Box(leftRect, "<b>ANATOMICAL SYSTEMS & MODES</b>");

            // System Visibility Toggles
            if (BodySystemManager.Instance != null)
            {
                GUI.Label(new Rect(25, 90, leftW - 20, 20), "<b>Body Systems Visibility:</b>");
                string[] sysKeys = new string[] { "Body", "Skeletal", "Cardiovascular", "Respiratory", "Nervous", "Digestive" };
                string[] sysNums = new string[] { "[6]", "[1]", "[2]", "[3]", "[4]", "[5]" };
                for (int i = 0; i < sysKeys.Length; i++)
                {
                    bool isVis = BodySystemManager.Instance.IsSystemVisible(sysKeys[i]);
                    string label = $"{sysNums[i]} {sysKeys[i]}: {(isVis ? "<color=#00FF88>ON</color>" : "<color=#FF4444>OFF</color>")}";
                    if (GUI.Button(new Rect(25, 114 + i * 27, leftW - 20, 23), label))
                    {
                        BodySystemManager.Instance.ToggleSystem(sysKeys[i]);
                    }
                }

                if (GUI.Button(new Rect(25, 114 + 6 * 27, (leftW - 30) / 2, 23), "Show All"))
                {
                    BodySystemManager.Instance.ShowAllSystems();
                }
                if (GUI.Button(new Rect(25 + (leftW - 30) / 2 + 10, 114 + 6 * 27, (leftW - 30) / 2, 23), "Hide All"))
                {
                    BodySystemManager.Instance.HideAllSystems();
                }
            }

            // Advanced View Modes
            int viewY = 320;
            GUI.Label(new Rect(25, viewY, leftW - 20, 20), "<b>Advanced View Modes:</b>");

            bool isExp = explodedViewController != null && explodedViewController.IsExploded;
            string expText = isExp ? "Assemble Anatomy [X]" : "Exploded View [X]";
            if (GUI.Button(new Rect(25, viewY + 24, leftW - 20, 25), expText))
            {
                if (explodedViewController != null) explodedViewController.ToggleExplodedView();
            }

            bool isXRay = xrayViewController != null && xrayViewController.IsXRayActive;
            string xrayText = isXRay ? "Disable X-Ray [Z]" : "X-Ray Mode [Z]";
            if (GUI.Button(new Rect(25, viewY + 52, leftW - 20, 25), xrayText))
            {
                if (xrayViewController != null) xrayViewController.ToggleXRay();
            }

            bool isComp = comparisonController != null && comparisonController.IsInComparisonMode;
            string compText = isComp ? "Exit Comparison [C]" : "Compare Organs [C]";
            if (GUI.Button(new Rect(25, viewY + 80, leftW - 20, 25), compText))
            {
                if (comparisonController != null) comparisonController.ToggleComparisonMode(selectedOrgan);
            }

            if (GUI.Button(new Rect(25, viewY + 108, leftW - 20, 25), "RESET ANATOMY [R]"))
            {
                ResetAllAnatomy();
            }
        }
        else if (currentArea == LabArea.Quiz)
        {
            GUI.Box(leftRect, "<b>CLINICAL ANATOMY QUIZ AREA</b>");
            if (learningUI != null)
            {
                Rect quizInnerRect = new Rect(25, 90, leftW - 20, leftH - 40);
                learningUI.RenderQuizTab(quizInnerRect);
            }
        }
        else if (currentArea == LabArea.ClinicalCases)
        {
            GUI.Box(leftRect, "<b>PATIENT CASE SIMULATION</b>");
            if (learningUI != null)
            {
                Rect caseInnerRect = new Rect(25, 90, leftW - 20, leftH - 40);
                learningUI.RenderClinicalCasesTab(caseInnerRect);
            }
        }
        else if (currentArea == LabArea.AITutor)
        {
            GUI.Box(leftRect, "<b>DR. NOVA (AI CLINICAL TUTOR)</b>");
            if (learningUI != null)
            {
                Rect tutorInnerRect = new Rect(25, 90, leftW - 20, leftH - 40);
                learningUI.RenderAITutorTab(tutorInnerRect);
            }
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

                if (GUI.Button(new Rect(rightX + 15, 394, rightW - 30, 26), "Ask Dr. Nova About Organ"))
                {
                    currentArea = LabArea.AITutor;
                    if (learningUI != null)
                    {
                        learningUI.activeTab = ImmersiveAnatomy.Learning.LearningTab.AITutor;
                        learningUI.NotifyOrganSelected(selectedOrgan);
                    }
                }

                if (GUI.Button(new Rect(rightX + 15, 426, rightW - 30, 24), "Deselect [R]"))
                {
                    ResetAllAnatomy();
                }
            }
            else if (hoveredOrgan != null)
            {
                // LIVE HOVER PREVIEW: Shows organ identity in real-time before clicking!
                GUI.Label(new Rect(rightX + 15, 90, rightW - 30, 22), $"<b>Targeting:</b> <color=#FFCC00>{hoveredOrgan.organName}</color>");
                GUI.Label(new Rect(rightX + 15, 112, rightW - 30, 22), $"<b>System:</b> {hoveredOrgan.bodySystem}  |  <b>Mass:</b> {hoveredOrgan.normalWeight}g");
                GUI.Label(new Rect(rightX + 15, 136, rightW - 30, 48), $"<b>Physiology:</b>\n{hoveredOrgan.description}");
                GUI.Label(new Rect(rightX + 15, 188, rightW - 30, 42), $"<b>Blood Supply:</b> {hoveredOrgan.arterialSupply}");
                GUI.Label(new Rect(rightX + 15, 234, rightW - 30, 42), $"<b>Innervation:</b> {hoveredOrgan.innervation}");
                GUI.Label(new Rect(rightX + 15, 280, rightW - 30, 42), $"<b>Clinical Notes:</b> {hoveredOrgan.commonPathology}");

                GUI.Box(new Rect(rightX + 15, 330, rightW - 30, 32), "");
                GUI.Label(new Rect(rightX + 20, 336, rightW - 40, 22), "<color=#00FF88><b>➔ Left-Click to Select & Lock Organ</b></color>");

                if (GUI.Button(new Rect(rightX + 15, 370, rightW - 30, 26), "Select This Organ"))
                {
                    SelectOrgan(hoveredOrgan);
                }
            }
            else
            {
                GUI.Label(new Rect(rightX + 15, 105, rightW - 30, 24), "<i>No organ currently selected.</i>");
                GUI.Label(new Rect(rightX + 15, 135, rightW - 30, 45), "Aim your cursor at any organ in the 3D model to inspect clinical anatomy.");
                GUI.Label(new Rect(rightX + 15, 185, rightW - 30, 45), "Press <b>[X]</b> to trigger the <b>Exploded Anatomy View</b>, smoothly separating organs along physiological axes.");
                GUI.Label(new Rect(rightX + 15, 240, rightW - 30, 45), "Press <b>[Z]</b> for <b>X-Ray Fluoroscopy</b> or <b>[C]</b> to compare two organs side-by-side.");
                GUI.Label(new Rect(rightX + 15, 295, rightW - 30, 45), "Press <b>[F]</b> to focus camera and right-click drag to orbit/inspect closely.");
                GUI.Label(new Rect(rightX + 15, 350, rightW - 30, 45), "Press <b>[L]</b> for the <b>Clinical Quiz, Case Simulation & AI Tutor</b>.");
            }
        }

        // 4. FLOATING 3D HOVER TOOLTIP OVER HOVERED ORGAN
        if (hoveredOrgan != null && playerCamera != null)
        {
            Vector3 worldPos = hoveredOrgan.transform.position + Vector3.up * 0.16f;
            Vector3 screenPos = playerCamera.WorldToScreenPoint(worldPos);
            if (screenPos.z > 0.1f)
            {
                float tagW = 220f;
                float tagH = 50f;
                float tagX = Mathf.Clamp(screenPos.x - tagW * 0.5f, 390f, Screen.width - 390f - tagW);
                float tagY = Mathf.Clamp(Screen.height - screenPos.y - tagH - 10f, 70f, Screen.height - 90f);

                GUI.Box(new Rect(tagX, tagY, tagW, tagH), "");
                GUI.Label(new Rect(tagX + 5, tagY + 4, tagW - 10, 22), $"<b>{hoveredOrgan.organName.ToUpper()}</b>", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.2f, 1f, 0.9f) } });
                GUI.Label(new Rect(tagX + 5, tagY + 26, tagW - 10, 18), $"<size=10>{hoveredOrgan.bodySystem} System | {hoveredOrgan.normalWeight:F0}g</size>", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } });
            }
        }

        // 5. QUIZ 3D SPOTLIGHT BEACON
        if (currentArea == LabArea.Quiz && learningUI != null && learningUI.quizEngine != null && learningUI.quizEngine.CurrentTargetOrgan != null && playerCamera != null)
        {
            Vector3 worldPos = learningUI.quizEngine.CurrentTargetOrgan.transform.position + Vector3.up * 0.28f;
            Vector3 screenPos = playerCamera.WorldToScreenPoint(worldPos);
            if (screenPos.z > 0.1f)
            {
                float tagW = 240f;
                float tagH = 32f;
                float tagX = Mathf.Clamp(screenPos.x - tagW * 0.5f, 390f, Screen.width - 390f - tagW);
                float tagY = Mathf.Clamp(Screen.height - screenPos.y - tagH - 12f, 65f, Screen.height - 90f);

                GUI.Box(new Rect(tagX, tagY, tagW, tagH), "");
                GUI.Label(new Rect(tagX + 5, tagY + 6, tagW - 10, 20), "<b>★ IDENTIFY THIS ORGAN ★</b>", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, 0.85f, 0.2f) } });
            }
        }

        // 6. BOTTOM CONTROLS QUICK BAR
        GUI.Box(new Rect(15, Screen.height - 45, Screen.width - 30, 32), "");
        string hint = "<b>Phase 5 Controls:</b> WASD Move | Mouse Look | Click Select | Q/E Rotate | Scroll Zoom | F Focus/Orbit | L AI Tutor & Quiz | X Explode | Z X-Ray | C Compare | I Isolate | 1-6 Systems | R Reset";
        GUI.Label(new Rect(25, Screen.height - 39, Screen.width - 50, 24), hint);
    }
}
