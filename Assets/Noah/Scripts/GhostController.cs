using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class GhostController : MonoBehaviour
{
    [Header("Possession Menu")]
    [SerializeField] private string possessionMenuCanvasName = "Possession Menu Canvas";
    public PossessionMenu possessionMenu;
    private bool menuOpen = false;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float flySpeed = 3f;
    public float lookSensitivity = 2f;
    public bool lockObjectRotation = true;

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
    public bool preserveCameraSetup = false; // prevents camera reset when controlling clones

    [Header("Status Flags")]
    public bool isControllingClone = false;

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
    public float xRotation;
    private bool isPhasing = false;
    private bool freezeInput = false;

    private int defaultLayer;
    private int ghostLayer;
    private int phaseableWallLayer;
    public GameObject activeClone;

    private Quaternion targetRotation;

    private List<PossessableObject> possessedObjects = new List<PossessableObject>();

    public System.Action OnDestroyClone;
    public System.Action OnReturnToGhost;

    private void OnDestroy()
    {
        // Clean up input subscriptions when destroyed
        RemoveInput();

        // Clear any ongoing coroutines
        StopAllCoroutines();
    }
    // -------------------------------
    // Initialization
    // -------------------------------
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ghostRenderer = ghostRenderer ?? GetComponentInChildren<Renderer>();
        targetRotation = transform.rotation;

        // Rigidbody setup
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Cache layer indexes
        defaultLayer = LayerMask.NameToLayer(defaultLayerName);
        ghostLayer = LayerMask.NameToLayer(ghostLayerName);
        phaseableWallLayer = LayerMask.NameToLayer(phaseableWallLayerName);

        gameObject.layer = defaultLayer;

        if (promptCanvas != null)
            promptCanvas.enabled = false;

        FindPossessionMenu();
    }

    // Automatically locate the Possession Menu in the scene
    private void FindPossessionMenu()
    {
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


    // -------------------------------
    // Possession Menu
    // -------------------------------
    private void OnMenuOptionSelected(GameObject selectedObject)
    {
        if (this == null)
        {
            Debug.LogError("GhostController has been destroyed!");
            return;
        }

        Debug.Log($"Menu option selected: {selectedObject?.name ?? "Explorer"}");

        if (selectedObject == null)
        {
            Debug.Log("Spawning explorer clone");
            GameManager.Instance.SpawnExplorerClone(this);
        }
        else
        {
            PossessableObject possessable = selectedObject.GetComponent<PossessableObject>();
            if (possessable != null && GameManager.Instance != null)
            {
                Debug.Log($"Spawning object clone: {possessable.name}");
                GameManager.Instance.SpawnObjectClone(this, possessable);
            }
            else
            {
                Debug.LogError($"Selected object {selectedObject.name} doesn't have PossessableObject component!");
                GameManager.Instance.SpawnExplorerClone(this);
            }
        }

        if (GameManager.Instance.HasActiveClone(this))
        {
            Debug.LogWarning("Already controlling a clone — ignoring menu input");
            return;
        }

        ClosePossessionMenu();
    }


    // -------------------------------
    // Input Assignment
    // -------------------------------
    public void AssignInput(PlayerInputs actions)
    {
        RemoveInput();
        playerInputs = actions;
        SubscribeInputs();
        playerInputs.Enable();

        if (!preserveCameraSetup && !isControllingClone)
        {
            if (cameraPivot != null)
            {
                cameraPivot.localPosition = Vector3.zero;
                cameraPivot.localRotation = Quaternion.identity;
            }
            else if (cameraTransform != null)
            {
                cameraTransform.localPosition = Vector3.zero;
                cameraTransform.localRotation = Quaternion.identity;
            }
        }
    }

    public void RemoveInput()
    {
        if (playerInputs == null) return;
        UnsubscribeInputs();
        playerInputs.Disable();
        playerInputs = null;
    }


    // -------------------------------
    // Input Subscriptions
    // -------------------------------
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


    // -------------------------------
    // Input Handlers
    // -------------------------------
    private void OnMovePerformed(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();
    private void OnMoveCanceled(InputAction.CallbackContext ctx) => moveInput = Vector2.zero;

    private void OnLookPerformed(InputAction.CallbackContext ctx) => lookInput = ctx.ReadValue<Vector2>();
    private void OnLookCanceled(InputAction.CallbackContext ctx) => lookInput = Vector2.zero;

    private void OnFlyUpPerformed(InputAction.CallbackContext ctx) => verticalInput = 1f;
    private void OnFlyUpCanceled(InputAction.CallbackContext ctx) => verticalInput = 0f;

    private void OnFlyDownPerformed(InputAction.CallbackContext ctx) => verticalInput = -1f;
    private void OnFlyDownCanceled(InputAction.CallbackContext ctx) => verticalInput = 0f;

    private void OnPhaseToggle(InputAction.CallbackContext ctx) => TogglePhase();
    private void OnPossessObject(InputAction.CallbackContext ctx) => TryPossessNearestObject();



    // -------------------------------
    // Possession Menu Logic
    // -------------------------------
    private void OnMenuToggle(InputAction.CallbackContext ctx)
    {
        // Safety check - if this object is destroyed, don't process input
        if (this == null) return;

        if (isControllingClone)
        {
            if (OnDestroyClone != null)
            {
                FreezeInput(true);
                SetVisibility(false);

                // Use a safer approach without coroutine
                OnDestroyClone?.Invoke();
                Destroy(gameObject);
            }
            return;
        }

        // Normal ghost menu
        TogglePossessionMenu();
    }

    private System.Collections.IEnumerator DestroyCloneNextFrame()
    {
        yield return null; // wait one frame
        OnDestroyClone?.Invoke();
        Destroy(gameObject); // now safe to destroy
    }
    public void TogglePossessionMenu()
    {
        if (this == null)
        {
            Debug.LogError("GhostController has been destroyed!");
            return;
        }

        if (isControllingClone)
        {
            Debug.Log("Menu disabled while controlling a clone/object.");
            return;
        }

        if (GameManager.Instance.HasActiveClone(this))
        {
            Debug.Log("Cannot open menu - already controlling a clone");
            return;
        }

        if (possessionMenu == null)
        {
            Debug.LogError("PossessionMenu is null!");
            return;
        }

        menuOpen = !menuOpen;
        if (menuOpen)
            OpenPossessionMenu();
        else
            ClosePossessionMenu();
    }

    private void OpenPossessionMenu()
    {
        if (possessionMenu == null)
        {
            Debug.LogError("PossessionMenu reference is null!");
            return;
        }

        List<GameObject> possessedGameObjects = new List<GameObject>();
        foreach (var obj in possessedObjects)
        {
            if (obj != null && obj.isPossessed)
                possessedGameObjects.Add(obj.gameObject);
        }

        // The menu should always show at least the Explorer option
        possessionMenu.OpenMenu(possessedGameObjects);

        FreezeInput(true);
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


    // -------------------------------
    // Update & Physics
    // -------------------------------
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


    // -------------------------------
    // Movement & Camera
    // -------------------------------
    private void HandleRotation()
    {
        float yRotation = lookInput.x * lookSensitivity;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, yRotation, 0f));

        // Control camera pitch (X rotation)
        if (!preserveCameraSetup)
        {
            xRotation -= lookInput.y * lookSensitivity * (invertYLook ? -1f : 1f);
            xRotation = Mathf.Clamp(xRotation, -80f, 80f);

            if (cameraTransform != null)
                cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }

    private void HandleMovement()
    {
        if (moveInput.sqrMagnitude <= 0f) return;

        Vector3 moveDir;

        if (lockObjectRotation)
        {
            // For objects: move relative to camera direction, not object direction
            if (cameraTransform != null)
            {
                // Get camera forward and right vectors, but flatten them to the horizontal plane
                Vector3 cameraForward = cameraTransform.forward;
                cameraForward.y = 0;
                cameraForward.Normalize();

                Vector3 cameraRight = cameraTransform.right;
                cameraRight.y = 0;
                cameraRight.Normalize();

                moveDir = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;
            }
            else
            {
                // Fallback: use object's forward/right
                moveDir = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
            }
        }
        else
        {
            // For ghost: use object's forward/right (normal behavior)
            moveDir = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
        }

        Vector3 verticalMove = Vector3.up * verticalInput;
        Vector3 moveAmount = (moveDir * moveSpeed + verticalMove * flySpeed) * Time.fixedDeltaTime;

        if (moveAmount.sqrMagnitude <= 0f) return;

        if (isPhasing)
        {
            int defaultMask = LayerMask.GetMask(defaultLayerName);
            if (!Physics.CapsuleCast(rb.position + Vector3.up * 0.5f,
                rb.position - Vector3.up * 0.5f, 0.5f,
                moveAmount.normalized, moveAmount.magnitude, defaultMask))
            {
                rb.MovePosition(rb.position + moveAmount);
            }
        }
        else
        {
            if (!Physics.CapsuleCast(rb.position + Vector3.up * 0.5f,
                rb.position - Vector3.up * 0.5f, 0.5f,
                moveAmount.normalized, moveAmount.magnitude))
            {
                rb.MovePosition(rb.position + moveAmount);
            }
        }
    }


    // -------------------------------
    // Abilities & Status
    // -------------------------------
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


    // -------------------------------
    // Possession System
    // -------------------------------
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
                if (!possessedObjects.Contains(nearest))
                {
                    possessedObjects.Add(nearest);
                    Debug.Log($"Added {nearest.name} to possessed list");
                }
            }
            else Debug.Log("Failed to possess object.");
        }
        else Debug.Log("No possessable object nearby.");
    }


    // -------------------------------
    // Utility & Helpers
    // -------------------------------
    public void FreezeInput(bool freeze)
    {
        freezeInput = freeze;
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

    public void SetPossessionMenu(PossessionMenu menu)
    {
        possessionMenu = menu;
        if (possessionMenu != null)
            possessionMenu.Initialize(OnMenuOptionSelected);
    }
}
