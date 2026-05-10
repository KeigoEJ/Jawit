using UnityEngine;

public class AvatarDashBehaviour : GenericBehaviour
{
    [Header("Behavior Settings")]
    public bool usingDash = true;
    public KeyCode dashKey = KeyCode.LeftControl;

    [Header("Dash Settings")]
    public float dashForce = 10f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    public int dashPose = 0;
    public float dashFOV = 110f;
    public bool dashFreeze = false;

    [Header("Camera FX")]
    public float smoothReturnSpeed = 5f;
    public float fovReturnSpeed = 5f;
    public float shakeMagnitude = 0.1f;
    public float shakeSpeed = 20f;

    private bool isDashing;
    private float dashTimer;
    private float lastDashTime;

    private int dashSpeedFloat;
    private Camera cam;
    private float originalFOV;
    private Vector3 camOriginalPos;

    void Start()
    {
        dashSpeedFloat = Animator.StringToHash("Speed");
        cam = behaviourManager.playerCamera.GetComponentInChildren<Camera>();

        if (cam != null)
            originalFOV = cam.fieldOfView;

        camOriginalPos = behaviourManager.GetCamScript.transform.localPosition;

        if (usingDash)
            behaviourManager.SubscribeBehaviour(this);
    }

    void Update()
    {
        if (!usingDash) return;

        if (!isDashing && Time.time >= lastDashTime + dashCooldown)
        {
            if (Input.GetKeyDown(dashKey))
                StartDash();
        }
    }

    public override void LocalFixedUpdate()
    {
        if (isDashing)
        {
            dashTimer += Time.fixedDeltaTime;

            // Dorong karakter ke depan
            behaviourManager.GetRigidBody.linearVelocity = transform.forward * dashForce;

            // Paksa animasi lebih cepat
            behaviourManager.GetAnim.SetFloat(dashSpeedFloat, 2.0f, 0.05f, Time.deltaTime);

            // Efek kamera
            if (cam != null)
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, dashFOV, Time.fixedDeltaTime * fovReturnSpeed);

            CameraShake();

            if (dashTimer >= dashDuration)
                EndDash();
        }
        else
        {
            // Smooth balik FOV kamera
            if (cam != null)
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, originalFOV, Time.deltaTime * fovReturnSpeed);

            // Reset posisi kamera (hapus shake)
            behaviourManager.GetCamScript.transform.localPosition = Vector3.Lerp(
                behaviourManager.GetCamScript.transform.localPosition,
                camOriginalPos,
                Time.deltaTime * 10f
            );

            // Smooth balik Speed animator
            float currentSpeed = behaviourManager.GetAnim.GetFloat(dashSpeedFloat);
            float targetSpeed = behaviourManager.IsMoving() ? 1.0f : 0.0f;
            float newSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * smoothReturnSpeed);
            behaviourManager.GetAnim.SetFloat(dashSpeedFloat, newSpeed);
        }
    }

    private void StartDash()
    {
        isDashing = true;
        dashTimer = 0f;
        lastDashTime = Time.time;

        // 🔥 Pastikan Dash selalu override behaviour lain
        foreach (var b in behaviourManager.GetComponents<GenericBehaviour>())
        {
            if (b != this) behaviourManager.RevokeOverridingBehaviour(b);
        }

        behaviourManager.OverrideWithBehaviour(this);
        behaviourManager.LockTempBehaviour(this.behaviourCode);

        camOriginalPos = behaviourManager.GetCamScript.transform.localPosition;

        if (dashFreeze)
        {
            behaviourManager.GetAnim.Play("Locomotion", dashPose, 0f);
            behaviourManager.GetAnim.speed = 0f;
        }
    }

    private void EndDash()
    {
        isDashing = false;

        behaviourManager.RevokeOverridingBehaviour(this);
        behaviourManager.UnlockTempBehaviour(this.behaviourCode);

        // Jangan langsung matikan velocity
        if (!behaviourManager.IsMoving())
            behaviourManager.GetRigidBody.linearVelocity = Vector3.zero;

        if (cam != null)
            cam.fieldOfView = originalFOV;

        behaviourManager.GetCamScript.transform.localPosition = camOriginalPos;
        behaviourManager.GetAnim.speed = 1f;
    }

    private void CameraShake()
    {
        Vector3 shake = Random.insideUnitSphere * shakeMagnitude;
        shake.z = 0;
        behaviourManager.GetCamScript.transform.localPosition =
            camOriginalPos + shake * Mathf.Sin(Time.time * shakeSpeed);
    }

    // 🔥 Dash = High priority, override Aim/Fly/Move
    public override BehaviourPriority Priority => BehaviourPriority.High;
}
