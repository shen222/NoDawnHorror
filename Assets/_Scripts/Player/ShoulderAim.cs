using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

// Created by TankControls for existing player instances; no scene rewiring needed.
[DefaultExecutionOrder(-50)]
public sealed class ShoulderAim : MonoBehaviour
{
    [Min(0.01f)] public float mouseSensitivity = 2f;
    public bool invertY;
    [Range(-.35f, .2f)] public float eyeForwardOffset = -.18f;
    public float eyeSideOffset = .10f;
    public float eyeVerticalOffset = -.025f;
    [Min(.1f)] public float eyeHeight = 1.55f;
    [Range(20, 90)] public float fieldOfView = 74f;
    [Range(1, 80)] public float pitchLimit = 45f;
    [Min(1)] public float aimDistance = 100f;
    public float Pitch { get; private set; }
    public bool IsActive => active && view != null;
    private Camera view;
    private CinemachineBrain brain;
    private TankControls controls;
    private CinemachineCamera aimCamera;
    private Transform head;
    private bool active, cursorVisible;
    private CursorLockMode cursorLock;
    private float yaw;
    private readonly Dictionary<MeshRenderer, Material> originalGunMaterials = new();
    private readonly FirstPersonArmDisplay armDisplay = new();

    internal bool OwnsCamera => IsActive && aimCamera != null && brain != null && brain.IsLiveChild(aimCamera);

    private void Awake()
    {
        controls = GetComponent<TankControls>();
        foreach (Transform bone in GetComponentsInChildren<Transform>())
        {
            if (bone.name != "DEF-head") continue;
            head = bone;
            break;
        }
    }

    public bool UpdateAim()
    {
        return UpdateAim(new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")), Application.isFocused);
    }

    internal bool UpdateAim(Vector2 mouseDelta, bool focused)
    {
        if (!focused) { EndAim(); return false; }
        if (!active)
        {
            view = Camera.main;
            brain = view != null ? view.GetComponent<CinemachineBrain>() : null;
            if (brain == null || !brain.isActiveAndEnabled) return false;
            if (aimCamera == null)
            {
                var anchor = new GameObject("First Person Aim Camera");
                anchor.transform.SetParent(transform, false);
                aimCamera = anchor.AddComponent<CinemachineCamera>();
                aimCamera.Priority = 60;
            }
            var lens = aimCamera.Lens;
            lens.FieldOfView = fieldOfView;
            lens.NearClipPlane = .035f;
            lens.FarClipPlane = view.farClipPlane;
            aimCamera.Lens = lens;
            aimCamera.enabled = true;
            cursorLock = Cursor.lockState;
            cursorVisible = Cursor.visible;
            yaw = transform.eulerAngles.y;
            Pitch = controls != null ? -controls.AimAngle : 0;
            active = true;
            ApplyFirstPersonGunMaterial();
            armDisplay.Show(transform, controls != null && controls.EquippedGun != null ? controls.EquippedGun.transform : null);

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
        if (!active) return;
        if (controls == null || !controls.isActiveAndEnabled || !controls.IsAiming || controls.CurrentShootingMode != ShootingMode.ShoulderAim || Time.timeScale == 0 || DialogueSystem.BlocksPlayer || StoryCameraDirector.IsPlaying)
        { EndAim(); return; }
        PositionCamera();
    }

    private void PositionCamera()
    {
        Quaternion rotation = Quaternion.Euler(Pitch, yaw, 0f);
        Vector3 eye = head != null ? head.position + Vector3.up * .055f : transform.position + Vector3.up * eyeHeight;
        // Rotate the framing offset with the view so pitch does not push the camera toward the grip.
        Vector3 position = eye + rotation * new Vector3(eyeSideOffset, eyeVerticalOffset, eyeForwardOffset);
        aimCamera.transform.SetPositionAndRotation(position, rotation);
    }

    public Vector3 GetAimPoint()
    {
        if (!IsActive) return transform.position + transform.forward * aimDistance;
        var ray = new Ray(aimCamera.transform.position, aimCamera.transform.forward);
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

    public void ReturnToExplorationImmediately() => EndAim();

    public void EndAim()
    {
        if (!active) return;
        active = false;
        if (aimCamera != null) aimCamera.enabled = false;
        foreach (var pair in originalGunMaterials)
            if (pair.Key != null) pair.Key.sharedMaterial = pair.Value;
        originalGunMaterials.Clear();
        armDisplay.Hide();
        Cursor.lockState = cursorLock;
        Cursor.visible = cursorVisible;
    }

    private void ApplyFirstPersonGunMaterial()
    {
        var gun = controls != null ? controls.EquippedGun : null;
        if (gun == null) return;
        var material = Resources.Load<Material>("FirstPersonGlock");
        if (material == null || !material.shader.isSupported) return;

        foreach (var renderer in gun.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (renderer.name is not ("glock_barrel" or "glock_slide" or "glock_grip" or "glock_magazine")) continue;
            originalGunMaterials.Add(renderer, renderer.sharedMaterial);
            renderer.sharedMaterial = material;
        }
    }

    private void OnDisable() => EndAim();
    private void OnApplicationFocus(bool focused) { if (!focused) EndAim(); }
    private void OnDestroy()
    {
        EndAim();
        armDisplay.Dispose();
        if (aimCamera != null) Destroy(aimCamera.gameObject);
    }
}
