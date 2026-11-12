using System;
using Unity.Cinemachine;
using UnityEngine;

// Run after both Cinemachine and ShoulderAim, including their camera blends.
[DefaultExecutionOrder(11000)]
[RequireComponent(typeof(Camera))]
public sealed class CameraObstacleAvoidance : MonoBehaviour
{
    public Transform player;
    public Vector3 playerPivotOffset = new Vector3(0, 1.3f, 0);
    public LayerMask obstacleLayers = ~0;
    [Min(0.01f)] public float collisionRadius = 0.25f;
    [Min(0.001f)] public float surfacePadding = 0.03f;
    [Min(0)] public float returnDamping = 0.35f;
    // Story shots move the obstruction pivot toward the story subject instead of
    // pulling a remote shot back toward the player behind a wall.
    public Transform storySubject { get; set; }
    public Vector3 storySubjectOffset { get; set; }
    public float storyFocusWeight { get; set; }
    public bool preserveAuthoredPosition { get; set; }
    public bool transitionInProgress { get; set; }
    public bool fixedShot { get; set; }

    private Camera view;
    private CinemachineBrain brain;
    private ICinemachineCamera previousLiveCamera;
    private SphereCollider probe;
    private RaycastHit[] hits = new RaycastHit[32];
    private Collider[] overlaps = new Collider[32];
    private Vector3 previousPosition;
    private bool recovering;
    private bool hasPreviousPosition;
    private ShoulderAim firstPersonAim;

    private void Awake()
    {
        view = GetComponent<Camera>();
        brain = GetComponent<CinemachineBrain>();
    }

    private void LateUpdate()
    {
        if (player == null) return;
        var liveCamera = brain != null ? brain.ActiveVirtualCamera : null;
        bool liveCameraChanged = !ReferenceEquals(liveCamera, previousLiveCamera);
        previousLiveCamera = liveCamera;
        if (firstPersonAim == null || firstPersonAim.transform != player)
            firstPersonAim = player.GetComponent<ShoulderAim>();
        if (firstPersonAim != null && firstPersonAim.OwnsCamera)
        {
            hasPreviousPosition = recovering = false;
            return;
        }
        // Recovery belongs to the previous shot. Carrying it across a cut can
        // pull a correctly selected Cinemachine camera back to the old view.
        // The brain can apply the cut a frame after the zone changes.
        // The new shot still gets the near-plane overlap check below.
        if (fixedShot || liveCameraChanged || (preserveAuthoredPosition && transitionInProgress))
        {
            recovering = false;
            hasPreviousPosition = false;
        }
        Vector3 pivot = player.position + playerPivotOffset;
        if (storySubject != null)
            pivot = Vector3.Lerp(pivot, storySubject.position + storySubjectOffset, storyFocusWeight);
        Vector3 desired = transform.position;
        // A fixed shot may intentionally have scenery between it and the player.
        // Sweep the camera's own travel instead of dragging it toward the player.
        // During a handoff the two authored cameras may be on opposite sides of
        // scenery. A sweep from the old shot would pin the new shot there forever.
        // Keep checking near-plane overlaps during blends; sweep continuous travel
        // once the new shot owns the view.
        if (preserveAuthoredPosition) pivot = !fixedShot && hasPreviousPosition && !transitionInProgress ? previousPosition : desired;
        float radius = GetProtectionRadius();
        Vector3 safe = ResolvePosition(pivot, desired, radius);
        if ((safe - desired).sqrMagnitude > 0.000001f)
        {
            // Never smooth through a wall when an obstacle first enters the shot.
            recovering = true;
        }
        else if (recovering)
        {
            float blend = returnDamping <= 0 ? 1 : 1 - Mathf.Exp(-Time.deltaTime / returnDamping);
            safe = ResolvePosition(pivot, Vector3.Lerp(previousPosition, desired, blend), radius);
            recovering = (safe - desired).sqrMagnitude > 0.0001f;
        }
        transform.position = safe;
        previousPosition = safe;
        hasPreviousPosition = true;
    }

    public float GetProtectionRadius()
    {
        if (view == null) view = GetComponent<Camera>();
        // Protect the whole near clipping plane, not just the camera's centre.
        float height = view.orthographic ? view.orthographicSize :
            view.nearClipPlane * Mathf.Tan(view.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float width = height * view.aspect;
        return Mathf.Max(collisionRadius, Mathf.Sqrt(width * width + height * height + view.nearClipPlane * view.nearClipPlane));
    }

    public Vector3 ResolvePosition(Vector3 pivot, Vector3 desired, float radius)
    {
        radius = Mathf.Max(0.01f, radius);
        Vector3 safe = Sweep(pivot, desired, radius + surfacePadding);
        EnsureProbe();
        probe.radius = radius + surfacePadding;
        // Unity needs an enabled native shape for ComputePenetration. Disable it
        // again before returning, so no physics simulation sees the probe.
        probe.enabled = true;
        try
        {
            // Sphere casts do not report initial overlaps. Resolve those separately,
            // including a camera spawned inside a wall and multiple adjoining walls.
            for (int iteration = 0; iteration < 8; iteration++)
            {
                int count;
                while ((count = Physics.OverlapSphereNonAlloc(safe, probe.radius, overlaps, obstacleLayers, QueryTriggerInteraction.Ignore)) == overlaps.Length)
                    Array.Resize(ref overlaps, overlaps.Length * 2);
                bool moved = false;
                for (int i = 0; i < count; i++)
                {
                    Collider obstacle = overlaps[i];
                    if (Ignore(obstacle)) continue;
                    if (!Physics.ComputePenetration(probe, safe, Quaternion.identity,
                        obstacle, obstacle.transform.position, obstacle.transform.rotation,
                        out Vector3 direction, out float distance)) continue;
                    safe += direction * (distance + surfacePadding);
                    moved = true;
                }
                if (!moved) break;
            }
            return safe;
        }
        finally { probe.enabled = false; }
    }

    private Vector3 Sweep(Vector3 pivot, Vector3 desired, float radius)
    {
        Vector3 offset = desired - pivot;
        float distance = offset.magnitude;
        if (distance < 0.0001f) return desired;
        Vector3 direction = offset / distance;
        int count;
        while ((count = Physics.SphereCastNonAlloc(pivot, radius, direction, hits, distance, obstacleLayers, QueryTriggerInteraction.Ignore)) == hits.Length)
            Array.Resize(ref hits, hits.Length * 2);
        float allowed = distance;
        for (int i = 0; i < count; i++)
            if (!Ignore(hits[i].collider)) allowed = Mathf.Min(allowed, Mathf.Max(0, hits[i].distance - surfacePadding));
        return pivot + direction * allowed;
    }

    private bool Ignore(Collider obstacle)
    {
        return obstacle == null || obstacle == probe ||
            (player != null && obstacle.transform.IsChildOf(player)) ||
            (storySubject != null && obstacle.transform.IsChildOf(storySubject)) ||
            obstacle.transform.IsChildOf(transform);
    }

    private void EnsureProbe()
    {
        if (probe != null) return;
        var root = new GameObject("Camera collision probe");
        root.hideFlags = HideFlags.HideAndDontSave;
        root.transform.position = Vector3.down * 100000;
        root.transform.localScale = Vector3.one;
        probe = root.AddComponent<SphereCollider>();
        // Used only by ComputePenetration; it never participates in gameplay physics.
        probe.enabled = false;
    }

    private void OnDisable() { recovering = false; hasPreviousPosition = false; }

    private void OnDestroy()
    {
        if (probe == null) return;
        if (Application.isPlaying) Destroy(probe.gameObject);
        else DestroyImmediate(probe.gameObject);
    }
}
