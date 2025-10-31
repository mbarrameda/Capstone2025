using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class ObjectMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 2f;
    public float maxRotationAngle = 45f;

    [Header("Ground Detection")]
    public string groundTag = "Ground"; // Set this to whatever tag your floor uses

    [Header("Camera Settings")]
    public Transform cameraTransform;
    public Camera objectCamera;

    [Header("References")]
    public Rigidbody rb;
    public Collider objectCollider;

    private PlayerInputs inputActions;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float xRotation = 0f;

    private bool hasControl = false;
    private bool isGrounded = false;
    private Vector3 spawnPosition;

    private void Awake()
    {
        // Get references if not assigned
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (objectCollider == null) objectCollider = GetComponent<Collider>();

        spawnPosition = transform.position;

        SetupRigidbody();
    }

    private void SetupRigidbody()
    {
        if (rb != null)
        {
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    public void TakeControl(PlayerInputs newInputs)
    {
        ReleaseControl();

        inputActions = newInputs;
        SubscribeInputs();
        inputActions.Enable();

        if (objectCamera != null)
            objectCamera.enabled = true;

        hasControl = true;

        // Reset position and ensure we're above ground
        ResetPosition();
    }

    private void ResetPosition()
    {
        // Position the object safely above ground
        Vector3 safePosition = spawnPosition;
        safePosition.y += 2f; // Start 2 units above spawn position

        transform.position = safePosition;

        // Reset velocity
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void ReleaseControl()
    {
        if (inputActions == null) return;

        UnsubscribeInputs();
        inputActions.Disable();
        inputActions = null;

        if (objectCamera != null)
            objectCamera.enabled = false;

        hasControl = false;
    }

    private void SubscribeInputs()
    {
        if (inputActions == null) return;

        inputActions.Player.Movement.performed += OnMovePerformed;
        inputActions.Player.Movement.canceled += OnMoveCanceled;
        inputActions.Player.Look.performed += OnLookPerformed;
        inputActions.Player.Look.canceled += OnLookCanceled;
    }

    private void UnsubscribeInputs()
    {
        if (inputActions == null) return;

        inputActions.Player.Movement.performed -= OnMovePerformed;
        inputActions.Player.Movement.canceled -= OnMoveCanceled;
        inputActions.Player.Look.performed -= OnLookPerformed;
        inputActions.Player.Look.canceled -= OnLookCanceled;
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();
    private void OnMoveCanceled(InputAction.CallbackContext ctx) => moveInput = Vector2.zero;

    private void OnLookPerformed(InputAction.CallbackContext ctx) => lookInput = ctx.ReadValue<Vector2>();
    private void OnLookCanceled(InputAction.CallbackContext ctx) => lookInput = Vector2.zero;

    private void Update()
    {
        if (!hasControl) return;

        // If we're falling through the world, reset position
        if (transform.position.y < -10f)
        {
            ResetPosition();
        }
    }

    private void FixedUpdate()
    {
        if (!hasControl) return;

        HandleMovement();
        HandleRotation();
    }

    // Check if we're grounded by looking at collisions
    private void OnCollisionStay(Collision collision)
    {
        // Check if we're colliding with the ground
        if (collision.gameObject.CompareTag(groundTag))
        {
            // Check if the contact points are below the center of the object
            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.point.y < transform.position.y - objectCollider.bounds.extents.y + 0.1f)
                {
                    isGrounded = true;
                    return;
                }
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag(groundTag))
        {
            isGrounded = false;
        }
    }

    private void HandleMovement()
    {
        if (moveInput.sqrMagnitude <= 0f) return;

        Vector3 moveDir = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;

        // Only allow movement if grounded
        if (isGrounded)
        {
            Vector3 moveAmount = moveDir * moveSpeed * Time.fixedDeltaTime;

            // Use Rigidbody for movement
            if (rb != null)
            {
                rb.MovePosition(rb.position + moveAmount);
            }
        }
    }

    private void HandleRotation()
    {
        if (lookInput.sqrMagnitude <= 0f) return;

        // Rotate the object left/right
        float yRotation = lookInput.x * rotationSpeed;
        Quaternion newRotation = rb.rotation * Quaternion.Euler(0f, yRotation, 0f);
        rb.MoveRotation(newRotation);

        // Handle camera look up/down (if camera exists)
        if (cameraTransform != null)
        {
            xRotation -= lookInput.y * rotationSpeed;
            xRotation = Mathf.Clamp(xRotation, -maxRotationAngle, maxRotationAngle);
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Debug to see what we're colliding with
        if (collision.gameObject.CompareTag(groundTag))
        {
            Debug.Log($"Object landed on ground: {gameObject.name}");
        }
    }

    private void OnDestroy()
    {
        ReleaseControl();
    }
}