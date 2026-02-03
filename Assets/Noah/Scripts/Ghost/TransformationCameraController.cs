using UnityEngine;

// TRANSFORMATION CAMERA CONTROLLER
// Switches between first-person (ghost form) and third-person (transformed)

public class TransformationCameraController : MonoBehaviour
{
    [Header("Camera References")]
    public Camera ghostCamera;
    public Transform cameraPivot; // The object that rotates up/down (pitch)

    [Header("First Person Settings (Ghost Form)")]
    public Vector3 firstPersonOffset = Vector3.zero;

    [Header("Third Person Settings (Transformed)")]
    public bool useThirdPersonWhenTransformed = true;
    public float thirdPersonDistance = 3f;
    public float thirdPersonHeight = 1.5f;

    [Header("Camera Transition")]
    public float transitionSpeed = 5f;

    [Header("Third Person Collision")]
    public bool avoidWalls = true;
    public LayerMask collisionLayers = ~0;
    public float collisionBuffer = 0.3f;

    private GhostController ghost;
    private Vector3 currentOffset;
    private Vector3 targetOffset;
    private bool isThirdPerson = false;

    void Start()
    {
        ghost = GetComponent<GhostController>();

        if (ghostCamera == null)
            ghostCamera = GetComponentInChildren<Camera>();

        if (cameraPivot == null && ghostCamera != null)
            cameraPivot = ghostCamera.transform.parent;

        currentOffset = firstPersonOffset;
        targetOffset = firstPersonOffset;
    }

    void LateUpdate()
    {
        if (ghost == null || ghostCamera == null || cameraPivot == null) return;

        bool shouldBeThirdPerson = useThirdPersonWhenTransformed && ghost.isControllingClone;

        if (shouldBeThirdPerson != isThirdPerson)
            SwitchCameraMode(shouldBeThirdPerson);

        if (isThirdPerson)
            UpdateThirdPersonCamera();
        else
            UpdateFirstPersonCamera();
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

            // Reset camera local rotation when returning to first person so it
            // doesn't carry over any stale orientation.
            ghostCamera.transform.localRotation = Quaternion.identity;
        }
    }

    void UpdateFirstPersonCamera()
    {
        currentOffset = Vector3.Lerp(currentOffset, targetOffset, Time.deltaTime * transitionSpeed);

        if (cameraPivot != null)
            cameraPivot.localPosition = currentOffset;

        // First person: camera local rotation is identity; it inherits
        // the pivot's pitch and the ghost's yaw naturally.
        ghostCamera.transform.localRotation = Quaternion.identity;
    }

    void UpdateThirdPersonCamera()
    {
        Vector3 desiredLocalPosition = new Vector3(0f, thirdPersonHeight, -thirdPersonDistance);

        if (avoidWalls)
            desiredLocalPosition = AdjustForWallCollision(desiredLocalPosition);

        // Smoothly lerp the pivot's local position toward the desired offset.
        currentOffset = Vector3.Lerp(currentOffset, desiredLocalPosition, Time.deltaTime * transitionSpeed);
        targetOffset = desiredLocalPosition;

        if (cameraPivot != null)
            cameraPivot.localPosition = currentOffset;

        // KEY FIX: Do NOT set ghostCamera.transform.rotation here.
        // The camera sits as a child of cameraPivot.  Its local rotation stays
        // at identity so it inherits:
        //   - Yaw  from the Ghost GameObject (driven by mouse-look X in GhostController)
        //   - Pitch from cameraPivot          (driven by mouse-look Y in GhostController)
        // This gives a proper orbit that follows where the player is looking,
        // regardless of the possessed object's own local axes.
        ghostCamera.transform.localRotation = Quaternion.identity;
    }

    Vector3 AdjustForWallCollision(Vector3 desiredPosition)
    {
        // Ray from character (at pivot height) to where the camera wants to be.
        Vector3 origin = transform.position + Vector3.up * thirdPersonHeight;
        Vector3 worldDesiredPos = transform.TransformPoint(desiredPosition);
        Vector3 direction = (worldDesiredPos - origin).normalized;
        float distance = Vector3.Distance(origin, worldDesiredPos);

        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, distance, collisionLayers))
        {
            float safeDistance = hit.distance - collisionBuffer;
            safeDistance = Mathf.Max(0.5f, safeDistance);
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
            cameraPivot.localPosition = firstPersonOffset;

        if (ghostCamera != null)
            ghostCamera.transform.localRotation = Quaternion.identity;
    }

    public void SetThirdPersonDistance(float distance)
    {
        thirdPersonDistance = distance;
        if (isThirdPerson)
            targetOffset = new Vector3(0f, thirdPersonHeight, -distance);
    }

    // Debug visualization
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || !isThirdPerson) return;

        Gizmos.color = Color.yellow;
        Vector3 desiredPos = transform.TransformPoint(new Vector3(0f, thirdPersonHeight, -thirdPersonDistance));
        Gizmos.DrawWireSphere(desiredPos, 0.2f);

        if (ghostCamera != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(ghostCamera.transform.position, 0.15f);
        }

        // Draw line from ghost to camera so you can eyeball the orbit radius
        if (ghostCamera != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, ghostCamera.transform.position);
        }
    }
}