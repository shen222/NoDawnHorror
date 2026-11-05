using UnityEngine;
using Unity.Cinemachine;

// Created by TankControls for existing player instances; no scene rewiring needed.
[DefaultExecutionOrder(10000)]
public sealed class ShoulderAim : MonoBehaviour
{
    [Min(0.01f)] public float mouseSensitivity = 2f;
    public bool invertY;
    [Min(0f)] public float transitionDuration = 0.4f;
    [Min(.1f)] public float eyeHeight = 1.55f;
    [Range(20, 90)] public float fieldOfView = 58f;
    [Range(1, 80)] public float pitchLimit = 45f;
    [Min(1)] public float aimDistance = 100f;
    public float Pitch { get; private set; }
    public bool IsActive => active && view != null;
    private Camera view;
    private CinemachineBrain brain;
    private TankControls controls;
    private readonly FirstPersonAimModel aimModel = new();
    private bool active, brainWasEnabled, cursorVisible;
    private CursorLockMode cursorLock;
    private Vector3 oldPosition;
    private Quaternion oldRotation;
    private float oldFov, yaw;
    private bool returning;
    private Vector3 blendPosition;
    private Quaternion blendRotation;
    private float blendFov, blendElapsed;

    private void Awake() => controls = GetComponent<TankControls>();

    public bool UpdateAim()
    {
        return UpdateAim(new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")), Application.isFocused);
    }

    internal bool UpdateAim(Vector2 mouseDelta, bool focused)
    {
        if (!focused) { EndAim(); return false; }
        if (!active)
        {
            bool wasReturning = returning;
            view = Camera.main;
            if (view == null) return false;
            brain = view.GetComponent<CinemachineBrain>();
            brainWasEnabled = brain != null && brain.enabled;
            if (!wasReturning)
            {
                oldPosition = view.transform.position;
                oldRotation = view.transform.rotation;
                oldFov = view.fieldOfView;
            }
            CaptureBlendStart();
            returning = false;
            cursorLock = Cursor.lockState;
            cursorVisible = Cursor.visible;
            yaw = transform.eulerAngles.y;
            Pitch = controls != null ? -controls.AimAngle : 0;
            active = true;
            if (brain != null) brain.enabled = false;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            // Ignore the mouse delta from the frame that captured the cursor.
        }
        else
        {
            yaw += mouseDelta.x * mouseSensitivity;
            Pitch = Mathf.Clamp(Pitch + mouseDelta.y * mouseSensitivity * (invertY ? 1 : -1), -pitchLimit, pitchLimit);
        }
        transform.rotation = Quaternion.Euler(0, yaw, 0);
        PositionCamera();
        return true;
    }

    private void LateUpdate()
    {
        if (returning)
        {
            if (view == null) { returning = false; return; }
            blendElapsed += Time.unscaledDeltaTime;
            // Cinemachine has already evaluated the current fixed camera this frame.
            // Follow that live destination even if the player crosses a camera zone.
            Vector3 destination = brain != null && brainWasEnabled ? view.transform.position : oldPosition;
            Quaternion rotation = brain != null && brainWasEnabled ? view.transform.rotation : oldRotation;
            float fov = brain != null && brainWasEnabled ? view.fieldOfView : oldFov;
            ApplyBlend(destination, rotation, fov);
            if (BlendFraction >= 1f) returning = false;
            return;
        }
        if (!active) return;
        if (controls == null || !controls.isActiveAndEnabled || !controls.IsAiming || controls.CurrentShootingMode != ShootingMode.ShoulderAim || Time.timeScale == 0 || DialogueSystem.BlocksPlayer || StoryCameraDirector.IsPlaying)
        { EndAim(); return; }
        blendElapsed += Time.unscaledDeltaTime;
        PositionCamera();
    }

    private void PositionCamera()
    {
        if (view == null) return;
        Quaternion rotation = Quaternion.Euler(Pitch, yaw, 0);
        Vector3 eye = transform.position + Vector3.up * eyeHeight + transform.forward * .05f;
        ApplyBlend(eye, rotation, fieldOfView);
        if (BlendFraction >= 1f)
        {
            aimModel.Show(view, transform);
            aimModel.AnimateReload(controls != null ? controls.EquippedGun : null);
        }
    }

    public Vector3 GetMuzzlePosition(Transform fallback)
    {
        if (!IsActive || !aimModel.Visible) return fallback.position;
        Vector3 muzzle = aimModel.MuzzlePosition;
        Vector3 start = view.transform.position;
        Vector3 offset = muzzle - start;
        float distance = offset.magnitude;
        if (distance <= .001f) return muzzle;
        Vector3 direction = offset / distance;
        foreach (var hit in Physics.RaycastAll(start, direction, distance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(transform) || hit.collider.GetComponentInParent<Projectile>() != null) continue;
            distance = Mathf.Min(distance, Mathf.Max(0f, hit.distance - .02f));
        }
        return start + direction * distance;
    }

    private float BlendFraction => transitionDuration <= 0f ? 1f : Mathf.Clamp01(blendElapsed / transitionDuration);

    private void CaptureBlendStart()
    {
        blendPosition = view.transform.position;
        blendRotation = view.transform.rotation;
        blendFov = view.fieldOfView;
        blendElapsed = 0f;
    }

    private void ApplyBlend(Vector3 position, Quaternion rotation, float fov)
    {
        float t = Mathf.SmoothStep(0f, 1f, BlendFraction);
        view.transform.SetPositionAndRotation(Vector3.Lerp(blendPosition, position, t), Quaternion.Slerp(blendRotation, rotation, t));
        view.fieldOfView = Mathf.Lerp(blendFov, fov, t);
    }

    public Vector3 GetAimPoint()
    {
        if (!IsActive) return transform.position + transform.forward * aimDistance;
        Ray ray = view.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        Vector3 target = ray.GetPoint(aimDistance);
        float nearest = aimDistance;
        foreach (var hit in Physics.RaycastAll(ray, aimDistance, ~0, QueryTriggerInteraction.Collide))
        {
            if (hit.transform.IsChildOf(transform) || hit.collider.GetComponentInParent<Projectile>() != null) continue;
            if (hit.collider.isTrigger && hit.collider.GetComponentInParent<ZombieAI>() == null &&
                hit.collider.gameObject.layer != LayerMask.NameToLayer("Enemy") &&
                hit.collider.gameObject.layer != LayerMask.NameToLayer("Target")) continue;
            if (hit.distance >= nearest) continue;
            nearest = hit.distance;
            target = hit.point;
        }
        return target;
    }

    public void CancelForStoryShot()
    {
        ReturnToExplorationImmediately();
    }

    public void ReturnToExplorationImmediately()
    {
        EndAim();
        returning = false;
        if (view != null && (brain == null || !brainWasEnabled))
        {
            view.transform.SetPositionAndRotation(oldPosition, oldRotation);
            view.fieldOfView = oldFov;
        }
    }

    public void EndAim()
    {
        if (!active) return;
        active = false;
        aimModel.Hide();
        if (view != null)
        {
            CaptureBlendStart();
            returning = true;
        }
        if (brain != null) brain.enabled = brainWasEnabled;
        Cursor.lockState = cursorLock;
        Cursor.visible = cursorVisible;
    }

    private void OnDisable()
    {
        EndAim();
        returning = false;
        if (view != null && (brain == null || !brainWasEnabled))
        {
            view.transform.SetPositionAndRotation(oldPosition, oldRotation);
            view.fieldOfView = oldFov;
        }
    }
    private void OnApplicationFocus(bool focused) { if (!focused) EndAim(); }
    private void OnDestroy() { EndAim(); aimModel.Destroy(); }
}
