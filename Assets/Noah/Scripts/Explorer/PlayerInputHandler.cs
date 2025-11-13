using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerInputHandler : MonoBehaviour
{
    [Header("Stamina UI")]
    [SerializeField] private GameObject staminaBarObject; // Drag the entire stamina bar GameObject here
    private Slider staminaSlider;
    private Image staminaFill;
    private CanvasGroup staminaCanvasGroup;

    [Header("Flashlight Battery UI")]
    public Slider batterySlider;

    [Header("Stamina Colors")]
    public Color fullStaminaColor = Color.green;
    public Color lowStaminaColor = Color.red;
    public Color mediumStaminaColor = Color.yellow;
    public float lowStaminaThreshold = 25f;
    public float mediumStaminaThreshold = 60f;

    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 25f; // Stamina drained per second while sprinting
    public float staminaRegenRate = 15f; // Stamina regenerated per second when not sprinting
    public float staminaRegenDelay = 1.5f; // Seconds after stopping sprint before regen starts
    public float minStaminaToSprint = 10f; // Minimum stamina required to start sprinting

    private float currentStamina;
    private float lastSprintTime;
    private bool canSprint = true;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 2f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    [Header("Camera Settings")]
    public Transform cameraTransform;
    public Camera playerCamera;
    public float lookSensitivity = 2f;

    [Header("Phone Settings")]
    public GameObject phonePrefab;
    [HideInInspector]
    public Phone phoneInstance;

    [Header("UI Hint")]
    public SimpleUIHintTMP uiHint;

    //Added for puzzle interaction
    [Header("Interaction Settings")]
    [Tooltip("Max distance for interacting with puzzle cubes")]
    public float interactDistance = 3f;
    [Tooltip("LayerMask for interactable puzzle cubes (optional)")]
    public LayerMask interactLayerMask = ~0; // all layers by default

    private PlayerInputs inputActions;
    private Vector2 moveInput;
    private Vector2 lookInput;

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation = 0f;
    private bool sprinting = false;

    private void Start()
    {
        // Manual assignment if Inspector dragging fails
        if (staminaBarObject == null)
        {
            FindStaminaBarAutomatically();
        }
        InitializeStaminaUI();
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
    private void Awake()
    {
        // Instantiate phone as a child of player
        if (phonePrefab != null && cameraTransform != null)
        {
            GameObject phoneObj = Instantiate(phonePrefab, cameraTransform);
            phoneInstance = phoneObj.GetComponent<Phone>();

            // Set position/rotation to prefab defaults
            phoneObj.transform.localPosition = phonePrefab.transform.localPosition;
            phoneObj.transform.localRotation = phonePrefab.transform.localRotation;

            phoneObj.SetActive(false); // start hidden
            phoneInstance.explorer = this; // assign reference

            // NOTE: Battery UI references must be manually assigned to the Phone instance
            // You'll need to assign these in the Inspector after the phone is instantiated
        }

        controller = GetComponent<CharacterController>();

        currentStamina = maxStamina;
        InitializeStaminaUI();
    }

    private void InitializeStaminaUI()
    {
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

    // Called when ghost transfers control to the clone
    public void TakeControl(PlayerInputs newInputs)
    {
        ReleaseControl();

        inputActions = newInputs;

        // Movement/look/jump/sprint
        inputActions.Player.Movement.performed += OnMovePerformed;
        inputActions.Player.Movement.canceled += OnMoveCanceled;

        inputActions.Player.Look.performed += OnLookPerformed;
        inputActions.Player.Look.canceled += OnLookCanceled;

        inputActions.Player.Jump.performed += OnJumpPerformed;
        inputActions.Player.Sprint.performed += OnSprintPerformed;

        // --- Phone inputs ---
        inputActions.Player.PullOutPhone.performed += ctx =>
        {
            if (phoneInstance != null)
                phoneInstance.TogglePhone();
        };

        {
            if (phoneInstance != null)
                phoneInstance.ToggleFlashlight();
        }
        ;

        //Added: Gamepad interaction (Square / X button)
        inputActions.Player.Interact.performed += OnInteractPerformed;

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

        //Clean up event
        inputActions.Player.Interact.performed -= OnInteractPerformed;

        inputActions.Player.Disable();
        inputActions = null;

        if (playerCamera != null)
            playerCamera.enabled = false;
    }

    #region Input Callbacks
    private void OnMovePerformed(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();
    private void OnMoveCanceled(InputAction.CallbackContext _) => moveInput = Vector2.zero;

    private void OnLookPerformed(InputAction.CallbackContext ctx) => lookInput = ctx.ReadValue<Vector2>();
    private void OnLookCanceled(InputAction.CallbackContext _) => lookInput = Vector2.zero;

    private void OnJumpPerformed(InputAction.CallbackContext _) => Jump();
    private void OnSprintPerformed(InputAction.CallbackContext _)
    {
        // Check if we have enough stamina to sprint
        if (currentStamina >= minStaminaToSprint && canSprint)
        {
            sprinting = !sprinting;
            if (sprinting)
            {
                lastSprintTime = Time.time;
                Debug.Log("Sprinting started");
            }
            else
            {
                Debug.Log("Sprinting stopped");
            }
        }
        else if (sprinting)
        {
            // Stop sprinting if we don't have enough stamina
            sprinting = false;
            Debug.Log("Sprinting stopped - not enough stamina");
        }
        else
        {
            Debug.Log("Cannot start sprinting - not enough stamina");
        }
    }
    #endregion

    private void Update()
    {
        HandleLook();
        HandleMovement();
        HandleHintRaycast();
        HandleStamina();
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
    private void HandleLook()
    {
        transform.Rotate(Vector3.up * lookInput.x * lookSensitivity);

        xRotation -= lookInput.y * lookSensitivity;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }
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
    private void HandleMovement()
    {
        // Calculate speed based on sprinting and stamina
        float speed = moveSpeed;
        if (sprinting && canSprint && currentStamina > 0)
        {
            speed = moveSpeed * sprintMultiplier;
        }
        else if (sprinting && !canSprint)
        {
            // Auto-stop sprinting if we can't sprint but still trying
            sprinting = false;
        }

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

    private void HandleHintRaycast()
    {
        if (cameraTransform == null || uiHint == null) return;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 3f))
        {
            // Check both cube types
            if (hit.collider.GetComponent<CubeChildInteract>() != null ||
                hit.collider.GetComponent<SoundCubeInteract>() != null)
            {
                uiHint.ShowHint("Press □ / X to Interact");
                return;
            }
        }

        uiHint.HideHint();
    }


    //Controller-based puzzle interaction
    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        if (cameraTransform == null) return;

        // Raycast forward from camera to detect puzzle cubes
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayerMask))
        {
            // If hit an interactable cube, trigger its interaction
            CubeChildInteract cube = hit.collider.GetComponent<CubeChildInteract>();
            if (cube != null)
            {
                cube.OnInteract();
                return;
            }
        }
    }
    private void OnDestroy()
    {
        ReleaseControl();
    }
}
