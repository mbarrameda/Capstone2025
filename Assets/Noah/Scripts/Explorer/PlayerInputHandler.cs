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
    [SerializeField] private GameObject staminaBarObject;
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
    public float staminaDrainRate = 25f;
    public float staminaRegenRate = 15f;
    public float staminaRegenDelay = 1.5f;
    public float minStaminaToSprint = 10f;

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
    // 🔹 INTERACTION & PUZZLE
    // =======================
    [Header("Interaction Settings")]
    public float interactDistance = 3f;
    public LayerMask interactLayerMask = ~0;

    [Header("Artifact Puzzle Settings")]
    public bool isCarryingArtifact = false;
    [TextArea] public string artifactSwapMessage = "i need to keep down the one i have on the alter";

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
    private BookInteract currentTargetBook;

    [Header("Sanity Game Over")]
    public string ghostWinSceneName = "GhostWinScene";
    private bool hasTriggeredGameOver = false;

    // =======================
    // 🔹 UNITY EVENTS
    // =======================
    private void Awake()
    {
        if (uiHint == null)
            uiHint = FindObjectOfType<SimpleUIHintTMP>();

        if (shouldFindUI && phonePrefab != null && cameraTransform != null)
        {
            GameObject phoneObj = Instantiate(phonePrefab, cameraTransform);
            phoneInstance = phoneObj.GetComponent<Phone>();

            phoneObj.transform.localPosition = phonePrefab.transform.localPosition;
            phoneObj.transform.localRotation = phonePrefab.transform.localRotation;

            phoneObj.SetActive(false);
            phoneInstance.explorer = this;
        }

        controller = GetComponent<CharacterController>();
        currentStamina = maxStamina;

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
        HandleBookRaycast();
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
        sanitySlider = null;
        sanityBarObject = null;
        staminaSlider = null;
        staminaBarObject = null;
        batterySlider = null;

        if (phoneInstance != null)
        {
            phoneInstance.gameObject.SetActive(false);
            phoneInstance.enabled = false;
        }
    }

    // =======================
    // 🔹 SANITY SETTINGS
    // =======================

    // This fixes the error in GhostController.cs
    public void ForceSanityUpdate()
    {
        sanityTimer = 0f;
        HandleSanity();
        Debug.Log("Sanity update forced by Ghost tracking change.");
    }

    private void InitializeSanityUI()
    {
        if (!shouldFindUI) return;
        FindSanityBarAutomatically();

        if (sanitySlider != null)
        {
            sanitySlider.minValue = 0f;
            sanitySlider.maxValue = maxSanity;
            sanitySlider.value = currentSanity;

            sanityCanvasGroup = sanitySlider.GetComponent<CanvasGroup>();
            if (sanityCanvasGroup == null)
                sanityCanvasGroup = sanitySlider.gameObject.AddComponent<CanvasGroup>();

            sanityCanvasGroup.alpha = 1f;
            sanitySlider.gameObject.SetActive(true);
        }
    }

    private void FindSanityBarAutomatically()
    {
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
                    FindSanityFillAutomatically();
                    return;
                }
            }
        }
    }

    private void FindSanityFillAutomatically()
    {
        if (sanitySlider == null || sanityFill != null) return;
        Transform fillArea = sanitySlider.transform.Find("Fill Area");
        if (fillArea != null)
        {
            Transform fill = fillArea.Find("Fill");
            if (fill != null) sanityFill = fill.GetComponent<Image>();
        }
        if (sanityFill == null) sanityFill = sanitySlider.fillRect?.GetComponent<Image>();
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
                    float distanceFactor = 1f - (distance / sanityEffectDistance);
                    float sanityLoss = sanityIncreaseRate * distanceFactor * sanityUpdateInterval;
                    currentSanity -= sanityLoss;
                    currentSanity = Mathf.Max(currentSanity, 0f);
                }
                else
                {
                    float sanityRegen = sanityDecreaseRate * sanityUpdateInterval;
                    currentSanity += sanityRegen;
                    currentSanity = Mathf.Min(currentSanity, maxSanity);
                }
            }
            else
            {
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
                bool isInactive = ghost == null || !ghost.gameObject.activeInHierarchy || !ghost.enabled || IsGhostFrozenOrInactive(ghost);
                if (isInactive) continue;

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

    private void CheckSanityGameOver()
    {
        if (currentSanity == 0f && !hasTriggeredGameOver)
        {
            hasTriggeredGameOver = true;
            if (!string.IsNullOrEmpty(ghostWinSceneName))
                UnityEngine.SceneManagement.SceneManager.LoadScene(ghostWinSceneName);
        }
    }

    private bool IsGhostFrozenOrInactive(GhostController ghost)
    {
        if (ghost == null) return true;
        if (ghost.isStunned) return true;
        if (!ghost.gameObject.activeInHierarchy || !ghost.enabled) return true;
        if (ghost.freezeInput && !ghost.menuOpen) return true;
        return false;
    }

    public void SetSanityActive(bool active) => shouldUpdateSanity = active;

    private void UpdateSanityUI()
    {
        if (sanitySlider == null) return;
        sanitySlider.value = currentSanity;
        if (sanityFill != null)
        {
            if (currentSanity >= highSanityThreshold) sanityFill.color = highSanityColor;
            else if (currentSanity >= mediumSanityThreshold) sanityFill.color = mediumSanityColor;
            else sanityFill.color = lowSanityColor;
        }
        OnExplorerSanityChanged?.Invoke(currentSanity);
    }

    public void ModifySanity(float amount)
    {
        currentSanity = Mathf.Clamp(currentSanity + amount, 0f, maxSanity);
        if (amount > 0) CheckSanityGameOver();
        UpdateSanityUI();
    }

    private bool IsRealExplorer()
    {
        if (GameManager.Instance != null && GameManager.Instance.explorers != null)
            return GameManager.Instance.explorers.Contains(this);
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
        inputActions.Player.PullOutPhone.performed += ctx => { if (phoneInstance != null) phoneInstance.TogglePhone(); };
        inputActions.Player.Interact.performed += OnInteractPerformed;
        inputActions.UI.Pause.performed += OnPausePerformed;
        inputActions.Player.Enable();
        inputActions.Ghost.Disable();
        if (playerCamera != null) playerCamera.enabled = true;
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
        if (playerCamera != null) playerCamera.enabled = false;
    }

    private void OnPausePerformed(InputAction.CallbackContext ctx)
    {
        if (PauseMenuManager.Instance != null && !PauseMenuManager.Instance.IsGamePaused())
            PauseMenuManager.Instance.PauseGame(this);
    }

    public void FreezeInput(bool freeze)
    {
        if (freeze)
        {
            moveInput = Vector2.zero;
            lookInput = Vector2.zero;
            sprinting = false;
            if (controller != null)
            {
                velocity = Vector3.zero;
                inputActions.UI.Enable();
            }
        }
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
            if (sprinting) lastSprintTime = Time.time;
        }
        else if (sprinting) sprinting = false;
    }

    // =======================
    // 🔹 STAMINA SYSTEM
    // =======================
    public void ModifyStamina(float amount)
    {
        currentStamina = Mathf.Clamp(currentStamina + amount, 0f, maxStamina);
        if (currentStamina < minStaminaToSprint && sprinting) sprinting = false;
        canSprint = currentStamina >= minStaminaToSprint;
        UpdateStaminaUI();
    }

    private void InitializeStaminaUI()
    {
        if (!shouldFindUI) return;
        if (staminaBarObject == null) staminaBarObject = GameObject.Find("StaminaBar");
        if (staminaBarObject == null) return;

        staminaSlider = staminaBarObject.GetComponent<Slider>();
        if (staminaSlider == null) return;

        staminaSlider.minValue = 0f;
        staminaSlider.maxValue = maxStamina;
        staminaSlider.value = currentStamina;

        if (staminaFill == null)
        {
            Transform fillArea = staminaSlider.transform.Find("Fill Area");
            if (fillArea != null)
            {
                Transform fill = fillArea.Find("Fill");
                if (fill != null) staminaFill = fill.GetComponent<Image>();
            }
            if (staminaFill == null) staminaFill = staminaSlider.fillRect?.GetComponent<Image>();
        }

        staminaCanvasGroup = staminaBarObject.GetComponent<CanvasGroup>();
        if (staminaCanvasGroup == null) staminaCanvasGroup = staminaBarObject.AddComponent<CanvasGroup>();
        if (currentStamina >= maxStamina) staminaCanvasGroup.alpha = 0f;
    }

    private void HandleStamina()
    {
        if (sprinting && moveInput != Vector2.zero)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            lastSprintTime = Time.time;
            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                sprinting = false;
                canSprint = false;
            }
            canSprint = currentStamina >= minStaminaToSprint;
        }
        else
        {
            if (Time.time - lastSprintTime >= staminaRegenDelay)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Min(currentStamina, maxStamina);
                if (currentStamina >= minStaminaToSprint) canSprint = true;
            }
        }
        UpdateStaminaUI();
    }

    private void UpdateStaminaUI()
    {
        if (staminaSlider == null) return;
        staminaSlider.value = currentStamina;
        if (staminaFill != null)
        {
            float staminaPercent = (currentStamina / maxStamina) * 100f;
            if (staminaPercent <= lowStaminaThreshold) staminaFill.color = lowStaminaColor;
            else if (staminaPercent <= mediumStaminaThreshold) staminaFill.color = mediumStaminaColor;
            else staminaFill.color = fullStaminaColor;
        }
        if (staminaCanvasGroup != null)
        {
            float targetAlpha = (currentStamina >= maxStamina && Time.time - lastSprintTime > 3f) ? 0f : 1f;
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
        if (cameraTransform != null) cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    private void HandleMovement()
    {
        float speed = sprinting && canSprint && currentStamina > 0 ? moveSpeed * sprintMultiplier : moveSpeed;
        Vector3 move = transform.forward * moveInput.y + transform.right * moveInput.x;
        controller.Move(move * speed * Time.deltaTime);
        if (controller.isGrounded && velocity.y < 0) velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void Jump()
    {
        if (controller.isGrounded) velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    // =======================
    // 🔹 INTERACTION 
    // =======================
    private void HandleBookRaycast()
    {
        if (cameraTransform == null) return;
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, interactDistance, interactLayerMask))
        {
            BookInteract book = hit.collider.GetComponentInParent<BookInteract>();
            if (book != null)
            {
                if (currentTargetBook != book)
                {
                    if (currentTargetBook != null) currentTargetBook.HideHover();
                    currentTargetBook = book;
                    currentTargetBook.ShowHover();
                }
                return;
            }
        }
        if (currentTargetBook != null)
        {
            currentTargetBook.HideHover();
            currentTargetBook = null;
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        // 1. Existing Book Interaction
        if (currentTargetBook != null)
        {
            currentTargetBook.OnInteract();
            currentTargetBook = null;
            return;
        }

        // 2. Artifact and Altar Interaction
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, interactDistance, interactLayerMask))
        {
            // Check for Artifact Pickup
            ArtifactItem artifact = hit.collider.GetComponent<ArtifactItem>();
            if (artifact != null)
            {
                artifact.Pickup(this);
                return;
            }

            // Check for Altar Deposit
            ArtifactAltar altar = hit.collider.GetComponent<ArtifactAltar>();
            if (altar != null)
            {
                altar.PlaceArtifact(this);
                return;
            }
        }

        OnInteractPressed?.Invoke();
    }

    private void FindStaminaBarAutomatically()
    {
        string[] possibleNames = { "StaminaBar", "Stamina", "StaminaSlider", "Stamina Bar" };
        foreach (string name in possibleNames)
        {
            GameObject found = GameObject.Find(name);
            if (found != null) { staminaBarObject = found; return; }
        }
    }

    public static event System.Action OnInteractPressed;
}