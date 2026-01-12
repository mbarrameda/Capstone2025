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
    public bool menuOpen = false;

    [Header("Ghost Sanity UI")]
    public GameObject ghostSanityBarPrefab;   // Drag prefab here
    private Slider ghostSanitySlider;
    private Image ghostSanityFill;


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

    [Header("Fear Drain Settings")]
    public float baseFearDrainRate = 5f;
    public float phasingFearDrainRate = 7f;
    public float cloneControlFearDrainRate = 10f;
    public float stunnedFearDrainRate = 5f;

    [Header("Fear Gain from Player Proximity")]
    public float maxFearGainDistance = 15f;
    public float minFearGainDistance = 1f;
    public float maxFearGainRate = 6f;
    public float fearGainUpdateInterval = 0.5f;
    private float fearGainTimer = 0f;
    private PlayerInputHandler nearestPlayer;

    [Header("Fear Regeneration")]
    public float fearRegenRate = 3f; // Fear regenerated per second
    public float fearRegenDelay = 2f; // Seconds after fear drain before regen starts
    public bool canRegenFear = true;
    private float fearRegenTimer = 0f;
    private bool isRegenerating = false;


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

    public bool isStunned = false;
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
    public bool freezeInput = false;

    private int defaultLayer;
    private int ghostLayer;
    private int phaseableWallLayer;
    public GameObject activeClone;

    private Quaternion targetRotation;

    public List<PossessableObject> possessedObjects = new List<PossessableObject>();

    public System.Action OnDestroyClone;
    public System.Action OnReturnToGhost;

    private void OnDestroy()
    {
        // Clean up input subscriptions when destroyed
        RemoveInput();

        // Clear any ongoing coroutines
        StopAllCoroutines();

        PlayerInputHandler.OnExplorerSanityChanged -= UpdateGhostSanityBar;
    }

    // -------------------------------
    // Update & Physics
    // -------------------------------
    private void Start()
    {
        SetupGhostSanityUI();

        PlayerInputHandler.OnExplorerSanityChanged += UpdateGhostSanityBar;
    }
    private void Update()
    {
        // Handle fear drain
        HandleFearDrain();

        // Handle fear gain from player proximity
        HandleFearGainFromPlayer();

        // Handle fear regeneration when not near players
        HandleFearRegeneration();

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
                Debug.Log("Ghost no longer stunned");
            }
        }

        UpdateUIPrompt();
    }

    private void FixedUpdate()
    {
        HandleRotation();
        HandleMovement();
        if (freezeInput)
        {
            Debug.Log("🚫 Input frozen - skipping movement update");
            return;
        }
    }

    private void UpdateGhostSanityBar(float sanity)
    {
        if (ghostSanitySlider == null) return;

        ghostSanitySlider.value = sanity;

        if (ghostSanityFill != null)
        {
            float percent = sanity / 100f;
            ghostSanityFill.color = Color.Lerp(Color.blue, Color.red, 1f - percent);
        }
    }

    void UpdateGhostSanityUI(float sanity)
    {
        if (ghostSanitySlider == null) return;

        ghostSanitySlider.value = sanity;

        if (ghostSanityFill != null)
        {
            ghostSanityFill.color = Color.Lerp(Color.green, Color.red, sanity / 100f);
        }
    }

    private void SetupGhostSanityUI()
    {
        if (ghostSanityBarPrefab == null)
        {
            Debug.LogError("Ghost sanity bar prefab is missing!");
            return;
        }

        // Find main canvas (Display 1)
        Canvas targetCanvas = FindObjectOfType<Canvas>();

        if (targetCanvas == null)
        {
            Debug.LogError("No canvas found!");
            return;
        }

        // Create the sanity bar on the main canvas
        GameObject uiObj = Instantiate(ghostSanityBarPrefab, targetCanvas.transform);

        ghostSanitySlider = uiObj.GetComponent<Slider>();

        if (ghostSanitySlider == null)
        {
            Debug.LogError("The prefab needs a Slider component at the root!");
            return;
        }

        // Try to find the Fill image
        if (ghostSanitySlider.fillRect != null)
            ghostSanityFill = ghostSanitySlider.fillRect.GetComponent<Image>();

        Debug.Log("Ghost sanity UI successfully created on split-screen display.");
    }

    // -------------------------------
    // Fear Management
    // -------------------------------

    private void HandleFearRegeneration()
    {
        if (!canRegenFear || fear >= 100f) return;

        // Check if we should start regenerating
        bool shouldRegen = nearestPlayer == null ||
                          Vector3.Distance(transform.position, nearestPlayer.transform.position) > maxFearGainDistance;

        if (shouldRegen)
        {
            fearRegenTimer += Time.deltaTime;

            // Start regeneration after the delay period
            if (fearRegenTimer >= fearRegenDelay)
            {
                if (!isRegenerating)
                {
                    isRegenerating = true;
                    Debug.Log("Fear regeneration started");
                }

                float regenAmount = fearRegenRate * Time.deltaTime;
                fear += regenAmount;
                fear = Mathf.Min(fear, 100f);

                // Debug logging
                if (Time.frameCount % 120 == 0)
                {
                    Debug.Log($"Fear Regen: {fear:F1} (+{regenAmount / Time.deltaTime:F1}/sec)");
                }
            }
        }
        else
        {
            // Reset regeneration timer when near players
            fearRegenTimer = 0f;
            isRegenerating = false;
        }
    }

    private void HandleFearDrain()
    {
        float drainAmount = 0f;

        // Calculate drain based on current state
        if (isStunned)
        {
            drainAmount = stunnedFearDrainRate * Time.deltaTime;
        }
        else if (isControllingClone)
        {
            drainAmount = cloneControlFearDrainRate * Time.deltaTime;
        }
        else if (isPhasing)
        {
            drainAmount = (baseFearDrainRate + phasingFearDrainRate) * Time.deltaTime;
        }
        else
        {
            drainAmount = baseFearDrainRate * Time.deltaTime;
        }

        // Apply fear drain
        fear -= drainAmount;
        fear = Mathf.Max(fear, 0f);

        // Debug logging to verify it's working
        if (Time.frameCount % 120 == 0)
        {
            Debug.Log($"Fear Drain: {fear:F1} (-{drainAmount / Time.deltaTime:F1}/sec)");
        }

        // Check if fear is depleted
        if (fear <= 0f)
        {
            HandleFearDepletion();
        }
    }

    private void HandleFearGainFromPlayer()
    {
        fearGainTimer -= Time.deltaTime;
        if (fearGainTimer <= 0f)
        {
            fearGainTimer = fearGainUpdateInterval;

            // Find nearest player
            FindNearestPlayer();

            if (nearestPlayer != null)
            {
                float distance = Vector3.Distance(transform.position, nearestPlayer.transform.position);

                // Calculate fear gain based on distance (closer = more fear)
                if (distance <= maxFearGainDistance)
                {
                    float distanceFactor = 1f - Mathf.Clamp01((distance - minFearGainDistance) / (maxFearGainDistance - minFearGainDistance));
                    float fearGain = maxFearGainRate * distanceFactor * fearGainUpdateInterval;

                    fear += fearGain;
                    fear = Mathf.Min(fear, 100f);

                    // Reset regeneration when gaining fear from players
                    fearRegenTimer = 0f;
                    isRegenerating = false;

                    // Debug logging
                    if (Time.frameCount % 120 == 0 && fearGain > 0)
                    {
                        Debug.Log($"Fear Gain: +{fearGain:F1} (Distance: {distance:F1})");
                    }
                }
            }
        }
    }

    private void FindNearestPlayer()
    {
        PlayerInputHandler[] players = FindObjectsOfType<PlayerInputHandler>();
        float nearestDistance = float.MaxValue;
        nearestPlayer = null;

        foreach (PlayerInputHandler player in players)
        {
            if (player != null && player.gameObject.activeInHierarchy)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestPlayer = player;
                }
            }
        }
    }

    private void HandleFearDepletion()
    {
        // Auto-exit phase if fear runs out while phasing
        if (isPhasing)
        {
            TogglePhase();
        }

        // Auto-return from clone if controlling one
        if (isControllingClone && GameManager.Instance != null)
        {
            if (GameManager.Instance.HasActiveClone(this))
            {
                GameManager.Instance.ReleaseClone(this);
            }
        }

        // Optional: Add other consequences like temporary inability to use abilities
        Debug.Log("Fear depleted! Ghost abilities limited.");
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

        // 🔥 FIX: Close menu FIRST before doing anything
        ClosePossessionMenu();

        // 🔥 FIX: Check if already controlling clone BEFORE spawning
        if (GameManager.Instance.HasActiveClone(this))
        {
            Debug.LogWarning("Already controlling a clone — ignoring menu input");
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
    }


    // -------------------------------
    // Input Assignment
    // -------------------------------
    public void AssignInput(PlayerInputs actions)
    {
        RemoveInput();
        playerInputs = actions;

        // IMPORTANT: Enable Ghost map FIRST before subscribing
        playerInputs.Ghost.Enable();
        playerInputs.UI.Enable();
        playerInputs.Player.Disable();

        SubscribeInputs();

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

        Debug.Log("Ghost input assigned and enabled");
    }

    public void RemoveInput()
    {
        if (playerInputs == null) return;
        UnsubscribeInputs();
        playerInputs.Ghost.Disable();
        playerInputs.UI.Disable();
        playerInputs = null;
    }


    // -------------------------------
    // Input Subscriptions
    // -------------------------------
    private void SubscribeInputs()
    {
        if (playerInputs == null) return;

        playerInputs.Ghost.Movement.performed += OnMovePerformed;
        playerInputs.Ghost.Movement.canceled += OnMoveCanceled;

        playerInputs.Ghost.Look.performed += OnLookPerformed;
        playerInputs.Ghost.Look.canceled += OnLookCanceled;

        playerInputs.Ghost.PhaseToggle.performed += OnPhaseToggle;
        playerInputs.Ghost.PossessObject.performed += OnPossessObject;

        if (!isControllingClone)
        {
            playerInputs.Ghost.MenuToggle.performed += OnMenuToggle;
        }

        playerInputs.Ghost.FlyUp.performed += OnFlyUpPerformed;
        playerInputs.Ghost.FlyUp.canceled += OnFlyUpCanceled;

        playerInputs.Ghost.FlyDown.performed += OnFlyDownPerformed;
        playerInputs.Ghost.FlyDown.canceled += OnFlyDownCanceled;
        playerInputs.UI.Pause.performed += OnPausePerformed;
    }

    private void UnsubscribeInputs()
    {
        if (playerInputs == null) return;

        playerInputs.Ghost.Movement.performed -= OnMovePerformed;
        playerInputs.Ghost.Movement.canceled -= OnMoveCanceled;

        playerInputs.Ghost.Look.performed -= OnLookPerformed;
        playerInputs.Ghost.Look.canceled -= OnLookCanceled;

        playerInputs.Ghost.PhaseToggle.performed -= OnPhaseToggle;
        playerInputs.Ghost.PossessObject.performed -= OnPossessObject;
        playerInputs.Ghost.MenuToggle.performed -= OnMenuToggle;

        playerInputs.Ghost.FlyUp.performed -= OnFlyUpPerformed;
        playerInputs.Ghost.FlyUp.canceled -= OnFlyUpCanceled;

        playerInputs.Ghost.FlyDown.performed -= OnFlyDownPerformed;
        playerInputs.Ghost.FlyDown.canceled -= OnFlyDownCanceled;
        playerInputs.UI.Pause.performed -= OnPausePerformed;
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

    public void ResetAllInputState()
    {
        moveInput = Vector2.zero;
        lookInput = Vector2.zero;
        verticalInput = 0f;

        // Reset any velocity or movement state
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log("✅ Ghost input state completely reset");
    }

    public void EmergencyInputReset()
    {
        Debug.Log("🔄 EMERGENCY INPUT RESET");

        // Reset all input variables
        moveInput = Vector2.zero;
        lookInput = Vector2.zero;
        verticalInput = 0f;

        // Reset physics
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Reset rotation state
        xRotation = 0f;

        // Reset flags
        freezeInput = false;

        // Force camera reset
        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.identity;
        }
        if (cameraPivot != null)
        {
            cameraPivot.localRotation = Quaternion.identity;
        }

        Debug.Log("✅ Emergency reset complete");
    }

    // -------------------------------
    // Possession Menu Logic
    // -------------------------------
    private void OnMenuToggle(InputAction.CallbackContext ctx)
    {
        // Safety check - if this object is destroyed, don't process input
        if (this == null) return;

        // 🔥 DISABLE MENU BUTTON ENTIRELY while controlling clone/object
        if (isControllingClone)
        {
            Debug.Log("❌ Menu button disabled - currently controlling a clone/object");
            return;
        }

        // 🔥 DOUBLE CHECK with GameManager
        if (GameManager.Instance != null && GameManager.Instance.HasActiveClone(this))
        {
            Debug.Log("❌ Menu button disabled - GameManager reports active clone");
            return;
        }

        GhostUIManager uiManager = GetComponent<GhostUIManager>();
        if (uiManager != null)
        {
            uiManager.TriggerTransformCooldown(0.5f);
        }

        // Normal ghost menu (only when not controlling anything)
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

        FreezeInput(true); // This should prevent movement
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Possession menu opened with " + possessedGameObjects.Count + " objects");
    }

    private void ClosePossessionMenu()
    {
        if (possessionMenu != null)
        {
            possessionMenu.CloseMenu();
            Debug.Log("Possession menu close requested");
        }

        menuOpen = false;

        // 🔥 FIX: Force unfreeze input when closing menu
        FreezeInput(false);

        // 🔥 FIX: Reset cursor state
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("Possession menu closed - input unfrozen");
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
    // 🔥 FIX: Allow movement if EITHER horizontal OR vertical input exists
    bool hasHorizontalInput = moveInput.sqrMagnitude > 0f;
    bool hasVerticalInput = Mathf.Abs(verticalInput) > 0.1f;
    
    if (!hasHorizontalInput && !hasVerticalInput) return;

    Vector3 moveDir = Vector3.zero;

    if (hasHorizontalInput)
    {
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
    }

        if (freezeInput)
        {
            // Ensure all input is zeroed out when frozen
            moveInput = Vector2.zero;
            verticalInput = 0f;
            return;
        }
        Vector3 horizontalMove = moveDir * moveSpeed * Time.fixedDeltaTime;
    Vector3 verticalMove = Vector3.up * verticalInput * flySpeed * Time.fixedDeltaTime;
    
    // 🔥 SEPARATE COLLISION CHECKS: Allow horizontal movement even when blocked vertically
    Vector3 finalMove = Vector3.zero;

    // Check horizontal movement separately
    if (horizontalMove.magnitude > 0)
    {
        if (isPhasing)
        {
            int defaultMask = LayerMask.GetMask(defaultLayerName);
            if (!Physics.CapsuleCast(rb.position + Vector3.up * 0.5f,
                rb.position - Vector3.up * 0.5f, 0.5f,
                horizontalMove.normalized, horizontalMove.magnitude, defaultMask))
            {
                finalMove += horizontalMove;
            }
            else
            {
                // 🔥 Even if blocked, try to slide along the surface
                Vector3 slideDirection = Vector3.ProjectOnPlane(horizontalMove.normalized, Vector3.up).normalized;
                if (!Physics.CapsuleCast(rb.position + Vector3.up * 0.5f,
                    rb.position - Vector3.up * 0.5f, 0.5f,
                    slideDirection, horizontalMove.magnitude, defaultMask))
                {
                    finalMove += slideDirection * horizontalMove.magnitude;
                }
            }
        }
        else
        {
            if (!Physics.CapsuleCast(rb.position + Vector3.up * 0.5f,
                rb.position - Vector3.up * 0.5f, 0.5f,
                horizontalMove.normalized, horizontalMove.magnitude))
            {
                finalMove += horizontalMove;
            }
            else
            {
                // 🔥 Even if blocked, try to slide along the surface
                Vector3 slideDirection = Vector3.ProjectOnPlane(horizontalMove.normalized, Vector3.up).normalized;
                if (!Physics.CapsuleCast(rb.position + Vector3.up * 0.5f,
                    rb.position - Vector3.up * 0.5f, 0.5f,
                    slideDirection, horizontalMove.magnitude))
                {
                    finalMove += slideDirection * horizontalMove.magnitude;
                }
            }
        }
    }

    // Check vertical movement separately - only apply if not blocked
    if (verticalMove.magnitude > 0)
    {
        if (isPhasing)
        {
            int defaultMask = LayerMask.GetMask(defaultLayerName);
            if (!Physics.CapsuleCast(rb.position + Vector3.up * 0.5f,
                rb.position - Vector3.up * 0.5f, 0.5f,
                Vector3.up * Mathf.Sign(verticalInput), Mathf.Abs(verticalMove.magnitude), defaultMask))
            {
                finalMove += verticalMove;
            }
        }
        else
        {
            if (!Physics.CapsuleCast(rb.position + Vector3.up * 0.5f,
                rb.position - Vector3.up * 0.5f, 0.5f,
                Vector3.up * Mathf.Sign(verticalInput), Mathf.Abs(verticalMove.magnitude)))
            {
                finalMove += verticalMove;
            }
        }
    }

    if (finalMove.magnitude > 0)
    {
        rb.MovePosition(rb.position + finalMove);
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

        GhostUIManager uiManager = GetComponent<GhostUIManager>();
        if (uiManager != null)
        {
            uiManager.TriggerPhaseCooldown(1f);
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
                UpdateGhostTracking();
                GhostUIManager uiManager = GetComponent<GhostUIManager>();
                if (uiManager != null)
                {
                    uiManager.TriggerPossessCooldown(1f);
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
            playerInputs.UI.Enable();
        }
        Debug.Log($"FreezeInput called: {freeze}");
    }

    public void UpdateGhostTracking()
    {
        // This method should be called by the GhostController when they start/stop controlling objects
        // to ensure their transform position is accurate for sanity calculations

        // Find all explorers and force a sanity update
        PlayerInputHandler[] explorers = FindObjectsOfType<PlayerInputHandler>();
        foreach (PlayerInputHandler explorer in explorers)
        {
            if (explorer != null)
            {
                explorer.ForceSanityUpdate();
            }
        }

        Debug.Log("Ghost tracking updated - forced sanity recalculation on all explorers");
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

    // Add this method to GhostController
    public void ReturnControlToGhost()
    {
        if (activeClone != null)
        {
            PlayerInputHandler cloneInputHandler = activeClone.GetComponent<PlayerInputHandler>();
            if (cloneInputHandler != null)
            {
                cloneInputHandler.ReleaseControl();
            }
            Destroy(activeClone);
            activeClone = null;
        }

        // Use nuclear option
        NuclearInputReset();
        SetVisibility(true);
        isControllingClone = false;

            UpdateGhostTracking();
        Debug.Log("Control returned to ghost");
    }

    private void OnPausePerformed(InputAction.CallbackContext ctx)
    {
        if (PauseMenuManager.Instance != null && !PauseMenuManager.Instance.IsGamePaused())
        {
            PauseMenuManager.Instance.PauseGame(this);
        }
    }
    public void ForceResetInput()
    {
        Debug.Log("🔄 FORCE RESETTING INPUT");

        // Completely reset all input state
        moveInput = Vector2.zero;
        lookInput = Vector2.zero;
        verticalInput = 0f;
        xRotation = 0f;
        freezeInput = false;

        // Reset physics
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Reset camera
        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.identity;
        }
        if (cameraPivot != null)
        {
            cameraPivot.localRotation = Quaternion.identity;
        }

        // Force re-enable ghost input map
        if (playerInputs != null)
        {
            playerInputs.Ghost.Enable();
            playerInputs.Player.Disable();

            // Re-subscribe to ensure clean state
            RemoveInput();
            SubscribeInputs();
        }

        Debug.Log("✅ Input force reset complete");
    }

    private void NuclearInputReset()
    {
        Debug.Log("☢️ NUCLEAR INPUT RESET");

        // Kill all coroutines
        StopAllCoroutines();

        // Reset ALL input variables
        moveInput = Vector2.zero;
        lookInput = Vector2.zero;
        verticalInput = 0f;
        xRotation = 0f;
        freezeInput = false;
        menuOpen = false;
        isPhasing = false;

        // Reset physics completely
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Reset camera
        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.identity;
            cameraTransform.localPosition = Vector3.zero;
        }
        if (cameraPivot != null)
        {
            cameraPivot.localRotation = Quaternion.identity;
            cameraPivot.localPosition = Vector3.zero;
        }

        // Completely rebuild input system
        if (playerInputs != null)
        {
            // Disable everything first
            playerInputs.Disable();

            // Remove all subscriptions
            UnsubscribeInputs();

            // Re-enable only ghost actions
            playerInputs.Ghost.Enable();
            playerInputs.Player.Disable();

            // Re-subscribe
            SubscribeInputs();
        }

        // Force close any open menus
        if (possessionMenu != null)
        {
            possessionMenu.CloseMenu();
        }

        // Reset cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("✅ Nuclear reset complete");
    }
}
