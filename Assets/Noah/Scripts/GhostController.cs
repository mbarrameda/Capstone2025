using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class GhostController : MonoBehaviour
{
    [Header("Possession Menu")]
    [SerializeField] private string possessionMenuCanvasName = "Possession Menu Canvas";
    private PossessionMenu possessionMenu;
    private bool menuOpen = false;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float flySpeed = 3f;
    public float lookSensitivity = 2f;

    [Header("Phasing & Fear")]
    public float fear = 100f;
    public float phaseCost = 25f;
    public float phaseDuration = 10f;
    private float phaseTimer = 0f;

    [Header("Layers")]
    public string defaultLayerName = "Default";
    public string ghostLayerName = "Ghost";
    public string phaseableWallLayerName = "PhaseableWall";

    [Header("References")]
    public Transform cameraTransform;
    public Camera ghostCamera;
    public Renderer ghostRenderer;
    public Rigidbody rb;

    [Header("Camera Settings")]
    public bool invertYLook = false;
    public Transform cameraPivot;

    private bool isStunned = false;
    private float stunTimer = 0f;
    private Canvas promptCanvas;
    private Text promptText;
    // Input & physics
    public PlayerInputs playerInputs;

    // Runtime
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

    private List<PossessableObject> possessedObjects = new List<PossessableObject>();

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

        if (promptCanvas != null)
            promptCanvas.enabled = false;

        // Find the possession menu automatically
        FindPossessionMenu();
    }

    private void FindPossessionMenu()
    {
        // Method 1: Find by name
        GameObject canvasObject = GameObject.Find(possessionMenuCanvasName);
        if (canvasObject != null)
        {
            possessionMenu = canvasObject.GetComponent<PossessionMenu>();
            if (possessionMenu != null)
            {
                Debug.Log("Found PossessionMenu automatically!");
                possessionMenu.Initialize(OnMenuOptionSelected);
            }
        }

        // Method 2: Find any PossessionMenu in scene
        if (possessionMenu == null)
        {
            possessionMenu = FindObjectOfType<PossessionMenu>();
            if (possessionMenu != null)
            {
                Debug.Log("Found PossessionMenu using FindObjectOfType!");
                possessionMenu.Initialize(OnMenuOptionSelected);
            }
        }

        if (possessionMenu == null)
        {
            Debug.LogWarning("Could not find PossessionMenu. Menu functionality will not work.");
        }
    }

    private void OnMenuOptionSelected(GameObject selectedObject)
    {
        // Check if this GhostController is still valid
        if (this == null)
        {
            Debug.LogError("GhostController has been destroyed!");
            return;
        }

        Debug.Log($"Menu option selected: {selectedObject?.name ?? "Explorer"}");

        if (selectedObject == null)
        {
            // Spawn explorer clone
            Debug.Log("Spawning explorer clone");
            GameManager.Instance.SpawnExplorerClone(this);
        }
        else
        {
            // Spawn object clone
            PossessableObject possessable = selectedObject.GetComponent<PossessableObject>();
            if (possessable != null && GameManager.Instance != null)
            {
                Debug.Log($"Spawning object clone: {possessable.name}");
                GameManager.Instance.SpawnObjectClone(this, possessable);
            }
            else
            {
                Debug.LogError($"Selected object {selectedObject.name} doesn't have PossessableObject component!");
                // Fallback to explorer clone
                GameManager.Instance.SpawnExplorerClone(this);
            }
        }

        ClosePossessionMenu();
    }

    public void AssignInput(PlayerInputs actions)
    {
        // First remove any existing input to prevent duplicates
        RemoveInput();

        playerInputs = actions;
        SubscribeInputs();
        playerInputs.Enable();
    }

    public void RemoveInput()
    {
        if (playerInputs == null) return;
        UnsubscribeInputs();
        playerInputs.Disable();
        playerInputs = null;
    }

    private void SubscribeInputs()
    {
        if (playerInputs == null) return;

        playerInputs.Player.Movement.performed += OnMovePerformed;
        playerInputs.Player.Movement.canceled += OnMoveCanceled;

        playerInputs.Player.Look.performed += OnLookPerformed;
        playerInputs.Player.Look.canceled += OnLookCanceled;

        playerInputs.Player.PhaseToggle.performed += OnPhaseToggle;
        playerInputs.Player.PossessObject.performed += OnPossessObject;
        playerInputs.Player.MenuToggle.performed += OnMenuToggle;

        playerInputs.Player.FlyUp.performed += OnFlyUpPerformed;
        playerInputs.Player.FlyUp.canceled += OnFlyUpCanceled;

        playerInputs.Player.FlyDown.performed += OnFlyDownPerformed;
        playerInputs.Player.FlyDown.canceled += OnFlyDownCanceled;
    }

    private void UnsubscribeInputs()
    {
        if (playerInputs == null) return;

        playerInputs.Player.Movement.performed -= OnMovePerformed;
        playerInputs.Player.Movement.canceled -= OnMoveCanceled;

        playerInputs.Player.Look.performed -= OnLookPerformed;
        playerInputs.Player.Look.canceled -= OnLookCanceled;

        playerInputs.Player.PhaseToggle.performed -= OnPhaseToggle;
        playerInputs.Player.PossessObject.performed -= OnPossessObject;
        playerInputs.Player.MenuToggle.performed -= OnMenuToggle;

        playerInputs.Player.FlyUp.performed -= OnFlyUpPerformed;
        playerInputs.Player.FlyUp.canceled -= OnFlyUpCanceled;

        playerInputs.Player.FlyDown.performed -= OnFlyDownPerformed;
        playerInputs.Player.FlyDown.canceled -= OnFlyDownCanceled;
    }

    // Input Handlers
    private void OnMovePerformed(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();
    private void OnMoveCanceled(InputAction.CallbackContext ctx) => moveInput = Vector2.zero;

    private void OnLookPerformed(InputAction.CallbackContext ctx) => lookInput = ctx.ReadValue<Vector2>();
    private void OnLookCanceled(InputAction.CallbackContext ctx) => lookInput = Vector2.zero;

    private void OnFlyUpPerformed(InputAction.CallbackContext ctx) => verticalInput = 1f;
    private void OnFlyUpCanceled(InputAction.CallbackContext ctx) => verticalInput = 0f;

    private void OnFlyDownPerformed(InputAction.CallbackContext ctx) => verticalInput = -1f;
    private void OnFlyDownCanceled(InputAction.CallbackContext ctx) => verticalInput = 0f;

    private void OnPhaseToggle(InputAction.CallbackContext ctx) => TogglePhase();

    private void OnPossessObject(InputAction.CallbackContext ctx)
    {
        TryPossessNearestObject();
    }

    private void OnMenuToggle(InputAction.CallbackContext ctx)
    {
        TogglePossessionMenu();
    }

    public void TogglePossessionMenu()
    {
        // Don't open menu if already controlling a clone
        if (GameManager.Instance.HasActiveClone(this))
        {
            Debug.Log("Cannot open menu - already controlling a clone");
            return;
        }

        menuOpen = !menuOpen;

        if (menuOpen)
        {
            OpenPossessionMenu();
        }
        else
        {
            ClosePossessionMenu();
        }
    }

    private void OpenPossessionMenu()
    {
        if (possessionMenu == null)
        {
            Debug.LogError("PossessionMenu reference is null!");
            // Fallback: spawn explorer clone
            GameManager.Instance.SpawnExplorerClone(this);
            return;
        }

        // Get possessed objects
        List<GameObject> possessedGameObjects = new List<GameObject>();
        foreach (var obj in possessedObjects)
        {
            if (obj != null && obj.isPossessed)
                possessedGameObjects.Add(obj.gameObject);
        }

        // Open the menu
        possessionMenu.OpenMenu(possessedGameObjects);

        // Freeze ghost movement
        FreezeInput(true);

        // Set cursor state
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Possession menu opened with " + possessedGameObjects.Count + " objects");
    }

    private void ClosePossessionMenu()
    {
        if (possessionMenu != null)
            possessionMenu.CloseMenu();

        menuOpen = false;
        FreezeInput(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("Possession menu closed");
    }

    private void Update()
    {
        if (isPhasing)
        {
            phaseTimer -= Time.deltaTime;
            if (phaseTimer <= 0f)
                TogglePhase();
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

        UpdateUIPrompt();
    }

    private void FixedUpdate()
    {
        if (freezeInput) return;

        HandleRotation();
        HandleMovement();
    }

    private void HandleRotation()
    {
        if (freezeInput) return;

        float yRotation = lookInput.x * lookSensitivity;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, yRotation, 0f));

        // Handle Y inversion based on setting
        float yLook = lookInput.y * lookSensitivity;
        if (invertYLook)
            yLook = -yLook;

        xRotation -= yLook;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        if (cameraPivot != null)
        {
            cameraPivot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
        else if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
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
        if (!isPhasing && fear < phaseCost) return;

        if (!isPhasing)
        {
            fear -= phaseCost;
            fear = Mathf.Max(fear, 0f);
            phaseTimer = phaseDuration;
        }

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

        if (ghostRenderer != null)
        {
            Color c = ghostRenderer.material.color;
            c.a = isPhasing ? 0.4f : 1f;
            ghostRenderer.material.color = c;
        }
    }

    private void UpdateUIPrompt()
    {
        if (promptCanvas == null || promptText == null) return;

        PossessableObject nearest = GetNearestPossessable();
        if (nearest != null)
        {
            promptCanvas.enabled = true;
            promptText.text = "Press [O/B] to Possess\nHold [Y/Triangle] to Transform";
        }
        else
        {
            promptCanvas.enabled = false;
        }
    }

    private PossessableObject GetNearestPossessable()
    {
        float range = 3f;
        Collider[] hits = Physics.OverlapSphere(transform.position, range);

        PossessableObject nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out PossessableObject obj) && !obj.isPossessed)
            {
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < nearestDist)
                {
                    nearest = obj;
                    nearestDist = dist;
                }
            }
        }
        return nearest;
    }

    private void TryPossessNearestObject()
    {
        PossessableObject nearest = GetNearestPossessable();

        if (nearest != null)
        {
            bool success = nearest.TryPossess(this);
            if (success)
            {
                Debug.Log($"Successfully possessed object: {nearest.name}");
                // Make sure it's added to the list
                if (!possessedObjects.Contains(nearest))
                {
                    possessedObjects.Add(nearest);
                    Debug.Log($"Added {nearest.name} to possessed objects list");
                }
            }
            else
            {
                Debug.Log("Failed to possess object (not enough fear or already possessed)");
            }
        }
        else
        {
            Debug.Log("No possessable object nearby.");
        }
    }

    public void FreezeInput(bool freeze)
    {
        freezeInput = freeze;

        // Also reset movement inputs when freezing
        if (freeze)
        {
            moveInput = Vector2.zero;
            lookInput = Vector2.zero;
            verticalInput = 0f;
        }
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

    public void RegisterPossessedObject(PossessableObject obj)
    {
        if (!possessedObjects.Contains(obj))
            possessedObjects.Add(obj);
    }

    // Public method to set possession menu (optional)
    public void SetPossessionMenu(PossessionMenu menu)
    {
        possessionMenu = menu;
        if (possessionMenu != null)
        {
            possessionMenu.Initialize(OnMenuOptionSelected);
        }
    }
}