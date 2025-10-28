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
    public float phaseCost = 25f;
    public float phaseDuration = 10f;   // seconds
    private float phaseTimer = 0f;

    [Header("Layers")]
    public string defaultLayerName = "Default";
    public string ghostLayerName = "Ghost";
    public string phaseableWallLayerName = "PhaseableWall";

    [Header("References")]
    public Transform cameraTransform;
    public Camera ghostCamera;
    public Renderer ghostRenderer;

    private bool isStunned = false;
    private float stunTimer = 0f;

    // input & physics
    public PlayerInputs inputActions;
    public Rigidbody rb;

    // runtime
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float verticalInput;
    private float xRotation;
    private bool isPhasing = false;
    private bool freezeInput = false;

    private int defaultLayer;
    private int ghostLayer;
    private int phaseableWallLayer;

    private Quaternion targetRotation;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ghostRenderer = ghostRenderer ?? GetComponentInChildren<Renderer>();
        targetRotation = transform.rotation;

        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        defaultLayer = LayerMask.NameToLayer(defaultLayerName);
        ghostLayer = LayerMask.NameToLayer(ghostLayerName);
        phaseableWallLayer = LayerMask.NameToLayer(phaseableWallLayerName);

        gameObject.layer = defaultLayer;
    }

    public void AssignInput(PlayerInputs actions)
    {
        inputActions = actions;
        SubscribeInputs();
        inputActions.Enable();
    }

    public void RemoveInput()
    {
        if (inputActions == null) return;
        UnsubscribeInputs();
        inputActions.Disable();
        inputActions = null;
    }

    private void SubscribeInputs()
    {
        if (inputActions == null) return;

        inputActions.Player.Movement.performed += OnMovePerformed;
        inputActions.Player.Movement.canceled += OnMoveCanceled;

        inputActions.Player.Look.performed += OnLookPerformed;
        inputActions.Player.Look.canceled += OnLookCanceled;

        inputActions.Player.PhaseToggle.performed += OnPhaseToggle;

        inputActions.Player.FlyUp.performed += OnFlyUp;
        inputActions.Player.FlyUp.canceled += OnFlyUpCanceled;

        inputActions.Player.FlyDown.performed += OnFlyDown;
        inputActions.Player.FlyDown.canceled += OnFlyDownCanceled;
    }

    private void UnsubscribeInputs()
    {
        if (inputActions == null) return;

        inputActions.Player.Movement.performed -= OnMovePerformed;
        inputActions.Player.Movement.canceled -= OnMoveCanceled;

        inputActions.Player.Look.performed -= OnLookPerformed;
        inputActions.Player.Look.canceled -= OnLookCanceled;

        inputActions.Player.PhaseToggle.performed -= OnPhaseToggle;

        inputActions.Player.FlyUp.performed -= OnFlyUp;
        inputActions.Player.FlyUp.canceled -= OnFlyUpCanceled;

        inputActions.Player.FlyDown.performed -= OnFlyDown;
        inputActions.Player.FlyDown.canceled -= OnFlyDownCanceled;
    }

    // Named input methods
    private void OnMovePerformed(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();
    private void OnMoveCanceled(InputAction.CallbackContext ctx) => moveInput = Vector2.zero;

    private void OnLookPerformed(InputAction.CallbackContext ctx) => lookInput = ctx.ReadValue<Vector2>();
    private void OnLookCanceled(InputAction.CallbackContext ctx) => lookInput = Vector2.zero;

    private void OnFlyUp(InputAction.CallbackContext ctx) => verticalInput = ctx.ReadValue<float>();
    private void OnFlyUpCanceled(InputAction.CallbackContext ctx) => verticalInput = 0f;

    private void OnFlyDown(InputAction.CallbackContext ctx) => verticalInput = -ctx.ReadValue<float>();
    private void OnFlyDownCanceled(InputAction.CallbackContext ctx) => verticalInput = 0f;

    private void OnPhaseToggle(InputAction.CallbackContext ctx) => TogglePhase();


    private void Update()
    {
        //if (freezeInput) return;

        //HandleLook();

        // Countdown phasing timer
        if (isPhasing)
        {
            phaseTimer -= Time.deltaTime;
            if (phaseTimer <= 0f)
            {
                // Time's up, automatically stop phasing
                TogglePhase();
            }
        }
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
            {
                isStunned = false;
                FreezeInput(false);
            }
        }
    }

    private void FixedUpdate()
    {
        if (freezeInput) return;

        // Rotate body and camera
        HandleRotation();

        // Move the ghost
        HandleMovement();
    }

    private void HandleRotation()
    {
        // Apply yaw to body
        float yRotation = lookInput.x * lookSensitivity;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, yRotation, 0f));

        // Apply pitch to camera
        xRotation -= lookInput.y * lookSensitivity;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);
        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }



    private void HandleLook()
    {
        // Apply body rotation (yaw)
        float yaw = lookInput.x * lookSensitivity;
        targetRotation *= Quaternion.Euler(0f, yaw, 0f);
        transform.rotation = targetRotation;

        // Apply camera pitch
        xRotation = Mathf.Clamp(xRotation - lookInput.y * lookSensitivity, -80f, 80f);
        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    private void HandleMovement()
    {
        Vector3 moveDir = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
        Vector3 verticalMove = Vector3.up * verticalInput;
        Vector3 moveAmount = (moveDir * moveSpeed + verticalMove * flySpeed) * Time.fixedDeltaTime;

        if (moveAmount.sqrMagnitude <= 0f) return;

        if (isPhasing)
        {
            int defaultMask = LayerMask.GetMask(defaultLayerName);
            if (!Physics.CapsuleCast(rb.position + Vector3.up * 0.5f, rb.position - Vector3.up * 0.5f, 0.5f,
                moveAmount.normalized, moveAmount.magnitude, defaultMask))
            {
                rb.MovePosition(rb.position + moveAmount);
            }
        }
        else
        {
            if (!Physics.CapsuleCast(rb.position + Vector3.up * 0.5f, rb.position - Vector3.up * 0.5f, 0.5f,
                moveAmount.normalized, moveAmount.magnitude))
            {
                rb.MovePosition(rb.position + moveAmount);
            }
        }
    }

    public void TogglePhase()
    {
        if (!isPhasing && fear < phaseCost) return; // can't enable if not enough fear

        // Only drain fear when turning on
        if (!isPhasing)
        {
            fear -= phaseCost;
            fear = Mathf.Max(fear, 0f);

            // Start timer
            phaseTimer = phaseDuration;
        }

        // Toggle phasing state
        isPhasing = !isPhasing;

        if (isPhasing)
        {
            gameObject.layer = ghostLayer;
            Physics.IgnoreLayerCollision(ghostLayer, phaseableWallLayer, true);
        }
        else
        {
            gameObject.layer = defaultLayer;
            Physics.IgnoreLayerCollision(ghostLayer, phaseableWallLayer, false);
        }

        // Update ghost transparency
        if (ghostRenderer != null)
        {
            Color c = ghostRenderer.material.color;
            c.a = isPhasing ? 0.4f : 1f;
            ghostRenderer.material.color = c;
        }
    }
    public void FreezeInput(bool freeze)
    {
        freezeInput = freeze;
    }

    public void SetVisibility(bool visible)
    {
        if (ghostRenderer != null)
            ghostRenderer.enabled = visible;

        if (ghostCamera != null)
            ghostCamera.enabled = visible;
    }

    public void ResetPositionAndRotation()
    {
        rb.isKinematic = false;
        rb.velocity = Vector3.zero;
    }
    public void Stun(float duration)
    {
        isStunned = true;
        stunTimer = duration;
        FreezeInput(true);
    }
}
