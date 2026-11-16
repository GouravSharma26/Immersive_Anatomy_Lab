using System.Collections;
using UnityEngine;

public class CameraInspectionController : MonoBehaviour
{
    [Header("References")]
    public Camera targetCamera;
    public DesktopPlayerController playerController;

    [Header("Focus Settings")]
    public float focusDistance = 1.2f;
    public float transitionSpeed = 5f;
    public float orbitSensitivity = 3f;
    public float zoomSensitivity = 1.5f;
    public float minDistance = 0.4f;
    public float maxDistance = 3.5f;

    private bool isFocusMode = false;
    public bool IsFocusMode => isFocusMode;

    private Transform currentTarget;
    private Vector3 targetFocusPoint;
    private float currentDistance;
    private float orbitYaw = 0f;
    private float orbitPitch = 15f;

    private Vector3 savedCamLocalPos;
    private Quaternion savedCamLocalRot;
    private Transform camParent;

    private void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (playerController == null) playerController = FindFirstObjectByType<DesktopPlayerController>();
        if (targetCamera != null)
        {
            camParent = targetCamera.transform.parent;
            savedCamLocalPos = targetCamera.transform.localPosition;
            savedCamLocalRot = targetCamera.transform.localRotation;
        }
    }

    public void FocusOnTarget(Transform target)
    {
        if (target == null || targetCamera == null) return;

        currentTarget = target;
        targetFocusPoint = target.position;
        currentDistance = focusDistance;

        // Calculate initial orbit angles relative to target
        Vector3 dir = (targetCamera.transform.position - targetFocusPoint).normalized;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion rot = Quaternion.LookRotation(-dir, Vector3.up);
            orbitYaw = rot.eulerAngles.y;
            orbitPitch = rot.eulerAngles.x;
            if (orbitPitch > 180f) orbitPitch -= 360f;
            orbitPitch = Mathf.Clamp(orbitPitch, -60f, 75f);
        }

        isFocusMode = true;
        if (playerController != null) playerController.enabled = false;
    }

    public void ExitFocusMode()
    {
        if (!isFocusMode) return;
        isFocusMode = false;
        currentTarget = null;

        StartCoroutine(RestoreCameraTransform());
    }

    public void ToggleFocus(Transform target)
    {
        if (isFocusMode)
        {
            ExitFocusMode();
        }
        else if (target != null)
        {
            FocusOnTarget(target);
        }
    }

    private void Update()
    {
        if (!isFocusMode) return;

        // Exit focus on WASD movement or Space
        if (Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f || Input.GetKeyDown(KeyCode.Space))
        {
            ExitFocusMode();
            return;
        }

        // Hotkey F to exit focus
        if (Input.GetKeyDown(KeyCode.F))
        {
            ExitFocusMode();
            return;
        }

        if (currentTarget != null)
        {
            targetFocusPoint = currentTarget.position;
        }

        // Orbit via Right Mouse Button or Left Mouse Button when cursor not over UI
        if (Input.GetMouseButton(1) || (Input.GetMouseButton(0) && Cursor.lockState == CursorLockMode.Locked))
        {
            orbitYaw += Input.GetAxis("Mouse X") * orbitSensitivity;
            orbitPitch -= Input.GetAxis("Mouse Y") * orbitSensitivity;
            orbitPitch = Mathf.Clamp(orbitPitch, -75f, 75f);
        }

        // Zoom via scroll wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentDistance -= scroll * zoomSensitivity;
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
        }

        // Compute desired camera position
        Quaternion rotation = Quaternion.Euler(orbitPitch, orbitYaw, 0f);
        Vector3 desiredPosition = targetFocusPoint + rotation * new Vector3(0, 0, -currentDistance);

        targetCamera.transform.position = Vector3.Lerp(targetCamera.transform.position, desiredPosition, Time.deltaTime * transitionSpeed);
        targetCamera.transform.rotation = Quaternion.Slerp(targetCamera.transform.rotation, Quaternion.LookRotation(targetFocusPoint - targetCamera.transform.position, Vector3.up), Time.deltaTime * transitionSpeed);
    }

    private IEnumerator RestoreCameraTransform()
    {
        if (targetCamera == null) yield break;

        float elapsed = 0f;
        float duration = 0.35f;
        Vector3 startWorldPos = targetCamera.transform.position;
        Quaternion startWorldRot = targetCamera.transform.rotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            Vector3 targetWorldPos = camParent != null ? camParent.TransformPoint(savedCamLocalPos) : savedCamLocalPos;
            Quaternion targetWorldRot = camParent != null ? camParent.rotation * savedCamLocalRot : savedCamLocalRot;

            targetCamera.transform.position = Vector3.Lerp(startWorldPos, targetWorldPos, t);
            targetCamera.transform.rotation = Quaternion.Slerp(startWorldRot, targetWorldRot, t);
            yield return null;
        }

        if (camParent != null)
        {
            targetCamera.transform.localPosition = savedCamLocalPos;
            targetCamera.transform.localRotation = savedCamLocalRot;
        }

        if (playerController != null) playerController.enabled = true;
    }
}
