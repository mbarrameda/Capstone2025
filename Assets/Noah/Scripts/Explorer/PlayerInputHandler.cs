using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerInputHandler : MonoBehaviour
{
    // =======================
    // 🔹 UI & STAMINA SYSTEM
    // =======================
    [Header("Stamina UI")]
    [SerializeField] private GameObject staminaBarObject; // Drag the entire stamina bar GameObject here
    private Slider staminaSlider;
    private Image staminaFill;
    private CanvasGroup staminaCanvasGroup;

    [Header("Stamina Colors")]
    public Color fullStaminaColor = Color.green;
    public Color lowStaminaColor = Color.red;
    public Color mediumStaminaColor = Color.yellow;
    public float lowStaminaThreshold = 25f;
    public float mediumStaminaThreshold = 60f;


    [Header("Flashlight Battery UI")]
    public Slider batterySlider;

    public static System.Action<float> OnExplorerSanityChanged;


    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 25f; // drained per sec while sprinting
    public float staminaRegenRate = 15f; // regen per sec
    public float staminaRegenDelay = 1.5f; // delay before regen
    public float minStaminaToSprint = 10f; // min required to sprint

    private float currentStamina;
    private float lastSprintTime;
    private bool canSprint = true;

    [Header("Sanity System")]
    public float maxSanity = 100f;
    public float currentSanity = 0f;
    public float sanityIncreaseRate = 10f;
    public float sanityDecreaseRate = 5f;
    public float sanityUpdateInterval = 0.5f;
    public float sanityEffectDistance = 15f;

    // Add this new field to track if player should have sanity
    private bool shouldUpdateSanity = true;

    [Header("Sanity UI")]
    public Slider sanitySlider;
    public Image sanityFill;
    public Color lowSanityColor = Color.blue;
    public Color mediumSanityColor = Color.yellow;
    public Color highSanityColor = Color.red;
    public float highSanityThreshold = 70f;
    public float mediumSanityThreshold = 30f;

    private float sanityTimer = 0f;
    private CanvasGroup sanityCanvasGroup;
    private GameObject sanityBarObject;

    // =======================
    // 🔹 MOVEMENT & CAMERA
    // =======================
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 2f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    [Header("Camera Settings")]
    public Transform cameraTransform;
    public Camera playerCamera;
    public float lookSensitivity = 2f;

    // =======================
    // 🔹 PHONE & UI HINT
    // =======================
    [Header("Phone Settings")]
    public GameObject phonePrefab;
    [HideInInspector] public Phone phoneInstance;

    [Header("UI Hint")]
    public SimpleUIHintTMP uiHint;

    // =======================
    // 🔹 INTERACTION
    // =======================
    [Header("Interaction Settings")]
    [Tooltip("Max distance for interacting with puzzle cubes")]
    public float interactDistance = 3f;
    [Tooltip("LayerMask for interactable puzzle cubes (optional)")]
    public LayerMask interactLayerMask = ~0; // all layers by default

    // =======================
    // 🔹 PRIVATE FIELDS
    // =======================
    public PlayerInputs inputActions;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation = 0f;
    private bool sprinting = false;

    public bool shouldFindUI = true;
    private bool isRealExplorer = true;



    [Header("Sanity Game Over")]
    public string ghostWinSceneName = "GhostWinScene"; // Name of the scene to load when sanity reaches 100
    private bool hasTriggeredGameOver = false;

    // =======================
    // 🔹 UNITY EVENTS
    // =======================
    private void Awake()
    {
        // Find hint text automatically
        if (uiHint == null)
            uiHint = FindObjectOfType<SimpleUIHintTMP>();

        // Only instantiate phone for real explorers, not clones
        if (shouldFindUI && phonePrefab != null && cameraTransform != null)
        {
            GameObject phoneObj = Instantiate(phonePrefab, cameraTransform);
            phoneInstance = phoneObj.GetComponent<Phone>();

            phoneObj.transform.localPosition = phonePrefab.transform.localPosition;
            phoneObj.transform.localRotation = phonePrefab.transform.localRotation;

            phoneObj.SetActive(false); // start hidden
            phoneInstance.explorer = this; // assign reference
        }

        controller = GetComponent<CharacterController>();
        currentStamina = maxStamina;

        // Only initialize stamina UI if this is a real explorer
        if (shouldFindUI)
        {
            InitializeStaminaUI();
        }
    }

    private void Start()
    {
        currentSanity = maxSanity;

        if (staminaBarObject == null)
        {
            FindStaminaBarAutomatically();
        }

        if (shouldFindUI)
        {
            InitializeStaminaUI();
            InitializeSanityUI();
        }

        isRealExplorer = GameManager.Instance != null && GameManager.Instance.explorers.Contains(this);
    }

    private void Update()
    {
        HandleLook();
        HandleMovement();
        HandleHintRaycast();
        HandleStamina();
        HandleSanity();
    }

    private void OnDestroy()
    {
        ReleaseControl();
    }

    public void SetAsClone()
    {
        shouldFindUI = false;
        isRealExplorer = false;

        // Don't try to find or control any UI elements
        sanitySlider = null;
        sanityBarObject = null;
        staminaSlider = null;
        staminaBarObject = null;
        batterySlider = null;

        // Disable phone functionality for clones
        if (phoneInstance != null)
        {
            phoneInstance.gameObject.SetActive(false);
            phoneInstance.enabled = false;
        }

        Debug.Log("Set as clone - UI and phone disabled");
    }

    // =======================
    // 🔹 SANITY SETTINGS
    // =======================
    private void InitializeSanityUI()
    {
        if (!shouldFindUI) return;

        // Try to find sanity bar automatically
        FindSanityBarAutomatically();

        if (sanitySlider != null)
        {
            sanitySlider.minValue = 0f;
            sanitySlider.maxValue = maxSanity;
            sanitySlider.value = currentSanity;

            // Get or add canvas group
            sanityCanvasGroup = sanitySlider.GetComponent<CanvasGroup>();
            if (sanityCanvasGroup == null)
            {
                sanityCanvasGroup = sanitySlider.gameObject.AddComponent<CanvasGroup>();
            }

            // 🔥 CHANGE: Always visible, no fading
            sanityCanvasGroup.alpha = 1f;
            sanitySlider.gameObject.SetActive(true);

            Debug.Log("Sanity UI initialized successfully - always visible");
        }
        else
        {
            Debug.LogError("Failed to initialize sanity UI");
        }
    }

    private void FindSanityBarAutomatically()
    {
        // Try multiple common names for the sanity bar
        string[] possibleNames = { "SanityBar", "Sanity", "SanitySlider", "Sanity Bar" };

        foreach (string name in possibleNames)
        {
            GameObject found = GameObject.Find(name);
            if (found != null)
            {
                sanityBarObject = found;
                sanitySlider = found.GetComponent<Slider>();
                if (sanitySlider != null)
                {
                    Debug.Log($"Found sanity bar: {name}");

                    // Try to find the fill image automatically
                    FindSanityFillAutomatically();
                    return;
                }
            }
        }

        // If not found, search all canvases
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            Slider[] sliders = canvas.GetComponentsInChildren<Slider>();
            foreach (Slider slider in sliders)
            {
                if (slider.name.Contains("Sanity") || slider.name.Contains("sanity"))
                {
                    sanityBarObject = slider.gameObject;
                    sanitySlider = slider;
                    Debug.Log($"Found sanity bar in canvas: {slider.name}");

                    // Try to find the fill image automatically
                    FindSanityFillAutomatically();
                    return;
                }
            }
        }

        Debug.Log("Could not find sanity bar automatically. It will be created when needed.");
    }

    private void FindSanityFillAutomatically()
    {
        if (sanitySlider == null || sanityFill != null) return;

        // Common paths for slider fill
        Transform fillArea = sanitySlider.transform.Find("Fill Area");
        if (fillArea != null)
        {
            Transform fill = fillArea.Find("Fill");
            if (fill != null)
            {
                sanityFill = fill.GetComponent<Image>();
            }
        }

        // Alternative path
        if (sanityFill == null)
        {
            sanityFill = sanitySlider.fillRect?.GetComponent<Image>();
        }

        if (sanityFill != null)
        {
            Debug.Log("Found sanity fill image automatically");
        }
    }

    private void HandleSanity()
    {


        if (!shouldUpdateSanity)
        {
            if (currentSanity != maxSanity && !IsRealExplorer())
            {
                currentSanity = maxSanity;
                UpdateSanityUI();
            }
            return;
        }

        sanityTimer -= Time.deltaTime;
        if (sanityTimer <= 0f)
        {
            sanityTimer = sanityUpdateInterval;

            GhostController nearestGhost = FindNearestActiveGhost();

            if (nearestGhost != null)
            {
                float distance = Vector3.Distance(transform.position, nearestGhost.transform.position);

                if (distance <= sanityEffectDistance)
                {
                    // 👻 NEAR A GHOST → SANITY DROPS
                    float distanceFactor = 1f - (distance / sanityEffectDistance);
                    float sanityLoss = sanityIncreaseRate * distanceFactor * sanityUpdateInterval;

                    currentSanity -= sanityLoss;
                    currentSanity = Mathf.Max(currentSanity, 0f);

                    if (Time.frameCount % 120 == 0)
                        Debug.Log($"Sanity Loss: -{sanityLoss:F1} (Distance: {distance:F1})");
                }
                else
                {
                    // 🧠 FAR AWAY → SANITY REGENERATES
                    float sanityRegen = sanityDecreaseRate * sanityUpdateInterval;

                    currentSanity += sanityRegen;
                    currentSanity = Mathf.Min(currentSanity, maxSanity);
                }
            }
            else
            {
                // No ghosts active → regenerate
                float sanityRegen = sanityDecreaseRate * sanityUpdateInterval;
                currentSanity += sanityRegen;
                currentSanity = Mathf.Min(currentSanity, maxSanity);
            }

            CheckSanityGameOver();
            UpdateSanityUI();
        }

        if (sanitySlider != null && !sanitySlider.gameObject.activeInHierarchy)
            sanitySlider.gameObject.SetActive(true);
    }


    private GhostController FindNearestActiveGhost()
    {
        if (GameManager.Instance != null && GameManager.Instance.ghosts != null)
        {
            float nearestDistance = float.MaxValue;
            GhostController nearestGhost = null;

            foreach (GhostController ghost in GameManager.Instance.ghosts)
            {
                // Use the updated check that allows object-controlling ghosts
                bool isInactive = ghost == null ||
                    !ghost.gameObject.activeInHierarchy ||
                    !ghost.enabled ||
                    IsGhostFrozenOrInactive(ghost);

                if (isInactive)
                {
                    continue;
                }

                // 🔥 IMPORTANT: Use the ghost's CURRENT position, even if they're controlling an object
                // The ghost transform should follow the object they're controlling
                float distance = Vector3.Distance(transform.position, ghost.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestGhost = ghost;
                }
            }

            if (nearestGhost != null && Time.frameCount % 120 == 0)
            {
                Debug.Log($"🧠 Nearest active ghost: {nearestGhost.name} at {nearestDistance:F1} units. " +
                         $"ControllingClone: {nearestGhost.isControllingClone}");
            }

            return nearestGhost;
        }

        return null;
    }

    private void CheckSanityGameOver()
    {
        // Check if sanity reached 100% and we haven't already triggered game over
        if (currentSanity == 0f && !hasTriggeredGameOver)
        {
            hasTriggeredGameOver = true;

            
            // Load the ghost win scene
            if (!string.IsNullOrEmpty(ghostWinSceneName))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(ghostWinSceneName);
            }
            else
            {
                Debug.LogError("Ghost win scene name is not set in PlayerInputHandler!");
            }
        }
    }
    private bool IsGhostFrozenOrInactive(GhostController ghost)
    {
        if (ghost == null) return true;

        // 🔥 FIX: Ghosts controlling objects should STILL affect sanity
        // Only consider ghosts inactive if they're actually disabled or in a state where they shouldn't affect sanity

        // Check if ghost is stunned - stunned ghosts don't affect sanity
        if (ghost.isStunned)
            return true;

        // Check if ghost is completely disabled
        if (!ghost.gameObject.activeInHierarchy || !ghost.enabled)
            return true;

        // 🔥 FIX: Ghosts controlling clones OR possessing objects should STILL affect sanity
        // The key insight: when a ghost is controlling something (clone or object), 
        // they're still "active" in the world and should affect sanity

        // Check if ghost is temporarily frozen (like during menu) but not permanently disabled
        if (ghost.freezeInput && !ghost.menuOpen)
        {
            // Only return true if this is a permanent freeze, not temporary
            return true;
        }

        // If we get here, the ghost is active and should affect sanity
        return false;
    }
    private GhostController FindNearestGhost()
    {

        if (GameManager.Instance != null && GameManager.Instance.ghosts != null)
        {
            float nearestDistance = float.MaxValue;
            GhostController nearestGhost = null;

            foreach (GhostController ghost in GameManager.Instance.ghosts)
            {
                if (ghost == null || !ghost.gameObject.activeInHierarchy) continue;

                // Always track the original ghost player's position
                // Even when they're controlling clones, the original ghost object
                // still exists in the scene (just hidden)
                float distance = Vector3.Distance(transform.position, ghost.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestGhost = ghost;
                }
            }

            return nearestGhost;
        }

        return null;
    }

    public void SetSanityActive(bool active)
    {
        shouldUpdateSanity = active;

        Debug.Log($"Sanity system {(active ? "enabled" : "disabled")}");
    }
    public void ForceSanityUpdate()
    {
        // Force an immediate sanity check
        sanityTimer = 0f;
        HandleSanity();

        Debug.Log($"🧠 Forced sanity update - Current: {currentSanity}");
    }
    private void UpdateSanityUI()
    {
        if (sanitySlider == null) return;

        sanitySlider.value = currentSanity;

        if (sanityFill != null)
        {
            if (currentSanity >= highSanityThreshold)
                sanityFill.color = highSanityColor;
            else if (currentSanity >= mediumSanityThreshold)
                sanityFill.color = mediumSanityColor;
            else
                sanityFill.color = lowSanityColor;
        }

        // 🔥 Send update to ghosts
        OnExplorerSanityChanged?.Invoke(currentSanity);
    }


    // Public method to modify sanity (for external effects)
    public void ModifySanity(float amount)
    {
        currentSanity = Mathf.Clamp(currentSanity + amount, 0f, maxSanity);
        if (amount > 0) // Only check if sanity is increasing
        {
            CheckSanityGameOver();
        }

        UpdateSanityUI();

        Debug.Log($"Sanity modified: {amount}. Current: {currentSanity}");
    }

    private bool IsRealExplorer()
    {
        // Real explorers are the ones spawned initially, not clones
        // You might need to add a flag to track this, or check if this is in the GameManager's explorers list
        if (GameManager.Instance != null && GameManager.Instance.explorers != null)
        {
            return GameManager.Instance.explorers.Contains(this);
        }
        return false;
    }

    // =======================
    // 🔹 CONTROL HANDLING
    // =======================
    public void TakeControl(PlayerInputs newInputs)
    {
        ReleaseControl();
        inputActions = newInputs;
        inputActions.Player.Movement.performed += OnMovePerformed;
        inputActions.Player.Movement.canceled += OnMoveCanceled;

        inputActions.Player.Look.performed += OnLookPerformed;
        inputActions.Player.Look.canceled += OnLookCanceled;

        inputActions.Player.Jump.performed += OnJumpPerformed;
        inputActions.Player.Sprint.performed += OnSprintPerformed;

        inputActions.Player.PullOutPhone.performed += ctx =>
        {
            if (phoneInstance != null)
                phoneInstance.TogglePhone();
        };

        inputActions.Player.Interact.performed += OnInteractPerformed;
        inputActions.UI.Pause.performed += OnPausePerformed;
        inputActions.Player.Enable();
        inputActions.Ghost.Disable();

        if (playerCamera != null)
            playerCamera.enabled = true;
    }

    public void ReleaseControl()
    {
        if (inputActions == null) return;
        inputActions.Player.Movement.performed -= OnMovePerformed;
        inputActions.Player.Movement.canceled -= OnMoveCanceled;
        inputActions.Player.Look.performed -= OnLookPerformed;
        inputActions.Player.Look.canceled -= OnLookCanceled;
        inputActions.Player.Jump.performed -= OnJumpPerformed;
        inputActions.Player.Sprint.performed -= OnSprintPerformed;
        inputActions.Player.Interact.performed -= OnInteractPerformed;
        inputActions.UI.Pause.performed -= OnPausePerformed;
        inputActions.Player.Disable();
        inputActions = null;

        if (playerCamera != null)
            playerCamera.enabled = false;
    }

    private void OnPausePerformed(InputAction.CallbackContext ctx)
    {
        if (PauseMenuManager.Instance != null && !PauseMenuManager.Instance.IsGamePaused())
        {
            PauseMenuManager.Instance.PauseGame(this);
        }
    }
    public void FreezeInput(bool freeze)
    {
        if (freeze)
        {
            // Store current input state and zero it out
            moveInput = Vector2.zero;
            lookInput = Vector2.zero;
            sprinting = false;

            // Also freeze the character controller
            if (controller != null)
            {
                velocity = Vector3.zero;
                inputActions.UI.Enable();
            }
        }
        // Note: We don't re-enable input here because the input callbacks will handle it automatically
        // when the game is unpaused and player starts moving again

        Debug.Log($"Player input {(freeze ? "frozen" : "unfrozen")}");
    }

    // =======================
    // 🔹 INPUT CALLBACKS
    // =======================
    private void OnMovePerformed(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();
    private void OnMoveCanceled(InputAction.CallbackContext _) => moveInput = Vector2.zero;
    private void OnLookPerformed(InputAction.CallbackContext ctx) => lookInput = ctx.ReadValue<Vector2>();
    private void OnLookCanceled(InputAction.CallbackContext _) => lookInput = Vector2.zero;
    private void OnJumpPerformed(InputAction.CallbackContext _) => Jump();

    private void OnSprintPerformed(InputAction.CallbackContext _)
    {
        if (currentStamina >= minStaminaToSprint && canSprint)
        {
            sprinting = !sprinting;
            if (sprinting)
                lastSprintTime = Time.time;
        }
        else if (sprinting)
        {
            sprinting = false;
        }
    }

    // =======================
    // 🔹 STAMINA SYSTEM
    // =======================

    public void ModifyStamina(float amount)
    {
        currentStamina = Mathf.Clamp(currentStamina + amount, 0f, maxStamina);

        // Update sprinting status if stamina drops too low
        if (currentStamina < minStaminaToSprint && sprinting)
        {
            sprinting = false;
            Debug.Log("Forced to stop sprinting due to stamina modification");
        }

        // Update canSprint flag
        canSprint = currentStamina >= minStaminaToSprint;

        // 🔥 Update UI immediately
        UpdateStaminaUI();
    }
    private void InitializeStaminaUI()
    {
        if (!shouldFindUI) return;

        if (staminaBarObject == null)
        {
            // Try to find it automatically
            staminaBarObject = GameObject.Find("StaminaBar");
            if (staminaBarObject == null)
            {
                Debug.LogWarning("Stamina bar not found - please assign in Inspector");
                return;
            }
        }

        staminaSlider = staminaBarObject.GetComponent<Slider>();
        if (staminaSlider == null)
        {
            Debug.LogError("StaminaBarObject doesn't have Slider component!");
            return;
        }

        // Set up slider values
        staminaSlider.minValue = 0f;
        staminaSlider.maxValue = maxStamina;
        staminaSlider.value = currentStamina;

        // Find the fill image automatically
        if (staminaFill == null)
        {
            // Common paths for slider fill
            Transform fillArea = staminaSlider.transform.Find("Fill Area");
            if (fillArea != null)
            {
                Transform fill = fillArea.Find("Fill");
                if (fill != null)
                {
                    staminaFill = fill.GetComponent<Image>();
                }
            }

            // Alternative path
            if (staminaFill == null)
            {
                staminaFill = staminaSlider.fillRect?.GetComponent<Image>();
            }
        }

        // Get or add canvas group
        staminaCanvasGroup = staminaBarObject.GetComponent<CanvasGroup>();
        if (staminaCanvasGroup == null)
        {
            staminaCanvasGroup = staminaBarObject.AddComponent<CanvasGroup>();
        }

        // Hide initially if full
        if (currentStamina >= maxStamina)
        {
            staminaCanvasGroup.alpha = 0f;
        }

        Debug.Log("Stamina UI initialized successfully");
    }
    private void HandleStamina()
    {
        if (sprinting && moveInput != Vector2.zero)
        {
            // Drain stamina while sprinting and moving
            currentStamina -= staminaDrainRate * Time.deltaTime;
            lastSprintTime = Time.time;

            // Auto-stop sprinting when stamina is depleted
            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                sprinting = false;
                canSprint = false;
                Debug.Log("Stamina depleted - auto-stop sprinting");
            }

            // Update canSprint flag
            canSprint = currentStamina >= minStaminaToSprint;
        }
        else
        {
            // Regenerate stamina after a delay
            if (Time.time - lastSprintTime >= staminaRegenDelay)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Min(currentStamina, maxStamina);

                // Re-enable sprinting when we have enough stamina
                if (currentStamina >= minStaminaToSprint)
                {
                    canSprint = true;
                }
            }
        }

        // 🔥 Update stamina UI
        UpdateStaminaUI();
    }

    private void UpdateStaminaUI()
    {
        if (staminaSlider == null) return;

        // Update slider value
        staminaSlider.value = currentStamina;

        // Update fill color based on stamina percentage
        if (staminaFill != null)
        {
            float staminaPercent = (currentStamina / maxStamina) * 100f;

            if (staminaPercent <= lowStaminaThreshold)
            {
                staminaFill.color = lowStaminaColor;
            }
            else if (staminaPercent <= mediumStaminaThreshold)
            {
                staminaFill.color = mediumStaminaColor;
            }
            else
            {
                staminaFill.color = fullStaminaColor;
            }
        }

        // Handle visibility - show when not full, hide when full and not recently used
        if (staminaCanvasGroup != null)
        {
            float targetAlpha = 1f;

            if (currentStamina >= maxStamina && Time.time - lastSprintTime > 3f)
            {
                // Hide when full and not used for 3 seconds
                targetAlpha = 0f;
            }
            else if (currentStamina < maxStamina || sprinting)
            {
                // Show when not full or when sprinting
                targetAlpha = 1f;
            }

            // Smooth fade
            staminaCanvasGroup.alpha = Mathf.Lerp(staminaCanvasGroup.alpha, targetAlpha, Time.deltaTime * 5f);
        }
    }

    // =======================
    // 🔹 MOVEMENT & LOOK
    // =======================
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
        float speed = sprinting && canSprint && currentStamina > 0
            ? moveSpeed * sprintMultiplier
            : moveSpeed;

        Vector3 move = transform.forward * moveInput.y + transform.right * moveInput.x;
        controller.Move(move * speed * Time.deltaTime);

        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void Jump()
    {
        if (controller.isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    // =======================
    // 🔹 INTERACTION
    // =======================
    private void HandleHintRaycast()
    {
        if (cameraTransform == null || uiHint == null) return;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 3f))
        {
            if (hit.collider.GetComponent<CubeChildInteract>() != null)
            {
                uiHint.ShowHint("Press □ / X to Interact");
                return;
            }
        }

        uiHint.HideHint();
    }

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        if (cameraTransform == null) return;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayerMask))
        {
            CubeChildInteract cube = hit.collider.GetComponent<CubeChildInteract>();
            if (cube != null)
            {
                cube.OnInteract();
                return;
            }
        }
    }


    private void FindStaminaBarAutomatically()
    {
        // Try multiple common names
        string[] possibleNames = { "StaminaBar", "Stamina", "StaminaSlider", "Stamina Bar" };

        foreach (string name in possibleNames)
        {
            GameObject found = GameObject.Find(name);
            if (found != null)
            {
                staminaBarObject = found;
                Debug.Log($"Found stamina bar: {name}");
                return;
            }
        }

        // Search all canvases
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            Slider[] sliders = canvas.GetComponentsInChildren<Slider>();
            foreach (Slider slider in sliders)
            {
                if (slider.name.Contains("Stamina") || slider.name.Contains("stamina"))
                {
                    staminaBarObject = slider.gameObject;
                    Debug.Log($"Found stamina bar in canvas: {slider.name}");
                    return;
                }
            }
        }

        Debug.LogError("Could not find stamina bar automatically. Please assign manually in Inspector.");
    }
    public static event System.Action OnInteractPressed;

    public void Interact(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnInteractPressed?.Invoke(); // Notify all interactable objects
        }
    }

}
