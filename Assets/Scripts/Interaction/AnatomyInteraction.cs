using UnityEngine;

public class AnatomyInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public Camera playerCamera;
    public float interactionDistance = 15f;

    [Header("Controllers")]
    public OrganHighlighter highlighter;
    public OrganIsolationController isolationController;

    private OrganInteractable hoveredOrgan;
    private OrganInteractable selectedOrgan;

    private void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (highlighter == null) highlighter = GetComponent<OrganHighlighter>();
        if (isolationController == null) isolationController = GetComponent<OrganIsolationController>();
    }

    private void Update()
    {
        HandleRaycastHover();

        if (Input.GetMouseButtonDown(0))
        {
            HandleSelection();
        }

        HandleKeyboardShortcuts();

        // Organ manipulation
        if (isolationController != null && isolationController.IsInIsolationMode)
        {
            float rotSpeed = 75f * Time.deltaTime;
            if (Input.GetKey(KeyCode.Q)) isolationController.RotateIsolatedOrgan(-rotSpeed, 0);
            if (Input.GetKey(KeyCode.E)) isolationController.RotateIsolatedOrgan(rotSpeed, 0);
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            isolationController.ZoomIsolatedOrgan(scroll);
        }
        else if (selectedOrgan != null)
        {
            RotateSelectedOrgan();
            ZoomSelectedOrgan();
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
        // Don't select if mouse is over GUI buttons on edges
        Vector2 mousePos = Input.mousePosition;
        if (mousePos.x < 240 && mousePos.y > Screen.height - 350) return; // System toggle area
        if (mousePos.x > Screen.width - 360 && mousePos.y > Screen.height - 480) return; // Inspector area

        if (hoveredOrgan != null)
        {
            selectedOrgan = hoveredOrgan;
            selectedOrgan.ShowInformation();
            if (highlighter != null) highlighter.SelectOrgan(selectedOrgan);
        }
    }

    private void HandleKeyboardShortcuts()
    {
        // Body System visibility toggles: 1-6
        if (BodySystemManager.Instance != null)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) BodySystemManager.Instance.ToggleSystem("Skeletal");
            if (Input.GetKeyDown(KeyCode.Alpha2)) BodySystemManager.Instance.ToggleSystem("Cardiovascular");
            if (Input.GetKeyDown(KeyCode.Alpha3)) BodySystemManager.Instance.ToggleSystem("Respiratory");
            if (Input.GetKeyDown(KeyCode.Alpha4)) BodySystemManager.Instance.ToggleSystem("Nervous");
            if (Input.GetKeyDown(KeyCode.Alpha5)) BodySystemManager.Instance.ToggleSystem("Digestive");
            if (Input.GetKeyDown(KeyCode.Alpha6)) BodySystemManager.Instance.ToggleSystem("Body");
        }

        // Isolation mode: I
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleIsolationMode();
        }

        // Deselect / Reset: R
        if (Input.GetKeyDown(KeyCode.R))
        {
            DeselectOrgan();
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

    public void DeselectOrgan()
    {
        if (isolationController != null && isolationController.IsInIsolationMode)
        {
            isolationController.ExitIsolation();
        }
        selectedOrgan = null;
        if (highlighter != null) highlighter.ClearSelection();
    }

    private void RotateSelectedOrgan()
    {
        float rotation = 65f * Time.deltaTime;
        if (Input.GetKey(KeyCode.Q)) selectedOrgan.transform.Rotate(Vector3.up, -rotation, Space.World);
        if (Input.GetKey(KeyCode.E)) selectedOrgan.transform.Rotate(Vector3.up, rotation, Space.World);
    }

    private void ZoomSelectedOrgan()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            Vector3 scale = selectedOrgan.transform.localScale + Vector3.one * scroll * 0.4f;
            float newScale = Mathf.Clamp(scale.x, 0.2f, 3f);
            selectedOrgan.transform.localScale = Vector3.one * newScale;
        }
    }

    private void OnGUI()
    {
        // 1. TOP HEADER BANNER
        GUI.Box(new Rect(15, 12, Screen.width - 30, 36), "");
        GUI.Label(new Rect(25, 20, 500, 24), "<b>IMMERSIVE ANATOMY LAB</b>  |  Phase 2 Clinical 3D Exploration System");
        
        string modeStatus = (isolationController != null && isolationController.IsInIsolationMode) 
            ? "<color=#FFCC00>MODE: ORGAN ISOLATION & HIGH RESOLUTION INSPECTION</color>" 
            : "<color=#00E5FF>MODE: ANATOMICAL EXPLORATION & SYSTEM NAVIGATION</color>";
        GUI.Label(new Rect(Screen.width - 480, 20, 460, 24), modeStatus);

        // 2. LEFT PANEL: BODY SYSTEM VISIBILITY MANAGER
        GUI.Box(new Rect(15, 58, 220, 260), "<b>ANATOMICAL SYSTEMS</b>");
        if (BodySystemManager.Instance != null)
        {
            string[] sysKeys = new string[] { "Body", "Skeletal", "Cardiovascular", "Respiratory", "Nervous", "Digestive" };
            string[] sysNums = new string[] { "[6]", "[1]", "[2]", "[3]", "[4]", "[5]" };
            for (int i = 0; i < sysKeys.Length; i++)
            {
                bool isVis = BodySystemManager.Instance.IsSystemVisible(sysKeys[i]);
                string label = $"{sysNums[i]} {sysKeys[i]}: {(isVis ? "<color=#00FF88>VISIBLE</color>" : "<color=#FF4444>HIDDEN</color>")}";
                if (GUI.Button(new Rect(25, 88 + i * 28, 200, 24), label))
                {
                    BodySystemManager.Instance.ToggleSystem(sysKeys[i]);
                }
            }

            if (GUI.Button(new Rect(25, 260, 95, 22), "Show All")) BodySystemManager.Instance.ShowAllSystems();
            if (GUI.Button(new Rect(130, 260, 95, 22), "Hide All")) BodySystemManager.Instance.HideAllSystems();
        }

        // 3. RIGHT PANEL: CLINICAL ORGAN INSPECTOR
        int rightW = 340;
        int rightX = Screen.width - rightW - 15;
        GUI.Box(new Rect(rightX, 58, rightW, 360), "<b>CLINICAL ORGAN INSPECTOR</b>");

        if (selectedOrgan != null)
        {
            GUI.Label(new Rect(rightX + 15, 88, rightW - 30, 22), $"<b>Organ:</b> <color=#00E5FF>{selectedOrgan.organName}</color>");
            GUI.Label(new Rect(rightX + 15, 110, rightW - 30, 22), $"<b>System:</b> {selectedOrgan.bodySystem}  |  <b>Mass:</b> {selectedOrgan.normalWeight}g");
            
            GUI.Label(new Rect(rightX + 15, 134, rightW - 30, 48), $"<b>Physiology:</b>\n{selectedOrgan.description}");
            GUI.Label(new Rect(rightX + 15, 186, rightW - 30, 42), $"<b>Blood Supply:</b> {selectedOrgan.arterialSupply}");
            GUI.Label(new Rect(rightX + 15, 232, rightW - 30, 42), $"<b>Innervation:</b> {selectedOrgan.innervation}");
            GUI.Label(new Rect(rightX + 15, 278, rightW - 30, 42), $"<b>Clinical Notes:</b> {selectedOrgan.commonPathology}");

            // Action Buttons
            bool inIso = isolationController != null && isolationController.IsInIsolationMode;
            string isoText = inIso ? "Exit Isolation [I]" : "Isolate Organ [I]";
            if (GUI.Button(new Rect(rightX + 15, 330, 150, 26), isoText))
            {
                ToggleIsolationMode();
            }

            if (GUI.Button(new Rect(rightX + 175, 330, 150, 26), "Deselect [R]"))
            {
                DeselectOrgan();
            }
        }
        else
        {
            GUI.Label(new Rect(rightX + 15, 100, rightW - 30, 24), "<i>No organ currently selected.</i>");
            GUI.Label(new Rect(rightX + 15, 130, rightW - 30, 45), "Aim the center crosshair at any organ (Heart, Brain, Lungs, Liver, Stomach) and click to inspect.");
            GUI.Label(new Rect(rightX + 15, 180, rightW - 30, 45), "Use keys <b>[1]-[6]</b> to hide or reveal individual anatomical systems.");
        }

        // 4. BOTTOM CONTROLS QUICK BAR
        GUI.Box(new Rect(15, Screen.height - 45, Screen.width - 30, 32), "");
        string hint = "<b>Controls:</b> WASD Move | Mouse Look | Left Click Select | Q/E Rotate Organ | Scroll Zoom | I Isolate Organ | 1-6 Systems | Esc Cursor";
        GUI.Label(new Rect(25, Screen.height - 39, Screen.width - 50, 24), hint);
    }
}
