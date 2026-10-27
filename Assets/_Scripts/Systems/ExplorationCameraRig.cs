using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;

[DefaultExecutionOrder(-200)]
public sealed class ExplorationCameraRig : MonoBehaviour
{
    public TankControls player;
    [Header("Exploration camera selection")]
    public CinemachineCamera defaultFixedCamera;
    [Tooltip("Enable only when you want a follow camera outside authored zones.")]
    public bool followOutsideZones;
    [Tooltip("Opt in to the separate cinematic movement zones. Fixed and Moving zones still work normally.")]
    public bool enableCinematicZones;
    public Vector3 followOffset = new Vector3(0.6f, 2.8f, -5f);
    [Min(0)] public float followDamping = 0.8f;
    [Min(0)] public float blendDuration = 0.65f;
    [Header("Comfort and transition stability")]
    [Min(0)] public float minimumBlendDuration = 1.4f;
    [Min(0)] public float minimumShotDuration = 1.5f;
    [Min(0)] public float switchConfirmationTime = 0.25f;
    [Tooltip("Applies to non-fixed camera zones. Fixed angles always cut; shoulder aiming has its own smooth transition.")]
    public bool smoothZoneTransitions = true;
    public CinemachineCamera FollowCamera { get; private set; }
    public CamSwitcher CurrentZone { get; private set; }
    private CinemachineBrain brain;
    private CinemachineBlendDefinition oldBlend;
    private float outsideTime;
    private bool initialized;
    private CinemachineCamera lastFixedCamera;
    private CameraObstacleAvoidance collision;
    private CamSwitcher pendingZone;
    private float pendingTime, shotAge;
    private bool pendingChange, evaluatedZones;
    private readonly Dictionary<CamSwitcher, long> zoneEntries = new Dictionary<CamSwitcher, long>();
    private readonly List<CamSwitcher> exitedZones = new List<CamSwitcher>();
    private long entrySequence;

    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<TankControls>();
        if (player == null || Camera.main == null) return;
        collision = Camera.main.GetComponent<CameraObstacleAvoidance>();
        if (collision == null) collision = Camera.main.gameObject.AddComponent<CameraObstacleAvoidance>();
        collision.player = player.transform;
        brain = Camera.main.GetComponent<CinemachineBrain>();
        if (brain == null) return;
        if (GetComponent<StoryCameraDirector>() == null) gameObject.AddComponent<StoryCameraDirector>();
        if (defaultFixedCamera == null)
        {
            float nearest = float.PositiveInfinity;
            foreach (var zone in CamSwitcher.Zones)
            {
                if (zone == null || zone.gameObject.scene != player.gameObject.scene || zone.mode != CamSwitcher.CameraMode.Fixed || zone.activeCamera == null) continue;
                var bounds = zone.GetComponent<Collider>();
                float distance = ((bounds != null ? bounds.ClosestPoint(player.transform.position) : zone.transform.position) - player.transform.position).sqrMagnitude;
                if (distance >= nearest) continue;
                nearest = distance;
                defaultFixedCamera = zone.activeCamera;
            }
        }
        oldBlend = brain.DefaultBlend;
        var root = new GameObject("Exploration Follow Camera");
        root.transform.SetParent(transform, false);
        FollowCamera = root.AddComponent<CinemachineCamera>();
        FollowCamera.Follow = player.transform;
        FollowCamera.LookAt = player.transform;
        FollowCamera.Lens.FieldOfView = 52;
        var follow = root.AddComponent<CinemachineFollow>();
        follow.FollowOffset = followOffset;
        follow.TrackerSettings.BindingMode = Unity.Cinemachine.TargetTracking.BindingMode.LazyFollow;
        follow.TrackerSettings.PositionDamping = Vector3.one * followDamping;
        var look = root.AddComponent<CinemachineHardLookAt>();
        look.LookAtOffset = Vector3.up * 1.3f;
        var obstacles = root.AddComponent<CinemachineDeoccluder>();
        obstacles.IgnoreTag = player.tag;
        initialized = true;
        SelectZone(null);
    }

    // Player movement runs in Update; evaluate zones afterwards, before Cinemachine's LateUpdate.
    private void LateUpdate() => AdvanceExploration(Time.deltaTime);

    internal void AdvanceExploration(float deltaTime)
    {
        if (!initialized || player == null || Time.timeScale == 0) return;
        Vector3 point = player.transform.position + Vector3.up * 0.25f;
        // Record entries, rather than letting the current zone permanently win
        // every equal-priority overlap. Entries in the same frame share a stamp.
        ++entrySequence;
        exitedZones.Clear();
        foreach (var entry in zoneEntries)
            if (entry.Key == null || !entry.Key.Contains(point)) exitedZones.Add(entry.Key);
        foreach (var exited in exitedZones) zoneEntries.Remove(exited);
        CamSwitcher selected = null;
        foreach (var zone in CamSwitcher.Zones)
        {
            if (zone == null || zone.gameObject.scene != player.gameObject.scene || !zone.Contains(point)) continue;
            if (!enableCinematicZones && zone.mode == CamSwitcher.CameraMode.Cinematic) continue;
            if (!zoneEntries.ContainsKey(zone)) zoneEntries.Add(zone, entrySequence);
            if (selected == null || zone.zonePriority > selected.zonePriority ||
                (zone.zonePriority == selected.zonePriority &&
                    (zoneEntries[zone] > zoneEntries[selected] ||
                     (zoneEntries[zone] == zoneEntries[selected] && zone == CurrentZone)))) selected = zone;
        }
        if (CurrentZone != null && CurrentZone.isActiveAndEnabled && !CurrentZone.Contains(point))
        {
            outsideTime += deltaTime;
            bool enteringCut = selected != CurrentZone && UsesCut(selected);
            if (!enteringCut && outsideTime < CurrentZone.exitDelay && (selected == null || selected.zonePriority <= CurrentZone.zonePriority)) selected = CurrentZone;
        }
        else outsideTime = 0;
        bool changed = selected != CurrentZone;
        CamSwitcher previousZone = CurrentZone;
        shotAge += deltaTime;
        if (!evaluatedZones)
        {
            SelectZone(selected);
            evaluatedZones = true;
            shotAge = 0;
        }
        else if (changed)
        {
            if (!pendingChange || pendingZone != selected) { pendingZone = selected; pendingTime = 0; pendingChange = true; }
            pendingTime += deltaTime;
            bool blending = brain != null && brain.IsBlending && !StoryCameraDirector.IsPlaying;
            // Fixed angles and explicit cuts switch immediately. Moving/follow
            // zones keep their safeguards. ShoulderAim blends independently.
            bool enteringCut = UsesCut(selected);
            if (enteringCut || (pendingTime >= switchConfirmationTime && shotAge >= minimumShotDuration && !blending))
            {
                SelectZone(selected);
                outsideTime = shotAge = pendingTime = 0;
                pendingChange = false;
            }
        }
        else { pendingChange = false; pendingTime = 0; }
        if (CurrentZone != null) CurrentZone.UpdateMovingCamera(player.transform, false);
        player.SetShootingZone(CurrentZone);
        if (collision != null)
        {
            collision.fixedShot = !StoryCameraDirector.IsPlaying && (!player.IsAiming || player.CurrentShootingMode == ShootingMode.Classic) &&
                (CurrentZone != null ? CurrentZone.mode == CamSwitcher.CameraMode.Fixed :
                    !followOutsideZones && (lastFixedCamera != null || defaultFixedCamera != null));
            collision.preserveAuthoredPosition = StoryCameraDirector.IsPlaying ||
                (CurrentZone != null ? CurrentZone.mode != CamSwitcher.CameraMode.Follow : !followOutsideZones && (lastFixedCamera != null || defaultFixedCamera != null));
            collision.transitionInProgress = previousZone != CurrentZone || (brain != null && brain.IsBlending) || StoryCameraDirector.IsPlaying;
        }
    }

    private void SelectZone(CamSwitcher zone)
    {
        CurrentZone = zone;
        if (player != null) player.SetShootingZone(zone);
        if (zone != null) zone.BeginShot();
        if (defaultFixedCamera != null) defaultFixedCamera.Priority = 0;
        if (lastFixedCamera != null) lastFixedCamera.Priority = 0;
        foreach (var entry in CamSwitcher.Zones)
            if (entry != null && entry.activeCamera != null) entry.activeCamera.Priority = 0;
        if (zone != null && zone.mode == CamSwitcher.CameraMode.Fixed) lastFixedCamera = zone.activeCamera;
        CinemachineCamera fallback = lastFixedCamera != null ? lastFixedCamera : defaultFixedCamera;
        bool useFollow = zone != null ? zone.mode == CamSwitcher.CameraMode.Follow : followOutsideZones || fallback == null;
        FollowCamera.Priority = useFollow ? 20 : -10;
        // Remove the fallback follow camera from Cinemachine's active camera
        // pool while an authored shot owns the view. The output Camera stays
        // enabled so both zone shots and ShoulderAim can render normally.
        FollowCamera.enabled = useFollow;
        if (!useFollow)
        {
            var selectedCamera = zone != null ? zone.activeCamera : fallback;
            if (selectedCamera != null)
            {
                // An authored camera can be disabled in the Inspector but still
                // assigned to a zone. Priority alone cannot bring it online.
                selectedCamera.enabled = true;
                selectedCamera.Priority = 20;
            }
        }
        if (!StoryCameraDirector.IsPlaying) ApplyExplorationBlend();
    }

    public void ApplyExplorationBlend()
    {
        if (brain == null) return;
        float duration = Mathf.Max(minimumBlendDuration, CurrentZone != null && CurrentZone.blendDurationOverride > 0 ? CurrentZone.blendDurationOverride : blendDuration);
        bool cut = UsesCut(CurrentZone);
        brain.DefaultBlend = new CinemachineBlendDefinition(cut ?
            CinemachineBlendDefinition.Styles.Cut : CinemachineBlendDefinition.Styles.EaseInOut, cut ? 0 : duration);
    }

    private bool UsesCut(CamSwitcher zone)
    {
        if (zone != null) return zone.mode == CamSwitcher.CameraMode.Fixed || zone.cutOnEnter;
        // Returning to the last fixed angle is also a fixed-camera switch.
        return !followOutsideZones && (lastFixedCamera != null || defaultFixedCamera != null);
    }

    private void OnDisable() { if (player != null) player.SetShootingZone(null); }
    private void OnDestroy() { if (brain != null && initialized) brain.DefaultBlend = oldBlend; }
}
