using UnityEngine;
using System.Collections;

public class AvatarAimController : GenericBehaviour
{
    [Header("Behavior Settings")]
    public bool usingAim = true;
    public KeyCode aimKey = KeyCode.Mouse1;
    public KeyCode shoulderKey = KeyCode.LeftShift;

    [Header("Aim Settings")]
    public Texture2D crosshair;
    public float crosshairSize = 40f;

    [Header("Camera Settings")]
    public float aimTurnSmoothing = 0.15f;
    public Vector3 aimPivotOffset = new Vector3(0.5f, 1.2f, 0f);
    public Vector3 aimCamOffset = new Vector3(0f, 0.4f, -0.7f);

    private int aimBool;
    private bool aim;

    void Start()
    {
        if (usingAim)
        {
            aimBool = Animator.StringToHash("Aim");
            behaviourManager.SubscribeBehaviour(this);
        }
    }

    void Update()
    {
        if (!usingAim) return;

        if (Input.GetKey(aimKey) && !aim)
        {
            StartCoroutine(ToggleAimOn());
        }
        else if (aim && !Input.GetKey(aimKey))
        {
            StartCoroutine(ToggleAimOff());
        }

        // Tidak bisa sprint saat aim
        canSprint = !aim;

        // Ganti bahu kamera
        if (aim && Input.GetKeyDown(shoulderKey))
        {
            aimCamOffset.x *= -1;
            aimPivotOffset.x *= -1;
        }

        behaviourManager.GetAnim.SetBool(aimBool, aim);
    }

    private IEnumerator ToggleAimOn()
    {
        yield return new WaitForSeconds(0.05f);

        // ❌ Jangan aktifkan Aim kalau behaviour lain nge-lock (contoh: Dash)
        if (behaviourManager.GetTempLockStatus(this.behaviourCode) || behaviourManager.IsOverriding(this))
            yield break;

        aim = true;
        behaviourManager.OverrideWithBehaviour(this);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private IEnumerator ToggleAimOff()
    {
        aim = false;
        yield return new WaitForSeconds(0.3f);
        behaviourManager.GetCamScript.ResetTargetOffsets();
        behaviourManager.GetCamScript.ResetMaxVerticalAngle();
        yield return new WaitForSeconds(0.05f);
        behaviourManager.RevokeOverridingBehaviour(this);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public override void LocalFixedUpdate()
    {
        if (aim)
            behaviourManager.GetCamScript.SetTargetOffsets(aimPivotOffset, aimCamOffset);
    }

    public override void LocalLateUpdate()
    {
        if (aim) AimManagement();
    }

    void AimManagement()
    {
        Rotating();
    }

    void Rotating()
    {
        Vector3 forward = behaviourManager.playerCamera.TransformDirection(Vector3.forward);
        forward.y = 0.0f;
        forward = forward.normalized;

        Quaternion targetRotation = Quaternion.Euler(0, behaviourManager.GetCamScript.GetH, 0);
        float minSpeed = Quaternion.Angle(transform.rotation, targetRotation) * aimTurnSmoothing;

        behaviourManager.SetLastDirection(forward);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, minSpeed * Time.deltaTime);
    }

    void OnGUI()
    {
        if (crosshair && aim)
        {
            float mag = behaviourManager.GetCamScript.GetCurrentPivotMagnitude(aimPivotOffset);
            if (mag < 0.05f)
            {
                float size = crosshairSize;
                GUI.DrawTexture(
                    new Rect(Screen.width / 2f - size * 0.5f,
                             Screen.height / 2f - size * 0.5f,
                             size,
                             size),
                    crosshair);
            }
        }
    }
}
