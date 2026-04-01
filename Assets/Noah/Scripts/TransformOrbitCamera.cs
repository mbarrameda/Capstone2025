using UnityEngine;

/// <summary>
/// Attached to the ghost at runtime when it transforms into an object.
/// Takes over the right stick input to orbit the clone camera around the ghost.
/// Removed automatically when the ghost untransforms.
/// </summary>
public class TransformOrbitCamera : MonoBehaviour
{
    [Header("Orbit Settings")]
    public float orbitSensitivity = 1f;
    public float minVerticalAngle = -60f;
    public float maxVerticalAngle = 75f;
    public float bodyRotateSpeed = 45f;

    [Header("Zoom Settings")]
    public float zoomSpeed = 3f;
    public float minDistance = 1f;
    public float maxDistance = 8f;

    private Camera orbitCamera;
    private GhostController ghost;
    private float currentYaw = 0f;
    private float currentPitch = 20f;  // start slightly above the object
    private float currentDistance = 4f;


    public void Initialise(Camera cam, GhostController ghostController)
    {
        orbitCamera = cam;
        ghost = ghostController;

        // Derive starting yaw/pitch/distance from wherever the camera
        // already is so there's no jump on first frame
        if (orbitCamera != null)
        {
            Vector3 offset = orbitCamera.transform.position - transform.position;
            currentDistance = offset.magnitude;
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

            if (offset != Vector3.zero)
            {
                currentYaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
                currentPitch = Mathf.Asin(offset.y / currentDistance) * Mathf.Rad2Deg;
                currentPitch = Mathf.Clamp(currentPitch, minVerticalAngle, maxVerticalAngle);
            }
        }
    }

    private void Update()
    {
        if (orbitCamera == null || ghost == null || ghost.playerInputs == null) return;

        HandleOrbit();
        HandleBodyRotation();
        HandleZoom();
        ApplyCameraTransform();
    }

    private void HandleOrbit()
    {
        if (ghost.playerInputs == null) return;

        // Right stick always freely orbits the camera
        Vector2 look = ghost.playerInputs.Ghost.Look.ReadValue<Vector2>();

        currentYaw += look.x * orbitSensitivity;
        currentPitch += look.y * orbitSensitivity;
        currentPitch = Mathf.Clamp(currentPitch, minVerticalAngle, maxVerticalAngle);
    }

    private void HandleBodyRotation()
    {
        if (ghost.playerInputs == null) return;

        // Bumpers slowly rotate the ghost's actual body while held
        bool rotateLeft = ghost.playerInputs.Ghost.RotateLeft.ReadValue<float>() > 0.1f;
        bool rotateRight = ghost.playerInputs.Ghost.RotateRight.ReadValue<float>() > 0.1f;

        if (!rotateLeft && !rotateRight) return;

        float direction = rotateLeft ? -1f : 1f;
        ghost.transform.Rotate(0f, direction * bodyRotateSpeed * Time.deltaTime, 0f);
    }

    private void HandleZoom()
    {
        if (ghost.playerInputs == null) return;

        bool zoomIn = ghost.playerInputs.Ghost.ZoomIn.ReadValue<float>() > 0.1f;
        bool zoomOut = ghost.playerInputs.Ghost.ZoomOut.ReadValue<float>() > 0.1f;

        if (zoomIn) currentDistance -= zoomSpeed * Time.deltaTime;
        if (zoomOut) currentDistance += zoomSpeed * Time.deltaTime;

        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
    }

    private void ApplyCameraTransform()
    {
        // Convert yaw + pitch to a world-space offset from the ghost
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 offset = rotation * Vector3.forward * currentDistance;

        orbitCamera.transform.position = transform.position + offset;
        orbitCamera.transform.LookAt(transform.position);
    }
}