using UnityEngine;
using UnityEngine.InputSystem;

// First-person player: WASD to walk
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    private Transform cameraTransform;

    [Header("Movement")]
    public float moveSpeed = 4f;

    [Header("Look")]
    public float mouseSensitivity = 0.1f;
    private float maxLookAngle = 90f;

    [Header("Footsteps")]
    [SerializeField] private AudioClip footstepSound;
    private float stepDistance = 1.2f;

    private CharacterController controller;
    private AudioSource footsteps;
    private InputAction moveAction;
    private InputAction lookAction;
    private float pitch;
    private float verticalVelocity;
    private float stepProgress;

    public bool CanLook { get; set; } = true;
    public bool CanMove { get; set; } = true;

    public float Pitch
    {
        get => pitch;
        set
        {
            pitch = Mathf.Clamp(value, -maxLookAngle, maxLookAngle);
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

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

        footsteps = gameObject.AddComponent<AudioSource>();
        footsteps.playOnAwake = false;
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
        Pitch = pitch - delta.y;
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
        Footsteps();
    }

    private void Footsteps()
    {
        Vector3 walked = controller.velocity;
        walked.y = 0f;
        if (walked.sqrMagnitude < 0.01f)
        {
            stepProgress = stepDistance;
            return;
        }
        if (!controller.isGrounded)
            return;

        stepProgress += walked.magnitude * Time.deltaTime;
        if (stepProgress < stepDistance || footstepSound == null)
            return;

        stepProgress = 0f;
        footsteps.pitch = Random.Range(0.9f, 1.1f);
        footsteps.PlayOneShot(footstepSound, Random.Range(0.8f, 1f));
    }
}
