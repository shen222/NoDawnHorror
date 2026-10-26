using UnityEngine;
using Unity.Cinemachine;

// Created by TankControls for existing player instances; no scene rewiring needed.
[DefaultExecutionOrder(10000)]
public sealed class ShoulderAim : MonoBehaviour
{
    [Min(0.01f)] public float mouseSensitivity = 2f;
    public bool invertY;
    [Min(0f)] public float transitionDuration = 0.4f;
    public Vector3 shoulderOffset = new Vector3(0.55f, 1.55f, -2.2f);
    [Range(20, 90)] public float fieldOfView = 50f;
    [Range(1, 80)] public float pitchLimit = 45f;
    [Min(1)] public float aimDistance = 100f;
    [Min(0.01f)] public float collisionRadius = 0.18f;
    public float Pitch { get; private set; }
    public bool IsActive => active && view != null;
    private Camera view;
    private CinemachineBrain brain;
    private TankControls controls;
    private GameObject crosshair;
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
        if (!Application.isFocused) { EndAim(); return false; }
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
            if (crosshair == null) BuildCrosshair();
            crosshair.SetActive(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            // Ignore the mouse delta from the frame that captured the cursor.
        }
        else
        {
            yaw += Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            Pitch = Mathf.Clamp(Pitch + Input.GetAxisRaw("Mouse Y") * mouseSensitivity * (invertY ? 1 : -1), -pitchLimit, pitchLimit);
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
        if (controls == null || !controls.isActiveAndEnabled || !controls.IsAiming || Time.timeScale == 0 || DialogueSystem.BlocksPlayer || StoryCameraDirector.IsPlaying)
        { EndAim(); return; }
        blendElapsed += Time.unscaledDeltaTime;
        PositionCamera();
    }

    private void PositionCamera()
    {
        if (view == null) return;
        Quaternion rotation = Quaternion.Euler(Pitch, yaw, 0);
        Vector3 pivot = transform.position + Vector3.up * shoulderOffset.y;
        Vector3 offset = rotation * new Vector3(shoulderOffset.x, 0, shoulderOffset.z);
        float distance = offset.magnitude;
        float allowed = distance;
        foreach (var hit in Physics.SphereCastAll(pivot, collisionRadius, offset.normalized, distance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(transform)) continue;
            allowed = Mathf.Min(allowed, Mathf.Max(0, hit.distance - 0.05f));
        }
        ApplyBlend(pivot + offset.normalized * allowed, rotation, fieldOfView);
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
        EndAim();
        returning = false;
    }

    public void EndAim()
    {
        if (!active) return;
        active = false;
        if (crosshair != null) crosshair.SetActive(false);
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
    private void OnDestroy() { EndAim(); if (crosshair != null) Destroy(crosshair); }

    private void BuildCrosshair()
    {
        crosshair = new GameObject("AimCrosshair", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(crosshair, gameObject.scene);
        var canvas = crosshair.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 95;
        var scaler = crosshair.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        Bar(new Vector2(-9, 0), new Vector2(8, 2));
        Bar(new Vector2(9, 0), new Vector2(8, 2));
        Bar(new Vector2(0, -9), new Vector2(2, 8));
        Bar(new Vector2(0, 9), new Vector2(2, 8));
    }

    private void Bar(Vector2 position, Vector2 size)
    {
        var obj = new GameObject("CrosshairBar", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Outline));
        var rect = obj.GetComponent<RectTransform>();
        rect.SetParent(crosshair.transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var image = obj.GetComponent<UnityEngine.UI.Image>();
        image.color = Color.white;
        image.raycastTarget = false;
        var outline = obj.GetComponent<UnityEngine.UI.Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.8f);
        outline.effectDistance = Vector2.one;
    }
}
