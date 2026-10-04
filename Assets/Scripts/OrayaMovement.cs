using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Camera-relative WASD movement for Oraya. Turns to face movement and drives the Animator Speed parameter.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class OrayaMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] float moveSpeed = 4f;
    [SerializeField, Min(0f)] float runSpeed = 7f;
    [SerializeField, Min(0f)] float rotationSpeed = 540f;
    [SerializeField, Min(0f)] float animationDampTime = 0.12f;

    const float GroundedStickVelocity = -2f;
    static readonly int SpeedId = Animator.StringToHash("Speed");

    CharacterController controller;
    Animator animator;
    float verticalVelocity;
    bool running;

    /// <summary>
    /// When false, movement and run input are ignored. Gravity still holds her on the ground.
    /// </summary>
    public bool InputEnabled { get; set; } = true;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        if (animator != null)
            animator.applyRootMotion = false;
    }

    void Update()
    {
        Vector3 move = InputEnabled ? ReadCameraRelativeDirection() : Vector3.zero;
        running = InputEnabled && move.sqrMagnitude > 0.0001f && IsRunHeld();
        if (move.sqrMagnitude > 0.0001f)
            RotateToward(move);

        ApplyMovement(move);
        UpdateAnimatorSpeed();
    }

    static Vector3 ReadCameraRelativeDirection()
    {
        float x = 0f;
        float z = 0f;
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed) x -= 1f;
            if (keyboard.dKey.isPressed) x += 1f;
            if (keyboard.sKey.isPressed) z -= 1f;
            if (keyboard.wKey.isPressed) z += 1f;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKey(KeyCode.A)) x -= 1f;
        if (Input.GetKey(KeyCode.D)) x += 1f;
        if (Input.GetKey(KeyCode.S)) z -= 1f;
        if (Input.GetKey(KeyCode.W)) z += 1f;
#endif
        Vector3 input = new Vector3(x, 0f, z);
        if (input.sqrMagnitude < 0.0001f)
            return Vector3.zero;
        if (input.sqrMagnitude > 1f)
            input.Normalize();

        Camera camera = Camera.main;
        if (camera == null)
            return input;

        Vector3 forward = camera.transform.forward;
        forward.y = 0f;
        Vector3 right = camera.transform.right;
        right.y = 0f;
        if (forward.sqrMagnitude < 0.0001f || right.sqrMagnitude < 0.0001f)
            return input;

        forward.Normalize();
        right.Normalize();
        return forward * input.z + right * input.x;
    }

    void RotateToward(Vector3 worldDirection)
    {
        Quaternion target = Quaternion.LookRotation(worldDirection, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            target,
            rotationSpeed * Time.deltaTime);
    }

    void ApplyMovement(Vector3 worldDirection)
    {
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = GroundedStickVelocity;
        else
            verticalVelocity += Physics.gravity.y * Time.deltaTime;

        float speed = running ? runSpeed : moveSpeed;
        Vector3 velocity = worldDirection * speed;
        velocity.y = verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    void UpdateAnimatorSpeed()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        Vector3 horizontal = controller.velocity;
        horizontal.y = 0f;
        float animatorSpeed = 0f;
        if (horizontal.sqrMagnitude > 0.0025f)
        {
            if (running && runSpeed > 0.01f)
                animatorSpeed = 1f + Mathf.Clamp01(horizontal.magnitude / runSpeed);
            else if (moveSpeed > 0.01f)
                animatorSpeed = Mathf.Clamp01(horizontal.magnitude / moveSpeed);
        }

        animator.SetFloat(SpeedId, animatorSpeed, animationDampTime, Time.deltaTime);
    }

    static bool IsRunHeld()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed))
            return true;
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            return true;
#endif
        return false;
    }
}
