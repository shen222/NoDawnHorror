using UnityEngine;
using UnityEngine.Events;

// A timed story beat. It is independent of the exploration camera zones.
[DefaultExecutionOrder(-175)]
public sealed class StoryFocusShot : MonoBehaviour
{
    public TankControls player;
    public Transform subject;
    public Vector3 subjectOffset = Vector3.up;
    public Transform cameraAnchor;
    [Tooltip("Optional endpoint for a slow camera move during the hold.")]
    public Transform cameraEnd;
    [Min(0.01f)] public float holdDuration = 1f;
    [Min(0)] public float blendInTime = 0.4f;
    [Min(0.01f)] public float blendOutTime = 0.4f;
    [Range(10, 100)] public float fieldOfView = 50f;
    public bool lockPlayer = true;
    public bool playOnEnter = true;
    public bool playOnce = true;
    [Header("Story dialogue")]
    public DialogueSequence dialogue;
    [Tooltip("Keep the shot until its dialogue finishes, in addition to the minimum hold duration.")]
    public bool waitForDialogue = true;
    [Header("Optional distance trigger")]
    public bool triggerByDistance;
    [Min(0.1f)] public float proximityRadius = 3f;
    public UnityEvent onStarted = new UnityEvent();
    public UnityEvent onFinished = new UnityEvent();
    public UnityEvent onCancelled = new UnityEvent();
    public bool HasPlayed { get; private set; }
    private bool entered;
    private Collider volume;

    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<TankControls>();
        volume = GetComponent<Collider>();
    }

    private void Update()
    {
        if (!playOnEnter || player == null || Time.timeScale == 0) return;
        // Position testing works with CharacterController without adding a Rigidbody.
        Vector3 point = player.transform.position + Vector3.up * 0.25f;
        bool inside;
        if (triggerByDistance)
        {
            if (subject == null) return;
            Vector3 destination = subject.position;
            destination.y = point.y;
            inside = (destination - point).sqrMagnitude <= proximityRadius * proximityRadius;
        }
        else
        {
            if (volume == null || !volume.enabled) return;
            inside = (volume.ClosestPoint(point) - point).sqrMagnitude < 0.0001f;
        }
        if (!inside) { entered = false; return; }
        if (!entered && TryPlay()) entered = true;
    }

    // This void wrapper can also be called from a UnityEvent or a story script.
    public void PlayShot() => TryPlay();

    public bool TryPlay()
    {
        if (!isActiveAndEnabled || (playOnce && HasPlayed) || subject == null || cameraAnchor == null) return false;
        if (player == null) player = FindFirstObjectByType<TankControls>();
        if (player == null) return false;
        ExplorationCameraRig rig = null;
        foreach (var candidate in FindObjectsByType<ExplorationCameraRig>(FindObjectsSortMode.None))
            if (candidate.player == player && candidate.gameObject.scene == player.gameObject.scene) { rig = candidate; break; }
        if (rig == null) return false;
        var director = rig.GetComponent<StoryCameraDirector>();
        if (director == null) director = rig.gameObject.AddComponent<StoryCameraDirector>();
        return director.TryPlay(this);
    }

    internal void Started()
    {
        HasPlayed = true;
        onStarted.Invoke();
    }

    internal void Finished(bool completed)
    {
        if (completed) onFinished.Invoke();
        else onCancelled.Invoke();
    }
}


