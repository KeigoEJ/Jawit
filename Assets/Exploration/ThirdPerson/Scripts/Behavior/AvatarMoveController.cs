using UnityEngine;
using UnityEngine.Serialization;

// MoveBehaviour inherits from GenericBehaviour. This class corresponds to basic walk and run behaviour, it is the default behaviour.
public class AvatarMoveController : GenericBehaviour
{
    [Header("Move Settings")]
    public float walkSpeed = 0.15f;                 // Default walk speed.
    public float runSpeed = 1.0f;                   // Default run speed.
    public float sprintSpeed = 2.0f;                // Default sprint speed.
    public float speedDampTime = 0.1f;              // Default damp time to change the animations based on current speed.

    [Header("Jump Settings")]
    public float jumpHeight = 1.5f;                 // Default jump height.
    public float jumpInertialForce = 10f;           // Default horizontal inertial force when jumping.
    public KeyCode jumpButton = KeyCode.Space;      // Default jump button.

    private float speed, speedSeeker;
    private int groundedBool;
    private int jumpBool;
    private bool jump;
    private bool isColliding;

    void Start()
    {
        jumpBool = Animator.StringToHash("Jump");
        groundedBool = Animator.StringToHash("Grounded");
        behaviourManager.GetAnim.SetBool(groundedBool, true);

        behaviourManager.SubscribeBehaviour(this);
        behaviourManager.RegisterDefaultBehaviour(this.behaviourCode);
        speedSeeker = runSpeed;
    }

    void Update()
    {
        // Keyboard input
        if (!jump && Input.GetKeyDown(jumpButton) &&
            behaviourManager.IsCurrentBehaviour(this.behaviourCode) &&
            !behaviourManager.IsOverriding())
        {
            jump = true;
        }
    }

    public override void LocalFixedUpdate()
    {
        MovementManagement(behaviourManager.GetH, behaviourManager.GetV);
        JumpManagement();
    }

    void JumpManagement()
    {
        // Start a new jump
        if (jump && !behaviourManager.GetAnim.GetBool(jumpBool) && behaviourManager.IsGrounded())
        {
            behaviourManager.LockTempBehaviour(this.behaviourCode);

            // 🔥 tandai anim Grounded = false saat awal lompat
            behaviourManager.GetAnim.SetBool(groundedBool, false);
            behaviourManager.GetAnim.SetBool(jumpBool, true);

            // locomotion jump
            if (behaviourManager.GetAnim.GetFloat(speedFloat) > 0.1f)
            {
                var col = GetComponent<CapsuleCollider>();
                col.material.dynamicFriction = 0f;
                col.material.staticFriction = 0f;

                RemoveVerticalVelocity();

                float velocity = 2f * Mathf.Abs(Physics.gravity.y) * jumpHeight;
                velocity = Mathf.Sqrt(velocity);
                behaviourManager.GetRigidBody.AddForce(Vector3.up * velocity, ForceMode.VelocityChange);
            }
        }
        // In air
        else if (behaviourManager.GetAnim.GetBool(jumpBool))
        {
            if (!behaviourManager.IsGrounded() && !isColliding && behaviourManager.GetTempLockStatus())
            {
                behaviourManager.GetRigidBody.AddForce(
                    transform.forward * (jumpInertialForce * Physics.gravity.magnitude * sprintSpeed),
                    ForceMode.Acceleration);
            }

            // Landed
            if (behaviourManager.GetRigidBody.linearVelocity.y < 0 && behaviourManager.IsGrounded())
            {
                behaviourManager.GetAnim.SetBool(groundedBool, true);

                var col = GetComponent<CapsuleCollider>();
                col.material.dynamicFriction = 0.6f;
                col.material.staticFriction = 0.6f;

                jump = false;
                behaviourManager.GetAnim.SetBool(jumpBool, false);
                behaviourManager.UnlockTempBehaviour(this.behaviourCode);
            }
        }
    }

    void MovementManagement(float horizontal, float vertical)
    {
        if (behaviourManager.IsGrounded())
            behaviourManager.GetRigidBody.useGravity = true;
        else if (!behaviourManager.GetAnim.GetBool(jumpBool) &&
                 behaviourManager.GetRigidBody.linearVelocity.y > 0)
        {
            RemoveVerticalVelocity();
        }

        Rotating(horizontal, vertical);

        Vector2 dir = new Vector2(horizontal, vertical);
        speed = Vector2.ClampMagnitude(dir, 1f).magnitude;

        speedSeeker += Input.GetAxis("Mouse ScrollWheel");
        speedSeeker = Mathf.Clamp(speedSeeker, walkSpeed, runSpeed);

        speed *= speedSeeker;
        if (behaviourManager.IsSprinting()) speed = sprintSpeed;

        behaviourManager.GetAnim.SetFloat(speedFloat, speed, speedDampTime, Time.deltaTime);
    }

    private void RemoveVerticalVelocity()
    {
        Vector3 horizontalVelocity = behaviourManager.GetRigidBody.linearVelocity;
        horizontalVelocity.y = 0;
        behaviourManager.GetRigidBody.linearVelocity = horizontalVelocity;
    }

    Vector3 Rotating(float horizontal, float vertical)
    {
        Vector3 forward = behaviourManager.playerCamera.TransformDirection(Vector3.forward);
        forward.y = 0.0f;
        forward.Normalize();

        Vector3 right = new Vector3(forward.z, 0, -forward.x);
        Vector3 targetDirection = forward * vertical + right * horizontal;

        if (behaviourManager.IsMoving() && targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            Quaternion newRotation = Quaternion.Slerp(
                behaviourManager.GetRigidBody.rotation, targetRotation, behaviourManager.turnSmoothing);
            behaviourManager.GetRigidBody.MoveRotation(newRotation);
            behaviourManager.SetLastDirection(targetDirection);
        }

        if (!(Mathf.Abs(horizontal) > 0.9f || Mathf.Abs(vertical) > 0.9f))
            behaviourManager.Repositioning();

        return targetDirection;
    }

    private void OnCollisionStay(Collision collision)
    {
        isColliding = true;
        if (behaviourManager.IsCurrentBehaviour(this.GetBehaviourCode()) &&
            collision.GetContact(0).normal.y <= 0.1f)
        {
            var col = GetComponent<CapsuleCollider>();
            col.material.dynamicFriction = 0f;
            col.material.staticFriction = 0f;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        isColliding = false;
        var col = GetComponent<CapsuleCollider>();
        col.material.dynamicFriction = 0.6f;
        col.material.staticFriction = 0.6f;
    }

    // 🔥 dipanggil UI Button
    public void TriggerJump()
    {
        if (!jump && behaviourManager.IsCurrentBehaviour(this.behaviourCode) &&
            !behaviourManager.IsOverriding())
        {
            jump = true;
        }
    }
}
