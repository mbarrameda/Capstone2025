using UnityEngine;

// SIMPLE PERSPECTIVE TOGGLE
// Press left stick (Perspective button) to toggle between first and third person view
// Works with your existing camera setup - doesn't need a camera pivot

public class SimplePerspectiveToggle : MonoBehaviour
{
    [Header("Camera Reference")]
    public Camera playerCamera;

    [Header("Perspective Settings")]
    public float thirdPersonDistance = 5f;
    public float thirdPersonHeight = 2f;
    public float transitionSpeed = 8f;

    [Header("Input")]
    public string perspectiveButtonName = "Perspective";

    [Header("Wall Collision")]
    public bool avoidWalls = true;
    public LayerMask collisionLayers = ~0;
    public float collisionBuffer = 0.3f;

    private bool isThirdPerson = false;
    private Vector3 firstPersonLocalPosition;
    private Quaternion firstPersonLocalRotation;
    private Vector3 currentCameraOffset;

    void Start()
    {
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        if (playerCamera != null)
        {
            // Store the camera's original local position and rotation
            firstPersonLocalPosition = playerCamera.transform.localPosition;
            firstPersonLocalRotation = playerCamera.transform.localRotation;
            currentCameraOffset = firstPersonLocalPosition;
        }
    }

    void Update()
    {
        // Toggle perspective when button is pressed
        if (Input.GetButtonDown(perspectiveButtonName))
        {
            TogglePerspective();
        }
    }

    void LateUpdate()
    {
        if (playerCamera == null) return;

        if (isThirdPerson)
            UpdateThirdPerson();
        else
            UpdateFirstPerson();
    }

    void TogglePerspective()
    {
        isThirdPerson = !isThirdPerson;

        if (isThirdPerson)
            Debug.Log("📷 Switched to THIRD PERSON");
        else
            Debug.Log("📷 Switched to FIRST PERSON");
    }

    void UpdateFirstPerson()
    {
        // Smoothly transition back to first person position
        currentCameraOffset = Vector3.Lerp(
            currentCameraOffset,
            firstPersonLocalPosition,
            Time.deltaTime * transitionSpeed
        );

        playerCamera.transform.localPosition = currentCameraOffset;

        // Restore first person rotation
        playerCamera.transform.localRotation = Quaternion.Slerp(
            playerCamera.transform.localRotation,
            firstPersonLocalRotation,
            Time.deltaTime * transitionSpeed
        );
    }

    void UpdateThirdPerson()
    {
        // Calculate third person position behind and above the character
        Vector3 desiredOffset = new Vector3(0f, thirdPersonHeight, -thirdPersonDistance);

        // Check for walls if enabled
        if (avoidWalls)
            desiredOffset = CheckWallCollision(desiredOffset);

        // Smoothly move camera to third person position
        currentCameraOffset = Vector3.Lerp(
            currentCameraOffset,
            desiredOffset,
            Time.deltaTime * transitionSpeed
        );

        playerCamera.transform.localPosition = currentCameraOffset;

        // Make camera look at the character
        Vector3 lookTarget = transform.position + Vector3.up * (thirdPersonHeight * 0.5f);
        Vector3 direction = lookTarget - playerCamera.transform.position;

        if (direction.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            playerCamera.transform.rotation = Quaternion.Slerp(
                playerCamera.transform.rotation,
                targetRotation,
                Time.deltaTime * transitionSpeed
            );
        }
    }

    Vector3 CheckWallCollision(Vector3 desiredOffset)
    {
        // Raycast from character to desired camera position
        Vector3 origin = transform.position + Vector3.up * thirdPersonHeight;
        Vector3 targetWorldPos = transform.TransformPoint(desiredOffset);
        Vector3 direction = (targetWorldPos - origin).normalized;
        float distance = Vector3.Distance(origin, targetWorldPos);

        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, distance, collisionLayers))
        {
            // Pull camera closer if there's a wall
            float safeDistance = hit.distance - collisionBuffer;
            safeDistance = Mathf.Max(0.5f, safeDistance);
            return new Vector3(0f, thirdPersonHeight, -safeDistance);
        }

        return desiredOffset;
    }

    // Public method to force first person (useful when dying, teleporting, etc.)
    public void ForceFirstPerson()
    {
        isThirdPerson = false;
        if (playerCamera != null)
        {
            playerCamera.transform.localPosition = firstPersonLocalPosition;
            playerCamera.transform.localRotation = firstPersonLocalRotation;
            currentCameraOffset = firstPersonLocalPosition;
        }
    }

    // Public method to set third person distance at runtime
    public void SetThirdPersonDistance(float distance)
    {
        thirdPersonDistance = distance;
    }

    // Debug visualization
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || !isThirdPerson || playerCamera == null) return;

        // Draw desired camera position
        Gizmos.color = Color.yellow;
        Vector3 desiredPos = transform.TransformPoint(new Vector3(0f, thirdPersonHeight, -thirdPersonDistance));
        Gizmos.DrawWireSphere(desiredPos, 0.2f);

        // Draw actual camera position
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(playerCamera.transform.position, 0.15f);

        // Draw line from character to camera
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position + Vector3.up * thirdPersonHeight, playerCamera.transform.position);
    }
}