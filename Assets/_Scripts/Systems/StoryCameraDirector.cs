using UnityEngine;
using Unity.Cinemachine;

[DefaultExecutionOrder(-150)]
public sealed class StoryCameraDirector : MonoBehaviour
{
    public enum ShotPhase { Idle, BlendIn, Hold, Return }
    public ShotPhase Phase { get; private set; }
    public static bool IsPlaying => activeDirector != null && activeDirector.Phase != ShotPhase.Idle;
    public static bool BlocksPlayer => IsPlaying && activeDirector.lockPlayer;
    public bool OwnsCamera => activeDirector == this && Phase != ShotPhase.Idle;
    private static StoryCameraDirector activeDirector;
    private ExplorationCameraRig rig;
    private Camera view;
    private CinemachineBrain brain;
    private CinemachineCamera shotCamera;
    private CameraObstacleAvoidance collision;
    private StoryFocusShot shot;
    private HealthManager health;
    private CinemachineBlendDefinition previousBlend;
    private float elapsed, blendIn, hold, blendOut, returnFocusWeight;
    private bool lockPlayer, completed;
    private DialogueSystem storyDialogue;
    private bool dialogueStarted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => activeDirector = null;

    public bool TryPlay(StoryFocusShot value) => TryPlayWithCamera(value, Camera.main);

    internal bool TryPlayWithCamera(StoryFocusShot value, Camera outputCamera)
    {
        if (!isActiveAndEnabled || IsPlaying || value == null || value.subject == null || value.cameraAnchor == null ||
            Time.timeScale == 0 || DialogueSystem.BlocksPlayer) return false;
        rig = GetComponent<ExplorationCameraRig>();
        if (rig == null || rig.player == null || rig.player != value.player || rig.player.IsQuickTurning) return false;
        if (value.dialogue != null && !DialogueSystem.HasValidLines(value.dialogue)) return false;
        storyDialogue = null;
        foreach (var dialogue in FindObjectsByType<DialogueSystem>(FindObjectsSortMode.None))
        {
            if (dialogue.gameObject.scene != rig.player.gameObject.scene) continue;
            if (dialogue.IsPlaying) return false;
            if (dialogue.isActiveAndEnabled) storyDialogue = dialogue;
        }
        health = rig.player.GetComponent<HealthManager>();
        if (health != null && health.IsDead) return false;
        view = outputCamera;
        if (view == null) return false;
        brain = view.GetComponent<CinemachineBrain>();
        if (brain == null) return false;
        if (value.dialogue != null && storyDialogue == null)
        {
            var root = new GameObject("Story Dialogue");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, rig.player.gameObject.scene);
            storyDialogue = root.AddComponent<DialogueSystem>();
            storyDialogue.player = rig.player;
        }
        // End shoulder aiming before taking control; its return blend is disabled
        // below so it cannot overwrite the beginning of this story shot.
        var aiming = rig.player.GetComponent<ShoulderAim>();
        if (aiming != null) aiming.CancelForStoryShot();
        if (!brain.enabled) return false;
        if (shotCamera == null)
        {
            var root = new GameObject("Story Focus Camera");
            root.transform.SetParent(transform, false);
            shotCamera = root.AddComponent<CinemachineCamera>();
            shotCamera.Priority = -100;
        }
        collision = view.GetComponent<CameraObstacleAvoidance>();
        previousBlend = brain.DefaultBlend;
        shot = value;
        blendIn = Mathf.Max(0, value.blendInTime);
        hold = Mathf.Max(0.01f, value.holdDuration);
        blendOut = Mathf.Max(0.01f, value.blendOutTime);
        lockPlayer = value.lockPlayer;
        completed = true;
        elapsed = 0;
        dialogueStarted = false;
        Phase = blendIn > 0 ? ShotPhase.BlendIn : ShotPhase.Hold;
        activeDirector = this;
        PositionShot(0);
        if (collision != null)
        {
            collision.storySubject = value.subject;
            collision.storySubjectOffset = value.subjectOffset;
            collision.storyFocusWeight = blendIn <= 0 ? 1 : 0;
        }
        brain.DefaultBlend = new CinemachineBlendDefinition(blendIn <= 0 ? CinemachineBlendDefinition.Styles.Cut : CinemachineBlendDefinition.Styles.EaseInOut, blendIn);
        shotCamera.Priority = 100;
        value.Started();
        return true;
    }

    private void Update() => Advance(Time.deltaTime);

    internal void Advance(float deltaTime)
    {
        if (Phase == ShotPhase.Idle) return;
        if (rig == null || rig.player == null || (health != null && health.IsDead)) { Finish(false); return; }
        if (deltaTime <= 0) return;
        elapsed += deltaTime;
        if (Phase == ShotPhase.Return)
        {
            if (collision != null) collision.storyFocusWeight = returnFocusWeight * (1 - Mathf.SmoothStep(0, 1, elapsed / blendOut));
            if (elapsed >= blendOut) Finish(completed);
            return;
        }
        if (shot == null || !shot.isActiveAndEnabled || shot.subject == null || shot.cameraAnchor == null)
        {
            BeginReturn(false);
            return;
        }
        float holdProgress = Mathf.Clamp01((elapsed - blendIn) / hold);
        PositionShot(holdProgress);
        Phase = elapsed < blendIn ? ShotPhase.BlendIn : ShotPhase.Hold;
        if (Phase == ShotPhase.Hold && shot.dialogue != null && !dialogueStarted)
        {
            if (storyDialogue == null || !storyDialogue.PlayForStory(shot.dialogue, this)) { BeginReturn(false); return; }
            dialogueStarted = true;
        }
        if (collision != null) collision.storyFocusWeight = blendIn <= 0 ? 1 : Mathf.SmoothStep(0, 1, elapsed / blendIn);
        bool waitingForDialogue = shot.waitForDialogue && dialogueStarted && storyDialogue != null && storyDialogue.IsPlayingFor(this);
        if (elapsed >= blendIn + hold && !waitingForDialogue) BeginReturn(true);
    }

    private void PositionShot(float progress)
    {
        Vector3 position = shot.cameraAnchor.position;
        if (shot.cameraEnd != null) position = Vector3.Lerp(position, shot.cameraEnd.position, Mathf.SmoothStep(0, 1, progress));
        Vector3 direction = shot.subject.position + shot.subjectOffset - position;
        shotCamera.transform.SetPositionAndRotation(position, direction.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(direction) : shot.cameraAnchor.rotation);
        var lens = shotCamera.Lens;
        lens.FieldOfView = shot.fieldOfView;
        shotCamera.Lens = lens;
    }

    private void BeginReturn(bool success)
    {
        completed = success;
        Phase = ShotPhase.Return;
        elapsed = 0;
        returnFocusWeight = collision != null ? collision.storyFocusWeight : 0;
        if (storyDialogue != null && storyDialogue.IsPlayingFor(this)) storyDialogue.Stop();
        if (brain != null) brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, blendOut);
        if (shotCamera != null) shotCamera.Priority = -100;
    }

    public void CancelShot()
    {
        if (Phase != ShotPhase.Idle && Phase != ShotPhase.Return) BeginReturn(false);
    }

    private void Finish(bool success)
    {
        if (Phase == ShotPhase.Idle) return;
        var finishedShot = shot;
        shot = null;
        Phase = ShotPhase.Idle;
        if (shotCamera != null) shotCamera.Priority = -100;
        if (activeDirector == this) activeDirector = null;
        if (storyDialogue != null && storyDialogue.IsPlayingFor(this)) storyDialogue.Stop();
        if (collision != null)
        {
            collision.storySubject = null;
            collision.storyFocusWeight = 0;
        }
        if (brain != null) brain.DefaultBlend = previousBlend;
        if (rig != null) rig.ApplyExplorationBlend();
        if (finishedShot != null) finishedShot.Finished(success);
    }

    private void OnDisable() => Finish(false);
}
