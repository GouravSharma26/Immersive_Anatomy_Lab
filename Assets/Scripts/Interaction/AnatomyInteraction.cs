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
    public bool hideMouseCursor = true;

    [Header("Player Locomotion")]
    public float walkSpeed = 3.0f;
    public float sprintSpeed = 5.5f;
    public float mouseSensitivity = 2.4f;
    private Transform playerRigTransform;
    private float cameraPitch = 0f;
    private Texture2D reticleTexture;

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

        var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin != null)
        {
            playerRigTransform = xrOrigin.transform;
        }
        else if (playerCamera != null && playerCamera.transform.parent != null)
        {
            playerRigTransform = playerCamera.transform.parent;
        }
        else if (playerCamera != null)
        {
            playerRigTransform = playerCamera.transform;
        }

        if (hideMouseCursor)
        {
            SetCursorVisible(false);
        }
    }

    public void SetCursorVisible(bool isVisible)
    {
        Cursor.visible = isVisible;
        Cursor.lockState = isVisible ? CursorLockMode.None : CursorLockMode.Confined;
    }

    private void Update()
    {
        HandlePlayerLocomotion();

        // Toggle mouse cursor visibility with Escape, Tilde (~), or M
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.BackQuote) || Input.GetKeyDown(KeyCode.M))
        {
            SetCursorVisible(!Cursor.visible);
        }

        HandleRaycastHover();

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
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

    private void HandlePlayerLocomotion()
    {
        if (playerCamera == null) return;
        if (cameraInspectionController != null && cameraInspectionController.IsFocusMode) return;

        // 1. WASD Translation
        float h = 0f;
        float v = 0f;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;

        if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
        {
            Vector3 forward = playerCamera.transform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 right = playerCamera.transform.right;
            right.y = 0f;
            right.Normalize();

            Vector3 moveDirection = (forward * v + right * h).normalized;
            float speed = (Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed) * Time.deltaTime;

            if (playerRigTransform != null)
            {
                playerRigTransform.position += moveDirection * speed;
            }
            else
            {
                playerCamera.transform.position += moveDirection * speed;
            }
        }

        // 2. First-Person Mouse Look (when cursor is hidden or right-clicking)
        if (!Cursor.visible || Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            if (Mathf.Abs(mouseX) > 0.001f)
            {
                if (playerRigTransform != null)
                {
                    playerRigTransform.Rotate(Vector3.up, mouseX, Space.World);
                }
                else
                {
                    playerCamera.transform.Rotate(Vector3.up, mouseX, Space.World);
                }
            }

            if (Mathf.Abs(mouseY) > 0.001f)
            {
                cameraPitch -= mouseY;
                cameraPitch = Mathf.Clamp(cameraPitch, -80f, 80f);
                playerCamera.transform.localEulerAngles = new Vector3(cameraPitch, playerCamera.transform.localEulerAngles.y, 0f);
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

        Ray ray;
        // When cursor is hidden (FPS / Reticle Mode), line of sight is ALWAYS the exact center dot!
        if (!Cursor.visible)
        {
            ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
        }
        else
        {
            Vector2 mousePos = Input.mousePosition;
            bool overLeftUI = mousePos.x < 380 && mousePos.y > 55 && mousePos.y < Screen.height - 55;
            bool overRightUI = mousePos.x > Screen.width - 380 && mousePos.y > 55 && mousePos.y < Screen.height - 55;
            bool overTopTabs = mousePos.y >= Screen.height - 55;
            bool overBottomBar = mousePos.y <= 55;

            if (!overLeftUI && !overRightUI && !overTopTabs && !overBottomBar &&
                mousePos.x >= 0 && mousePos.x <= Screen.width && mousePos.y >= 0 && mousePos.y <= Screen.height)
            {
                ray = playerCamera.ScreenPointToRay(mousePos);
            }
            else
            {
                ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
            }
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
        if (playerCamera == null) return;

        Ray ray;
        if (!Cursor.visible)
        {
            ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
        }
        else
        {
            Vector2 mousePos = Input.mousePosition;
            if (mousePos.x < 380 && mousePos.y > 55 && mousePos.y < Screen.height - 55) return;
            if (mousePos.x > Screen.width - 380 && mousePos.y > 55 && mousePos.y < Screen.height - 55) return;
            if (mousePos.y >= Screen.height - 55 || mousePos.y <= 55) return;
            ray = playerCamera.ScreenPointToRay(mousePos);
        }

        // Check if aiming at comparison pedestal trigger
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            var pedTrigger = hit.collider.GetComponentInParent<ComparisonPedestalTrigger>();
            if (pedTrigger != null)
            {
                pedTrigger.OnPedestalClicked();
                return;
            }
        }

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
            // Clear selectedOrgan so subsequent clicks on other organs are not blocked
            selectedOrgan = null;
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

    private void DrawCenterReticle()
    {
        if (playerCamera == null) return;

        float centerX = Screen.width / 2f;
        float centerY = Screen.height / 2f;

        if (reticleTexture == null)
        {
            reticleTexture = new Texture2D(1, 1);
            reticleTexture.SetPixel(0, 0, Color.white);
            reticleTexture.Apply();
        }

        bool hasTarget = hoveredOrgan != null;

        if (hasTarget)
        {
            // TARGET LOCK-ON: Emerald / Neon Cyan Active Crosshair Reticle
            Color activeColor = new Color(0f, 1f, 0.72f, 0.95f);
            GUI.color = activeColor;

            // Center solid pip
            float pipSize = 6f;
            GUI.DrawTexture(new Rect(centerX - pipSize / 2f, centerY - pipSize / 2f, pipSize, pipSize), reticleTexture);

            // 4 Targeting corner brackets
            float bracketDist = 14f;
            float bracketThick = 2f;
            float bracketLen = 8f;

            // Top
            GUI.DrawTexture(new Rect(centerX - bracketLen / 2f, centerY - bracketDist - bracketThick, bracketLen, bracketThick), reticleTexture);
            // Bottom
            GUI.DrawTexture(new Rect(centerX - bracketLen / 2f, centerY + bracketDist, bracketLen, bracketThick), reticleTexture);
            // Left
            GUI.DrawTexture(new Rect(centerX - bracketDist - bracketThick, centerY - bracketLen / 2f, bracketThick, bracketLen), reticleTexture);
            // Right
            GUI.DrawTexture(new Rect(centerX + bracketDist, centerY - bracketLen / 2f, bracketThick, bracketLen), reticleTexture);

            // Reticle Data HUD Tag
            float tagW = 240f;
            GUI.color = Color.white;
            GUI.Label(new Rect(centerX - tagW / 2f, centerY + 18f, tagW, 20f),
                $"<b><color=#00FFAA>{hoveredOrgan.organName.ToUpper()}</color></b>",
                new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 13, fontStyle = FontStyle.Bold });

            GUI.Label(new Rect(centerX - tagW / 2f, centerY + 36f, tagW, 16f),
                $"<size=11><color=#E0E0E0>{hoveredOrgan.bodySystem} System • {hoveredOrgan.normalWeight:F0}g</color></size>",
                new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });

            GUI.Label(new Rect(centerX - tagW / 2f, centerY + 52f, tagW, 16f),
                "<size=10><color=#00E5FF>➔ Left-Click / Press [E] to Select</color></size>",
                new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
        }
        else
        {
            // IDLE RETICLE: Crisp central targeting dot with 4 subtle outer tick pips
            GUI.color = new Color(1f, 1f, 1f, 0.85f);
            float dotSize = 5f;
            GUI.DrawTexture(new Rect(centerX - dotSize / 2f, centerY - dotSize / 2f, dotSize, dotSize), reticleTexture);

            GUI.color = new Color(0.3f, 0.85f, 1f, 0.45f);
            float tickDist = 12f;
            float tickSize = 2f;
            GUI.DrawTexture(new Rect(centerX - tickSize / 2f, centerY - tickDist, tickSize, tickSize), reticleTexture);
            GUI.DrawTexture(new Rect(centerX - tickSize / 2f, centerY + tickDist, tickSize, tickSize), reticleTexture);
            GUI.DrawTexture(new Rect(centerX - tickDist, centerY - tickSize / 2f, tickSize, tickSize), reticleTexture);
            GUI.DrawTexture(new Rect(centerX + tickDist, centerY - tickSize / 2f, tickSize, tickSize), reticleTexture);
        }

        GUI.color = Color.white;
    }

    private void OnGUI()
    {
        // 0. FIRST-PERSON CENTER TARGETING RETICLE (Always drawn where you are looking)
        DrawCenterReticle();

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
            GUI.Box(new Rect(rightX, 62, rightW, 490), "<b>ORGAN COMPARISON MATRIX</b>");

            OrganInteractable a = comparisonController.OrganA;
            OrganInteractable b = comparisonController.OrganB;
            OrganInteractable ext = comparisonController.ExtractedOrgan;

            // Notice if an organ has been extracted to the front
            if (ext != null)
            {
                GUI.Box(new Rect(rightX + 10, 88, rightW - 20, 52), "");
                GUI.Label(new Rect(rightX + 15, 90, rightW - 30, 20), $"<b>Floating in Front:</b> <color=#00FFAA>{ext.organName}</color>");
                if (GUI.Button(new Rect(rightX + 15, 112, (rightW - 35) / 2, 24), "➔ Snap to Pillar A"))
                {
                    comparisonController.SnapOrganToPedestalA(ext);
                }
                if (GUI.Button(new Rect(rightX + 20 + (rightW - 35) / 2, 112, (rightW - 35) / 2, 24), "➔ Snap to Pillar B"))
                {
                    comparisonController.SnapOrganToPedestalB(ext);
                }
            }

            int contentY = (ext != null) ? 146 : 90;

            // Pillar A Display
            if (a == null)
            {
                GUI.Label(new Rect(rightX + 15, contentY, rightW - 30, 36), "<b>Pillar A:</b> <color=#666666>[Empty]</color>\n<i>Click any organ on the body to extract it.</i>");
            }
            else
            {
                GUI.Label(new Rect(rightX + 15, contentY, rightW - 30, 20), $"<b>Pillar A:</b> <color=#00E5FF>{a.organName}</color> ({a.bodySystem})");
                GUI.Label(new Rect(rightX + 15, contentY + 18, rightW - 30, 18), $"Mass: <b>{a.normalWeight}g</b> | Vascular: {a.arterialSupply}");
            }

            GUI.Box(new Rect(rightX + 15, contentY + 40, rightW - 30, 2), "");

            // Pillar B Display
            if (b == null)
            {
                GUI.Label(new Rect(rightX + 15, contentY + 48, rightW - 30, 36), "<b>Pillar B:</b> <color=#666666>[Empty]</color>\n<i>Click a second organ on the body to extract it.</i>");
            }
            else
            {
                GUI.Label(new Rect(rightX + 15, contentY + 48, rightW - 30, 20), $"<b>Pillar B:</b> <color=#FF88FF>{b.organName}</color> ({b.bodySystem})");
                GUI.Label(new Rect(rightX + 15, contentY + 66, rightW - 30, 18), $"Mass: <b>{b.normalWeight}g</b> | Vascular: {b.arterialSupply}");
            }

            // Both organs placed -> Show Comparative Differential
            if (a != null && b != null)
            {
                int diffY = contentY + 90;
                float diff = Mathf.Abs(a.normalWeight - b.normalWeight);
                string heavier = a.normalWeight >= b.normalWeight ? a.organName : b.organName;
                float ratio = (float)Mathf.Max(a.normalWeight, b.normalWeight) / Mathf.Max(1f, Mathf.Min(a.normalWeight, b.normalWeight));

                GUI.Label(new Rect(rightX + 15, diffY, rightW - 30, 20), $"<b>Mass Difference:</b> <color=#FFCC00>{diff:F0}g</color> ({ratio:F1}x - {heavier} is heavier)");
                GUI.Label(new Rect(rightX + 15, diffY + 22, rightW - 30, 42), $"<b>{a.organName} Function:</b>\n<size=11>{a.clinicalFunction}</size>");
                GUI.Label(new Rect(rightX + 15, diffY + 66, rightW - 30, 42), $"<b>{b.organName} Function:</b>\n<size=11>{b.clinicalFunction}</size>");
                GUI.Label(new Rect(rightX + 15, diffY + 110, rightW - 30, 40), $"<b>Pathology Contrast:</b>\n<size=11>{a.commonPathology} vs {b.commonPathology}</size>");
            }

            // Footer Controls
            if (GUI.Button(new Rect(rightX + 15, 415, (rightW - 35) / 2, 26), "⇄ Swap A & B"))
            {
                comparisonController.SwapPedestals();
            }
            if (GUI.Button(new Rect(rightX + 20 + (rightW - 35) / 2, 415, (rightW - 35) / 2, 26), "Clear & Return"))
            {
                comparisonController.ReturnAllOrgansToSockets();
            }

            if (GUI.Button(new Rect(rightX + 15, 448, rightW - 30, 26), "Exit Comparison Mode [C]"))
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
        string cursorStatus = Cursor.visible ? "<color=#FFCC00>Visible</color>" : "<color=#00FFAA>Hidden</color>";
        string hint = $"<b>Controls:</b> WASD Move | Mouse Look | G / Mid-Click Grab | Click Select | Q/E Rotate | F Focus | C Compare | <b>Cursor [{cursorStatus} - Esc/~ to Toggle]</b>";
        GUI.Label(new Rect(25, Screen.height - 39, Screen.width - 50, 24), hint);
    }
}
