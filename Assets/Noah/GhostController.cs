using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class GhostController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float flySpeed = 3f;
    public float lookSensitivity = 2f;

    [Header("Phasing & Fear")]
    public float fear = 100f;
    public float fearDrainRate = 20f;
    public LayerMask phaseableWalls; // walls that ghost can pass through
    public LayerMask solidWalls;     // walls that block ghost

    public Transform cameraTransform;

    private PlayerInputs inputActions;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float verticalInput = 0f;
    private float xRotation = 0f;

    private Rigidbody rb;
    private Renderer ghostRenderer;
    private bool isPhasing = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ghostRenderer = GetComponentInChildren<Renderer>();

        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    public void AssignInput(PlayerInputs actions)
    {
        inputActions = actions;

        if (inputActions == null)
        {
            Debug.LogError("Assigned PlayerInputs is null!");
            return;
        }

        inputActions.Enable();

        // Movement
        inputActions.Player.Movement.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Movement.canceled += ctx => moveInput = Vector2.zero;

        // Look
        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;

        // Phase toggle (X button)
        inputActions.Player.PhaseToggle.performed += ctx => TogglePhase();

        // Fly up/down
        inputActions.Player.FlyUp.performed += ctx => verticalInput = ctx.ReadValue<float>();
        inputActions.Player.FlyUp.canceled += ctx => verticalInput = 0f;

        inputActions.Player.FlyDown.performed += ctx => verticalInput = -ctx.ReadValue<float>();
        inputActions.Player.FlyDown.canceled += ctx => verticalInput = 0f;
    }

    private void Update()
    {
        HandleLook();
        HandleFear();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleLook()
    {
        transform.Rotate(Vector3.up * lookInput.x * lookSensitivity);

        xRotation -= lookInput.y * lookSensitivity;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);
        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    private void HandleMovement()
    {
        Vector3 horizontalMove = transform.forward * moveInput.y + transform.right * moveInput.x;
        Vector3 verticalMove = Vector3.up * verticalInput * flySpeed;
        Vector3 desiredMove = horizontalMove * moveSpeed + verticalMove;

        if (!isPhasing)
        {
            // Check collisions with solid walls
            Vector3 moveDir = desiredMove.normalized;
            float moveDistance = desiredMove.magnitude * Time.fixedDeltaTime;

            if (Physics.Raycast(rb.position, moveDir, out RaycastHit hit, moveDistance, solidWalls))
            {
                // Prevent moving into solid wall
                desiredMove = Vector3.zero;
            }
        }

        rb.velocity = desiredMove;
    }

    private void TogglePhase()
    {
        if (fear <= 0f) return;

        isPhasing = !isPhasing;

        // Visual feedback
        if (ghostRenderer != null)
        {
            Color c = ghostRenderer.material.color;
            c.a = isPhasing ? 0.5f : 1f;
            ghostRenderer.material.color = c;
        }
    }

    private void HandleFear()
    {
        if (isPhasing && fear > 0f)
        {
            fear -= fearDrainRate * Time.deltaTime;
            fear = Mathf.Max(fear, 0f);

            if (fear <= 0f && isPhasing)
                TogglePhase(); // auto-disable phasing if out of fear
        }
    }

    public void SetVerticalInput(float value) => verticalInput = value;
}
