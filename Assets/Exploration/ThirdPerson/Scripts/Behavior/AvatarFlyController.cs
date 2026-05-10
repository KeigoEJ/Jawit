using UnityEngine;
using System.Collections;

public class AvatarFlyController : GenericBehaviour
{
    [Header("Behavior Settings")]
    public bool usingFly;
    public KeyCode flyKey = KeyCode.F;

    [Header("Fly Settings")]
    public float flyMaxVerticalAngle = 60f;
    public float flyLiftOffset = 2.0f;
    public float flyLiftDuration = 0.6f;
    public float flyLandDuration = 0.6f;
    public float flySpeed = 4.0f;
    public float sprintFactor = 2.0f;

    [Header("Camera FX")]
    public float liftFOV = 105f;
    public float landFOV = 95f;
    public float fovLerpSpeed = 4f;

    private int flyBool;
    private bool fly = false;
    private CapsuleCollider col;
    private Coroutine flyCoroutine;

    private Camera cam;
    private float originalFOV;

    void Start()
    {
        if (usingFly)
        {
            flyBool = Animator.StringToHash("Fly");
            col = this.GetComponent<CapsuleCollider>();
            behaviourManager.SubscribeBehaviour(this);

            cam = behaviourManager.playerCamera.GetComponentInChildren<Camera>();
            if (cam != null)
                originalFOV = cam.fieldOfView;
        }
    }

    void Update()
    {
        if (usingFly)
        {
            if (Input.GetKeyDown(flyKey)
                && !behaviourManager.IsOverriding()
                && !behaviourManager.GetTempLockStatus(behaviourManager.GetDefaultBehaviour))
            {
                fly = !fly;
                behaviourManager.UnlockTempBehaviour(behaviourManager.GetDefaultBehaviour);
                behaviourManager.GetRigidBody.useGravity = !fly;

                if (fly)
                {
                    behaviourManager.RegisterBehaviour(this.behaviourCode);

                    if (flyCoroutine != null) StopCoroutine(flyCoroutine);
                    flyCoroutine = StartCoroutine(SmoothLiftOffFX());
                }
                else
                {
                    col.direction = 1;
                    behaviourManager.GetCamScript.ResetTargetOffsets();
                    behaviourManager.UnregisterBehaviour(this.behaviourCode);

                    if (flyCoroutine != null) StopCoroutine(flyCoroutine);
                    flyCoroutine = StartCoroutine(SmoothLandingFX());
                }
            }

            // 🔥 cukup set animator Fly = true/false
            behaviourManager.GetAnim.SetBool(flyBool, fly && behaviourManager.IsCurrentBehaviour(this.behaviourCode));
        }
    }

    public override void OnOverride()
    {
        col.direction = 1;
    }

    public override void LocalFixedUpdate()
    {
        if (fly)
        {
            behaviourManager.GetCamScript.SetMaxVerticalAngle(flyMaxVerticalAngle);
            FlyManagement(behaviourManager.GetH, behaviourManager.GetV);
        }
    }

    private void FlyManagement(float horizontal, float vertical)
    {
        Vector3 direction = Rotating(horizontal, vertical);
        behaviourManager.GetRigidBody.AddForce(
            direction * (flySpeed * 100 * (behaviourManager.IsSprinting() ? sprintFactor : 1)),
            ForceMode.Acceleration
        );
    }

    private Vector3 Rotating(float horizontal, float vertical)
    {
        Vector3 forward = behaviourManager.playerCamera.TransformDirection(Vector3.forward);
        forward = forward.normalized;

        Vector3 right = new Vector3(forward.z, 0, -forward.x);
        Vector3 targetDirection = forward * vertical + right * horizontal;

        if (behaviourManager.IsMoving() && targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            Quaternion newRotation = Quaternion.Slerp(
                behaviourManager.GetRigidBody.rotation,
                targetRotation,
                behaviourManager.turnSmoothing
            );

            behaviourManager.GetRigidBody.MoveRotation(newRotation);
            behaviourManager.SetLastDirection(targetDirection);
        }

        if (!(Mathf.Abs(horizontal) > 0.2 || Mathf.Abs(vertical) > 0.2))
        {
            behaviourManager.Repositioning();
            col.direction = 1;
        }
        else
        {
            col.direction = 2;
        }

        return targetDirection;
    }

    private IEnumerator SmoothLiftOffFX()
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + Vector3.up * flyLiftOffset;
        float elapsed = 0f;

        while (elapsed < flyLiftDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / flyLiftDuration);

            behaviourManager.GetRigidBody.MovePosition(Vector3.Lerp(startPos, targetPos, t));

            if (cam != null)
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, liftFOV, Time.deltaTime * fovLerpSpeed);

            yield return null;
        }

        behaviourManager.GetRigidBody.MovePosition(targetPos);
        if (cam != null) cam.fieldOfView = originalFOV;
    }

    private IEnumerator SmoothLandingFX()
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = new Vector3(startPos.x, Mathf.Round(startPos.y), startPos.z);
        float elapsed = 0f;

        while (elapsed < flyLandDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / flyLandDuration);

            behaviourManager.GetRigidBody.MovePosition(Vector3.Lerp(startPos, targetPos, t));

            if (cam != null)
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, landFOV, Time.deltaTime * fovLerpSpeed);

            yield return null;
        }

        behaviourManager.GetRigidBody.MovePosition(targetPos);
        if (cam != null) cam.fieldOfView = originalFOV;
    }

    public override BehaviourPriority Priority => BehaviourPriority.Medium;
}
