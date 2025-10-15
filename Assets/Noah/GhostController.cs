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
    public float fearDrainRateDuringPossession = 15f;
    public float phaseCost = 10f;
    public float requiredFearToPossess = 50f;

    [Header("Layers")]
    public string defaultLayerName = "Default";
    public string ghostLayerName = "Ghost";
    public string phaseableWallLayerName = "PhaseableWall";

    [Header("References")]
    public Transform cameraTransform;    // ghost camera pivot
    public Camera ghostCamera;           // ghost Camera component
    public PlayerInputHandler player;    // player script (assign in Inspector)

    private PlayerInputs inputActions;
    private Rigidbody rb;
    private Renderer ghostRenderer;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private float verticalInput;
    private float xRotation;
    private bool isPhasing = false;
    private bool isPossessing = false;

    private int defaultLayer;
    private int ghostLayer;
    private int phaseableWallLayer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ghostRenderer = GetComponentInChildren<Renderer>();

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
        // store reference and subscribe handlers
        inputActions = actions;
        SubscribeInputs();
        inputActions.Enable();
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

        inputActions.Player.Possess.performed += OnPossessPressed;
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

        inputActions.Player.Possess.performed -= OnPossessPressed;
    }

    #region Input Callbacks
    private void OnMovePerformed(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();
    private void OnMoveCanceled(InputAction.CallbackContext _) => moveInput = Vector2.zero;

    private void OnLookPerformed(InputAction.CallbackContext ctx) => lookInput = ctx.ReadValue<Vector2>();
    private void OnLookCanceled(InputAction.CallbackContext _) => lookInput = Vector2.zero;

    private void OnPhaseToggle(InputAction.CallbackContext _) => TryTogglePhase();

    private void OnFlyUp(InputAction.CallbackContext ctx) => verticalInput = ctx.ReadValue<float>();
    private void OnFlyUpCanceled(InputAction.CallbackContext _) => verticalInput = 0f;

    private void OnFlyDown(InputAction.CallbackContext ctx) => verticalInput = -ctx.ReadValue<float>();
    private void OnFlyDownCanceled(InputAction.CallbackContext _) => verticalInput = 0f;

    private void OnPossessPressed(InputAction.CallbackContext _) => TryTogglePossession();
    #endregion

    private void Update()
    {
        if (isPossessing) return; // ghost doesn't look or move while possessing
        HandleLook();
        HandleFear();
    }

    private void FixedUpdate()
    {
        if (isPossessing) return;
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
        Vector3 moveDir = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
        Vector3 verticalMove = Vector3.up * verticalInput;
        Vector3 moveAmount = (moveDir * moveSpeed + verticalMove * flySpeed) * Time.fixedDeltaTime;

        if (isPhasing)
        {
            // when phasing we ignore phaseable walls only (default stays blocking)
            int defaultMask = LayerMask.GetMask(defaultLayerName);
            if (!Physics.CapsuleCast(rb.position + Vector3.up * 0.5f, rb.position - Vector3.up * 0.5f, 0.5f,
                moveAmount.normalized, moveAmount.magnitude, defaultMask))
            {
                rb.position += moveAmount;
            }
        }
        else
        {
            // normal movement blocks everything
            if (moveAmount.sqrMagnitude > 0f)
            {
                if (!Physics.CapsuleCast(rb.position + Vector3.up * 0.5f, rb.position - Vector3.up * 0.5f, 0.5f,
                    moveAmount.normalized, moveAmount.magnitude))
                {
                    rb.MovePosition(rb.position + moveAmount);
                }
            }
        }
    }

    private void TryTogglePhase()
    {
        if (fear < phaseCost) return;
        if (isPossessing) return;

        isPhasing = !isPhasing;
        fear -= phaseCost;
        fear = Mathf.Max(fear, 0f);

        if (isPhasing)
        {
            gameObject.layer = ghostLayer;
            Physics.IgnoreLayerCollision(ghostLayer, phaseableWallLayer, true);
        }
        else
        {
            Collider[] overlaps = Physics.OverlapCapsule(rb.position + Vector3.up * 0.5f, rb.position - Vector3.up * 0.5f, 0.5f,
                (1 << defaultLayer) | (1 << phaseableWallLayer));
            foreach (var hit in overlaps)
            {
                Vector3 push = rb.position - hit.ClosestPoint(rb.position);
                rb.position += push.normalized * 0.6f;
            }

            gameObject.layer = defaultLayer;
            Physics.IgnoreLayerCollision(ghostLayer, phaseableWallLayer, false);
        }

        if (ghostRenderer != null)
        {
            Color c = ghostRenderer.material.color;
            c.a = isPhasing ? 0.4f : 1f;
            ghostRenderer.material.color = c;
        }
    }

    private void TryTogglePossession()
    {
        if (isPossessing)
        {
            EndPossession();
            return;
        }

        if (fear < requiredFearToPossess) return;

        float distance = Vector3.Distance(transform.position, player.transform.position);
        if (distance > 3f) return;

        StartPossession();
    }

    private void StartPossession()
    {
        if (inputActions == null) return;

        isPossessing = true;

        // stop ghost from receiving inputs
        UnsubscribeInputs();
        inputActions.Disable();

        // hide and disable physics
        rb.isKinematic = true;
        GetComponent<Collider>().enabled = false;
        if (ghostRenderer) ghostRenderer.enabled = false;
        if (ghostCamera) ghostCamera.enabled = false;

        // transfer the inputs to the player (player will subscribe its own callbacks)
        player.TakeControl(inputActions);

        // note: player should enable its own camera inside TakeControl
    }

    public void EndPossession()
    {
        if (!isPossessing) return;

        isPossessing = false;

        // ask player to release the shared input
        player.ReleaseControl();

        // restore ghost visuals and physics
        rb.isKinematic = false;
        GetComponent<Collider>().enabled = true;
        if (ghostRenderer) ghostRenderer.enabled = true;
        if (ghostCamera) ghostCamera.enabled = true;

        // re-subscribe ghost handlers and re-enable input
        SubscribeInputs();
        inputActions.Enable();
    }

    private void HandleFear()
    {
        // drain while possessing is handled by player? keep here if you prefer
        // (we'll handle possession drain in player to avoid coupling; but below is an option if desired)
    }
}
