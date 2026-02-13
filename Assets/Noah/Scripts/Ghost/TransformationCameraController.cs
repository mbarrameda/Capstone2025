using UnityEngine;

// TRANSFORMATION CAMERA CONTROLLER
// Switches between first-person (ghost form) and third-person (transformed)
// Add this to your Ghost prefab

public class TransformationCameraController : MonoBehaviour
{
    [Header("Camera References")]
    public Camera ghostCamera;
    public Transform cameraPivot; // The object that rotates up/down

    [Header("First Person Settings (Ghost Form)")]
    public Vector3 firstPersonOffset = Vector3.zero;

    [Header("Third Person Settings (Transformed)")]
    public bool useThirdPersonWhenTransformed = true;
    public float thirdPersonDistance = 3f;
    public float thirdPersonHeight = 1.5f;
    public Vector3 thirdPersonTargetOffset = new Vector3(0f, 0.5f, 0f); // Look at this offset from ghost center

    [Header("Camera Transition")]
    public float transitionSpeed = 5f;

    [Header("Third Person Collision")]
    public bool avoidWalls = true;
    public LayerMask collisionLayers = ~0; // Check against everything by default
    public float collisionBuffer = 0.3f;

    private GhostController ghost;
    private Vector3 currentOffset;
    private Vector3 targetOffset;
    private bool isThirdPerson = false;
    private float currentDistance;

    void Start()
    {
        ghost = GetComponent<GhostController>();

        if (ghostCamera == null)
        {
            ghostCamera = GetComponentInChildren<Camera>();
        }

        if (cameraPivot == null && ghostCamera != null)
        {
            cameraPivot = ghostCamera.transform.parent;
        }

        currentOffset = firstPersonOffset;
        targetOffset = firstPersonOffset;
        currentDistance = 0f;
    }

    void LateUpdate()
    {
        if (ghost == null || ghostCamera == null || cameraPivot == null) return;

        // Check if we should be in third person
        bool shouldBeThirdPerson = useThirdPersonWhenTransformed && ghost.isControllingClone;

        // Switch modes if needed
        if (shouldBeThirdPerson != isThirdPerson)
        {
            SwitchCameraMode(shouldBeThirdPerson);
        }

        // Update camera position
        if (isThirdPerson)
        {
            UpdateThirdPersonCamera();
        }
        else
        {
            UpdateFirstPersonCamera();
        }
    }

    void SwitchCameraMode(bool toThirdPerson)
    {
        isThirdPerson = toThirdPerson;

        if (toThirdPerson)
        {
            Debug.Log("📷 Switching to THIRD PERSON camera");
            targetOffset = new Vector3(0f, thirdPersonHeight, -thirdPersonDistance);
        }
        else
        {
            Debug.Log("📷 Switching to FIRST PERSON camera");
            targetOffset = firstPersonOffset;
        }
    }

    void UpdateFirstPersonCamera()
    {
        // Smoothly transition back to first person
        currentOffset = Vector3.Lerp(currentOffset, targetOffset, Time.deltaTime * transitionSpeed);
        currentDistance = 0f;

        // Position camera at first person offset
        if (cameraPivot != null)
        {
            cameraPivot.localPosition = currentOffset;
        }
    }

    void UpdateThirdPersonCamera()
    {
        // Calculate desired third person position
        Vector3 desiredLocalPosition = new Vector3(0f, thirdPersonHeight, -thirdPersonDistance);

        // Check for wall collisions if enabled
        if (avoidWalls)
        {
            desiredLocalPosition = AdjustForWallCollision(desiredLocalPosition);
        }

        // Smoothly move camera
        currentOffset = Vector3.Lerp(currentOffset, desiredLocalPosition, Time.deltaTime * transitionSpeed);

        if (cameraPivot != null)
        {
            // Set local position
            cameraPivot.localPosition = currentOffset;

            // Make camera look at the target point (slightly above ghost center)
            Vector3 lookTarget = transform.position + transform.TransformDirection(thirdPersonTargetOffset);
            Vector3 cameraWorldPos = cameraPivot.position;

            // Calculate look direction
            Vector3 lookDirection = (lookTarget - cameraWorldPos).normalized;

            // Only override the camera's local rotation, not the pivot's rotation
            // The pivot still rotates with mouse look, but camera looks at character
            if (ghostCamera != null)
            {
                ghostCamera.transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }
    }

    Vector3 AdjustForWallCollision(Vector3 desiredPosition)
    {
        // Cast ray from character to desired camera position
        Vector3 origin = transform.position + transform.TransformDirection(new Vector3(0f, thirdPersonHeight, 0f));
        Vector3 worldDesiredPos = transform.TransformPoint(desiredPosition);
        Vector3 direction = (worldDesiredPos - origin).normalized;
        float distance = Vector3.Distance(origin, worldDesiredPos);

        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, distance, collisionLayers))
        {
            // Hit a wall - move camera closer
            float safeDistance = hit.distance - collisionBuffer;
            safeDistance = Mathf.Max(0.5f, safeDistance); // Minimum distance

            return new Vector3(0f, thirdPersonHeight, -safeDistance);
        }

        return desiredPosition;
    }

    // Call this when you want to force first person (e.g., when exiting transformation)
    public void ForceFirstPerson()
    {
        isThirdPerson = false;
        targetOffset = firstPersonOffset;
        currentOffset = firstPersonOffset;

        if (cameraPivot != null)
        {
            cameraPivot.localPosition = firstPersonOffset;
        }

        // Reset camera rotation to follow pivot
        if (ghostCamera != null && cameraPivot != null)
        {
            ghostCamera.transform.localRotation = Quaternion.identity;
        }
    }

    // Public method to adjust third person distance at runtime
    public void SetThirdPersonDistance(float distance)
    {
        thirdPersonDistance = distance;
        if (isThirdPerson)
        {
            targetOffset = new Vector3(0f, thirdPersonHeight, -distance);
        }
    }

    // Debug visualization
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || !isThirdPerson) return;

        // Draw desired camera position
        Gizmos.color = Color.yellow;
        Vector3 desiredPos = transform.TransformPoint(new Vector3(0f, thirdPersonHeight, -thirdPersonDistance));
        Gizmos.DrawWireSphere(desiredPos, 0.2f);

        // Draw actual camera position
        if (ghostCamera != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(ghostCamera.transform.position, 0.15f);
        }

        // Draw look target
        Gizmos.color = Color.red;
        Vector3 lookTarget = transform.position + transform.TransformDirection(thirdPersonTargetOffset);
        Gizmos.DrawWireSphere(lookTarget, 0.1f);

        // Draw line from camera to look target
        if (ghostCamera != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(ghostCamera.transform.position, lookTarget);
        }
    }
}