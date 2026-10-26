using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class CamSwitcher : MonoBehaviour
{
    public enum CameraMode { Fixed, Moving, Follow, Cinematic }
    public enum ProgressMode { AlongPath, ApproachMarker }
    internal static readonly List<CamSwitcher> Zones = new List<CamSwitcher>();
    public Transform player;
    public CinemachineCamera activeCamera;
    public CameraMode mode;
    public int zonePriority = 10;
    [Min(0)] public float exitDelay = 0.25f;
    [Tooltip("Switch immediately when this zone wins camera selection, bypassing blend and shot delays. Overlapping zones still obey Zone Priority.")]
    public bool cutOnEnter;
    [Min(0)] public float blendDurationOverride;
    [Header("Moving camera: player path and camera endpoints")]
    public Transform pathStart, pathEnd, cameraStart, cameraEnd;
    [Min(0)] public float movementDamping = 0.3f;
    [Min(0.01f)] public float maximumTravelSpeed = 2.5f;
    [Min(1)] public float maximumTurnSpeed = 35f;
    [Header("Cinematic shot")]
    public ProgressMode progressMode;
    [Tooltip("For Approach Marker, progress increases as the player approaches this point.")]
    public Transform approachMarker;
    [Min(0)] public float farDistance = 10f;
    [Min(0)] public float nearDistance = 2f;
    public AnimationCurve travelCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("Optional middle point for a curved camera path. Start and End remain the endpoints.")]
    public Transform cameraControl;
    public Vector3 playerLookOffset = new Vector3(0, 1.3f, 0);
    [Tooltip("Optional object to reveal. Focus Weight controls the shift from player to object.")]
    public Transform focusTarget;
    public Vector3 focusOffset;
    public AnimationCurve focusWeight = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Min(0)] public float rotationDamping = 0.3f;
    public bool animateFieldOfView;
    [Range(10, 100)] public float startFieldOfView = 52f;
    [Range(10, 100)] public float endFieldOfView = 45f;
    private Collider volume;
    private Vector3 velocity;
    private bool shotInitialized;
    private void OnEnable() { volume = GetComponent<Collider>(); if (!Zones.Contains(this)) Zones.Add(this); }
    private void OnDisable() => Zones.Remove(this);
    public bool Contains(Vector3 point)
    {
        if (volume == null) volume = GetComponent<Collider>();
        return isActiveAndEnabled && volume != null && volume.enabled &&
            (mode == CameraMode.Follow || (activeCamera != null && activeCamera.gameObject.activeInHierarchy)) &&
            (volume.ClosestPoint(point) - point).sqrMagnitude < 0.0001f;
    }
    public static float PathProgress(Vector3 point, Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        return direction.sqrMagnitude < 0.0001f ? 0 : Mathf.Clamp01(Vector3.Dot(point - start, direction) / direction.sqrMagnitude);
    }
    public static float ApproachProgress(Vector3 point, Vector3 marker, float far, float near)
    {
        // Floor distance avoids changing the shot because of the player's height.
        point.y = marker.y;
        return Mathf.Clamp01((far - Vector3.Distance(point, marker)) / Mathf.Max(0.01f, far - near));
    }
    internal void BeginShot()
    {
        velocity = Vector3.zero;
        shotInitialized = false;
    }
    internal void UpdateMovingCamera(Transform target, bool snap)
    {
        if ((mode != CameraMode.Moving && mode != CameraMode.Cinematic) || target == null || activeCamera == null || cameraStart == null || cameraEnd == null) return;
        bool cinematic = mode == CameraMode.Cinematic;
        float progress;
        if (cinematic && progressMode == ProgressMode.ApproachMarker)
        {
            if (approachMarker == null) return;
            progress = ApproachProgress(target.position, approachMarker.position, farDistance, nearDistance);
        }
        else
        {
            if (pathStart == null || pathEnd == null) return;
            progress = PathProgress(target.position, pathStart.position, pathEnd.position);
        }
        float t = cinematic && travelCurve != null ? Mathf.Clamp01(travelCurve.Evaluate(progress)) : progress;
        Vector3 position = Vector3.Lerp(cameraStart.position, cameraEnd.position, t);
        if (cinematic && cameraControl != null)
            position = (1 - t) * (1 - t) * cameraStart.position + 2 * (1 - t) * t * cameraControl.position + t * t * cameraEnd.position;
        snap |= !shotInitialized;
        activeCamera.transform.position = snap || movementDamping <= 0 ? position : Vector3.SmoothDamp(activeCamera.transform.position, position, ref velocity, movementDamping, maximumTravelSpeed, Time.deltaTime);
        Vector3 lookPoint = target.position + (cinematic ? playerLookOffset : Vector3.up * 1.3f);
        if (cinematic && focusTarget != null)
        {
            float weight = focusWeight != null ? Mathf.Clamp01(focusWeight.Evaluate(progress)) : progress;
            lookPoint = Vector3.Lerp(lookPoint, focusTarget.position + focusOffset, weight);
        }
        Vector3 direction = lookPoint - activeCamera.transform.position;
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion rotation = Quaternion.LookRotation(direction);
            float blend = snap || rotationDamping <= 0 ? 1 : 1 - Mathf.Exp(-Time.deltaTime / rotationDamping);
            Quaternion smoothed = Quaternion.Slerp(activeCamera.transform.rotation, rotation, blend);
            activeCamera.transform.rotation = snap ? rotation : Quaternion.RotateTowards(activeCamera.transform.rotation, smoothed, maximumTurnSpeed * Time.deltaTime);
        }
        if (cinematic && animateFieldOfView)
        {
            var lens = activeCamera.Lens;
            float desired = Mathf.Lerp(startFieldOfView, endFieldOfView, t);
            lens.FieldOfView = snap || movementDamping <= 0 ? desired : Mathf.Lerp(lens.FieldOfView, desired, 1 - Mathf.Exp(-Time.deltaTime / movementDamping));
            activeCamera.Lens = lens;
        }
        shotInitialized = true;
    }
}
