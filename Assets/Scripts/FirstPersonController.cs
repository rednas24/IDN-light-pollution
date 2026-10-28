using UnityEngine;
using UnityEngine.InputSystem;

// First-person player: WASD to walk
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    private Transform cameraTransform;

    [Header("Movement")]
    private float moveSpeed = 4f;

    [Header("Look")]
    private float mouseSensitivity = 0.1f;
    private float maxLookAngle = 90f;

    private CharacterController controller;
    private InputAction moveAction;
    private InputAction lookAction;
    private float pitch;
    private float verticalVelocity;

    public bool CanLook { get; set; } = true;
    public bool CanMove { get; set; } = true;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        cameraTransform = GetComponentInChildren<Camera>()?.transform;

        if (cameraTransform == null)
        {
            Debug.LogError(
                "FirstPersonController: No Camera found as a child of the player.",
                this
            );
            enabled = false;
            return;
        }

        moveAction = InputSystem.actions.FindAction(
            "Player/Move",
            throwIfNotFound: true
        );

        lookAction = InputSystem.actions.FindAction(
            "Player/Look",
            throwIfNotFound: true
        );
    }

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        if (CanLook)
            Look();
        Move();
    }

    private void Look()
    {
        Vector2 delta = lookAction.ReadValue<Vector2>() * mouseSensitivity;
        transform.Rotate(0f, delta.x, 0f);
        pitch = Mathf.Clamp(pitch - delta.y, -maxLookAngle, maxLookAngle);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void Move()
    {
        Vector2 input = CanMove ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        Vector3 direction = transform.right * input.x + transform.forward * input.y;
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        verticalVelocity += Physics.gravity.y * Time.deltaTime;

        Vector3 velocity = direction * moveSpeed + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }
}
