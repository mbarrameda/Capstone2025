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

    private PlayerInputs inputActions;
    private Vector2 moveInput;
    private Vector2 lookInput;

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation = 0f;
    private bool sprinting = false;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    // Called when ghost transfers control to the player
    public void TakeControl(PlayerInputs newInputs)
    {
        ReleaseControl(); // clear any old bindings
        inputActions = newInputs;
        SubscribeInputs();
        inputActions.Enable();

        if (playerCamera != null)
            playerCamera.enabled = true;
    }

    public void ReleaseControl()
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

        inputActions.Player.Jump.performed += _ => Jump();
        inputActions.Player.Sprint.performed += _ => sprinting = !sprinting;

        // Possess handled by ghost, so no action here
        inputActions.Player.Possess.performed += _ => { };
    }

    private void UnsubscribeInputs()
    {
        if (inputActions == null) return;

        inputActions.Player.Movement.performed -= OnMovePerformed;
        inputActions.Player.Movement.canceled -= OnMoveCanceled;

        inputActions.Player.Look.performed -= OnLookPerformed;
        inputActions.Player.Look.canceled -= OnLookCanceled;

        // No need to unsubscribe lambdas, but good practice:
        inputActions.Player.Jump.performed -= _ => Jump();
        inputActions.Player.Sprint.performed -= _ => sprinting = !sprinting;
        inputActions.Player.Possess.performed -= _ => { };
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();
    private void OnMoveCanceled(InputAction.CallbackContext _) => moveInput = Vector2.zero;

    private void OnLookPerformed(InputAction.CallbackContext ctx) => lookInput = ctx.ReadValue<Vector2>();
    private void OnLookCanceled(InputAction.CallbackContext _) => lookInput = Vector2.zero;

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
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    public void ForceDisableCamera()
    {
        if (playerCamera != null)
            playerCamera.enabled = false;
    }
}
