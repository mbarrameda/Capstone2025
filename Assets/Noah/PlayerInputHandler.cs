using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerInputHandler : MonoBehaviour
{
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

    private PlayerInputs inputActions;
    private Vector2 moveInput;
    private Vector2 lookInput;

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation = 0f;
    private bool sprinting = false;

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
        }

        controller = GetComponent<CharacterController>();
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

        inputActions.Player.Flashlight.performed += ctx =>
        {
            if (phoneInstance != null)
                phoneInstance.ToggleFlashlight();
        };

        inputActions.Enable();

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

        inputActions.Disable();
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
    private void OnSprintPerformed(InputAction.CallbackContext _) => sprinting = !sprinting;
    #endregion

    private void Update()
    {
        HandleLook();
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
        float speed = sprinting ? moveSpeed * sprintMultiplier : moveSpeed;
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
}
