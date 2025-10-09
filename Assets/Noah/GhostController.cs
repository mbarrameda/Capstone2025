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

    [Header("Layers")]
    public string defaultLayerName = "Default";
    public string ghostLayerName = "Ghost";
    public string phaseableWallLayerName = "PhaseableWall";

    [Header("References")]
    public Transform cameraTransform;

    private PlayerInputs inputActions;
    private Rigidbody rb;
    private Renderer ghostRenderer;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private float verticalInput;
    private float xRotation;
    private bool isPhasing = false;

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

        Physics.IgnoreLayerCollision(defaultLayer, ghostLayer, false);
        Physics.IgnoreLayerCollision(defaultLayer, phaseableWallLayer, false);
    }

    public void AssignInput(PlayerInputs actions)
    {
        inputActions = actions;
        inputActions.Enable();

        inputActions.Player.Movement.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Movement.canceled += ctx => moveInput = Vector2.zero;

        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;

        inputActions.Player.PhaseToggle.performed += ctx => TogglePhase();

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
        Vector3 moveDir = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
        Vector3 verticalMove = Vector3.up * verticalInput;
        Vector3 moveAmount = (moveDir * moveSpeed + verticalMove * flySpeed) * Time.fixedDeltaTime;

        if (isPhasing)
        {
            // Free movement only through phaseable walls (default walls are still colliding)
            rb.MovePosition(rb.position + moveAmount);
        }
        else
        {
            // Solid movement, collide with everything
            if (!Physics.CapsuleCast(
                    rb.position + Vector3.up * 0.5f,
                    rb.position - Vector3.up * 0.5f,
                    0.5f,
                    moveAmount.normalized,
                    moveAmount.magnitude,
                    ~LayerMask.GetMask(ghostLayerName)))
            {
                rb.MovePosition(rb.position + moveAmount);
            }
        }
    }


    private void TogglePhase()
    {
        if (fear <= 0f && !isPhasing) return;

        isPhasing = !isPhasing;

        if (isPhasing)
        {
            gameObject.layer = ghostLayer;

            // Only ignore collisions with phaseable walls
            Physics.IgnoreLayerCollision(ghostLayer, phaseableWallLayer, true);
        }
        else
        {
            // Push ghost out of walls to prevent clipping
            Collider[] overlaps = Physics.OverlapCapsule(
                rb.position + Vector3.up * 0.5f,
                rb.position - Vector3.up * 0.5f,
                0.5f,
                (1 << defaultLayer) | (1 << phaseableWallLayer));

            foreach (var hit in overlaps)
            {
                Vector3 push = rb.position - hit.ClosestPoint(rb.position);
                rb.position += push.normalized * 0.6f;
            }

            gameObject.layer = defaultLayer;
            Physics.IgnoreLayerCollision(ghostLayer, phaseableWallLayer, false);
        }

        // Optional ghost transparency feedback
        if (ghostRenderer != null)
        {
            Color c = ghostRenderer.material.color;
            c.a = isPhasing ? 0.4f : 1f;
            ghostRenderer.material.color = c;
        }
    }


    private void HandleFear()
    {
        if (isPhasing)
        {
            fear -= fearDrainRate * Time.deltaTime;
            fear = Mathf.Max(fear, 0f);

            if (fear <= 0f && isPhasing)
            {
                TogglePhase();
            }
        }
    }
}
