using UnityEngine;
using System.Collections.Generic;

// Urutan prioritas behaviour
public enum BehaviourPriority
{
    Low,    // Default (Move)
    Medium, // Aim
    High    // Dash
}

// Base class untuk semua behaviour
public abstract class GenericBehaviour : MonoBehaviour
{
    protected int speedFloat;
    protected AvatarMainController behaviourManager;
    protected int behaviourCode;
    protected bool canSprint;

    // 🔥 Set default priority
    public virtual BehaviourPriority Priority => BehaviourPriority.Low;

    void Awake()
    {
        behaviourManager = GetComponent<AvatarMainController>();
        speedFloat = Animator.StringToHash("Speed");
        canSprint = true;

        // Kode unik untuk tiap behaviour
        behaviourCode = this.GetType().GetHashCode();
    }

    public virtual void LocalFixedUpdate() { }
    public virtual void LocalLateUpdate() { }
    public virtual void OnOverride() { }

    public int GetBehaviourCode() => behaviourCode;
    public bool AllowSprint() => canSprint;
}

// ================================
// MAIN CONTROLLER
// ================================
public class AvatarMainController : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform playerCamera;

    [Header("Value Settings")]
    public float turnSmoothing = 0.06f;
    public float sprintFOV = 100f;
    public KeyCode sprintButton = KeyCode.LeftShift;

    [Header("Joystick Settings")]
    public AvatarJoystickController playerJoystick;

    private float h;
    private float v;
    private int currentBehaviour;
    private int defaultBehaviour;
    private int behaviourLocked;
    private Vector3 lastDirection;
    private Animator anim;
    private AvatarCameraController camScript;
    private bool sprint;
    private bool changedFOV;
    private int hFloat;
    private int vFloat;
    private List<GenericBehaviour> behaviours;
    private List<GenericBehaviour> overridingBehaviours;
    private Rigidbody rBody;
    private int groundedBool;
    private Vector3 colExtents;

    // Properties publik
    public float GetH => h;
    public float GetV => v;
    public AvatarCameraController GetCamScript => camScript;
    public Rigidbody GetRigidBody => rBody;
    public Animator GetAnim => anim;
    public int GetDefaultBehaviour => defaultBehaviour;

    void Awake()
    {
        behaviours = new List<GenericBehaviour>();
        overridingBehaviours = new List<GenericBehaviour>();
        anim = GetComponent<Animator>();
        hFloat = Animator.StringToHash("H");
        vFloat = Animator.StringToHash("V");
        camScript = playerCamera.GetComponent<AvatarCameraController>();
        rBody = GetComponent<Rigidbody>();

        groundedBool = Animator.StringToHash("Grounded");
        colExtents = GetComponent<Collider>().bounds.extents;
    }

    void Update()
    {
        // Ambil input dari joystick dulu
        if (playerJoystick != null && playerJoystick.IsTouching)
        {
            h = playerJoystick.Horizontal();
            v = playerJoystick.Vertical();
        }
        else
        {
            // fallback: keyboard
            h = Input.GetAxis("Horizontal");
            v = Input.GetAxis("Vertical");
        }
        // Set animasi
        anim.SetFloat(hFloat, h, 0.1f, Time.deltaTime);
        anim.SetFloat(vFloat, v, 0.1f, Time.deltaTime);

        // Sprint input
        sprint = Input.GetKey(sprintButton);

        // Sprint FOV
        if (IsSprinting())
        {
            changedFOV = true;
            camScript.SetFOV(sprintFOV);
        }
        else if (changedFOV)
        {
            camScript.ResetFOV();
            changedFOV = false;
        }

        // Ground check
        anim.SetBool(groundedBool, IsGrounded());
    }

    void FixedUpdate()
    {
        bool isAnyBehaviourActive = false;

        if (behaviourLocked > 0 || overridingBehaviours.Count == 0)
        {
            // Default behaviour
            foreach (GenericBehaviour behaviour in behaviours)
            {
                if (behaviour.isActiveAndEnabled && currentBehaviour == behaviour.GetBehaviourCode())
                {
                    isAnyBehaviourActive = true;
                    behaviour.LocalFixedUpdate();
                }
            }
        }
        else
        {
            // 🔥 Behaviour dengan prioritas tertinggi kontrol gerakan
            GenericBehaviour main = GetHighestPriorityBehaviour();
            if (main != null) main.LocalFixedUpdate();

            // 🔥 Behaviour lain tetap boleh jalan untuk kamera/UI
            foreach (GenericBehaviour b in overridingBehaviours)
            {
                if (b != main) b.LocalLateUpdate();
            }
        }

        if (!isAnyBehaviourActive && overridingBehaviours.Count == 0)
        {
            rBody.useGravity = true;
            Repositioning();
        }
    }

    private void LateUpdate()
    {
        if (behaviourLocked > 0 || overridingBehaviours.Count == 0)
        {
            foreach (GenericBehaviour behaviour in behaviours)
            {
                if (behaviour.isActiveAndEnabled && currentBehaviour == behaviour.GetBehaviourCode())
                {
                    behaviour.LocalLateUpdate();
                }
            }
        }
        else
        {
            foreach (GenericBehaviour behaviour in overridingBehaviours)
            {
                behaviour.LocalLateUpdate();
            }
        }
    }

    // ================================
    // Behaviour management
    // ================================
    public void SubscribeBehaviour(GenericBehaviour behaviour)
    {
        behaviours.Add(behaviour);
    }

    public void RegisterDefaultBehaviour(int behaviourCode)
    {
        defaultBehaviour = behaviourCode;
        currentBehaviour = behaviourCode;
    }

    public void RegisterBehaviour(int behaviourCode)
    {
        if (currentBehaviour == defaultBehaviour)
        {
            currentBehaviour = behaviourCode;
        }
    }

    public void UnregisterBehaviour(int behaviourCode)
    {
        if (currentBehaviour == behaviourCode)
        {
            currentBehaviour = defaultBehaviour;
        }
    }

    public bool OverrideWithBehaviour(GenericBehaviour behaviour)
    {
        if (!overridingBehaviours.Contains(behaviour))
        {
            overridingBehaviours.Add(behaviour);
            return true;
        }
        return false;
    }

    public bool RevokeOverridingBehaviour(GenericBehaviour behaviour)
    {
        if (overridingBehaviours.Contains(behaviour))
        {
            overridingBehaviours.Remove(behaviour);
            return true;
        }
        return false;
    }

    public bool IsOverriding(GenericBehaviour behaviour = null)
    {
        if (behaviour == null)
            return overridingBehaviours.Count > 0;
        return overridingBehaviours.Contains(behaviour);
    }

    public bool IsCurrentBehaviour(int behaviourCode)
    {
        return this.currentBehaviour == behaviourCode;
    }

    public bool GetTempLockStatus(int behaviourCodeIgnoreSelf = 0)
    {
        return (behaviourLocked != 0 && behaviourLocked != behaviourCodeIgnoreSelf);
    }

    public void LockTempBehaviour(int behaviourCode)
    {
        if (behaviourLocked == 0)
        {
            behaviourLocked = behaviourCode;
        }
    }

    public void UnlockTempBehaviour(int behaviourCode)
    {
        if (behaviourLocked == behaviourCode)
        {
            behaviourLocked = 0;
        }
    }

    // ================================
    // Helpers
    // ================================
    public bool IsSprinting()
    {
        return sprint && IsMoving() && CanSprint();
    }

    public bool CanSprint()
    {
        foreach (GenericBehaviour behaviour in behaviours)
        {
            if (!behaviour.AllowSprint())
                return false;
        }
        foreach (GenericBehaviour behaviour in overridingBehaviours)
        {
            if (!behaviour.AllowSprint())
                return false;
        }
        return true;
    }

    public bool IsHorizontalMoving() => h != 0;
    public bool IsMoving() => (h != 0) || (v != 0);
    public Vector3 GetLastDirection() => lastDirection;
    public void SetLastDirection(Vector3 direction) => lastDirection = direction;

    public void Repositioning()
    {
        if (lastDirection != Vector3.zero)
        {
            lastDirection.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(lastDirection);
            Quaternion newRotation = Quaternion.Slerp(rBody.rotation, targetRotation, turnSmoothing);
            rBody.MoveRotation(newRotation);
        }
    }

    public bool IsGrounded()
    {
        CapsuleCollider col = GetComponent<CapsuleCollider>();
        float radius = col.radius * 0.9f;
        Vector3 point1 = transform.position + col.center + Vector3.up * (col.height / 2 - radius);
        Vector3 point2 = transform.position + col.center - Vector3.up * (col.height / 2 - radius);

        return Physics.CheckCapsule(point1, point2, radius, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore);
    }

    // 🔥 pilih behaviour dengan prioritas tertinggi
    private GenericBehaviour GetHighestPriorityBehaviour()
    {
        GenericBehaviour chosen = null;
        BehaviourPriority highest = BehaviourPriority.Low;

        foreach (var b in overridingBehaviours)
        {
            if (b.Priority >= highest)
            {
                highest = b.Priority;
                chosen = b;
            }
        }
        return chosen;
    }
}
