using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Unity.Cinemachine;

public static class StoryCameraValidation
{
    [MenuItem("Tools/Cameras/Validate Story Cameras")]
    public static void Validate()
    {
        var root = new GameObject("Story camera validation");
        root.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            var player = Child(root.transform, "Player").AddComponent<TankControls>();
            var rigRoot = Child(root.transform, "Rig");
            var rig = rigRoot.AddComponent<ExplorationCameraRig>();
            rig.player = player;
            Check(!rig.followOutsideZones && !rig.enableCinematicZones, "Fixed cameras must remain the default");
            var director = rigRoot.AddComponent<StoryCameraDirector>();
            var cameraRoot = Child(root.transform, "Output");
            var output = cameraRoot.AddComponent<Camera>();
            output.enabled = false;
            var brain = cameraRoot.AddComponent<CinemachineBrain>();
            var originalBlend = brain.DefaultBlend;
            var collision = cameraRoot.AddComponent<CameraObstacleAvoidance>();
            collision.enabled = false;
            collision.player = player.transform;
            var shot = Child(root.transform, "Shot").AddComponent<StoryFocusShot>();
            shot.player = player;
            shot.subject = Child(root.transform, "Subject").transform;
            shot.cameraAnchor = Child(root.transform, "Anchor").transform;
            shot.cameraAnchor.position = new Vector3(4, 2, -4);
            shot.blendInTime = 0.25f;
            shot.holdDuration = 1;
            shot.blendOutTime = 0.25f;
            int started = 0, finished = 0, cancelled = 0;
            shot.onStarted.AddListener(() => started++);
            shot.onFinished.AddListener(() => finished++);
            shot.onCancelled.AddListener(() => cancelled++);
            var start = typeof(StoryCameraDirector).GetMethod("TryPlayWithCamera", BindingFlags.Instance | BindingFlags.NonPublic);
            var advance = typeof(StoryCameraDirector).GetMethod("Advance", BindingFlags.Instance | BindingFlags.NonPublic);
            if (start == null || advance == null) throw new InvalidOperationException("Missing story camera lifecycle methods");
            Func<bool> play = () => (bool)start.Invoke(director, new object[] { shot, output });
            Action<float> tick = delta => advance.Invoke(director, new object[] { delta });
            Check(play(), "Story shot should start");
            Check(StoryCameraDirector.IsPlaying && StoryCameraDirector.BlocksPlayer, "Story should own the camera and lock input");
            Check(!play(), "Overlapping story requests must be rejected");
            Check(shot.HasPlayed && !shot.TryPlay(), "One-shot trigger must not replay");
            tick(0);
            Check(director.Phase == StoryCameraDirector.ShotPhase.BlendIn, "Pause must freeze the timer");
            tick(0.25f);
            Check(director.Phase == StoryCameraDirector.ShotPhase.Hold, "Hold begins after blending in");
            Check(collision.storySubject == shot.subject && Mathf.Approximately(collision.storyFocusWeight, 1), "Obstacle pivot must follow the story subject");
            tick(0.99f);
            Check(director.Phase == StoryCameraDirector.ShotPhase.Hold, "Subject must remain visible for the entire hold duration");
            tick(0.02f);
            Check(director.Phase == StoryCameraDirector.ShotPhase.Return && finished == 0 && StoryCameraDirector.BlocksPlayer, "Return blend must finish before releasing input");
            tick(0.26f);
            Check(!StoryCameraDirector.IsPlaying && !StoryCameraDirector.BlocksPlayer && finished == 1 && started == 1, "Normal return must release control once");
            Check(collision.storySubject == null && collision.storyFocusWeight == 0, "Collision pivot must return to exploration");
            Check(brain.DefaultBlend.Style == originalBlend.Style && Mathf.Approximately(brain.DefaultBlend.Time, originalBlend.Time), "Original blend must be restored");
            Check(play(), "Director must accept another shot after finishing");
            shot.subject = null;
            tick(0.1f);
            Check(director.Phase == StoryCameraDirector.ShotPhase.Return, "Missing subjects must cancel safely");
            tick(0.26f);
            Check(cancelled == 1 && finished == 1 && !StoryCameraDirector.IsPlaying, "Cancellation must release control and notify once");
            shot.subject = Child(root.transform, "Replacement subject").transform;
            Check(play(), "Director must recover after cancellation");
            director.enabled = false;
            // Non-ExecuteAlways behaviours do not receive their runtime lifecycle
            // callbacks in an Edit-mode batch check, so exercise that callback explicitly.
            typeof(StoryCameraDirector).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(director, null);
            Check(!StoryCameraDirector.IsPlaying && !StoryCameraDirector.BlocksPlayer && cancelled == 2, "Disabling the director must not leave input locked");
            Debug.Log("PASS: fixed defaults, one-second hold, pause, concurrent requests, one-shot replay, return blend, collision pivot, missing subject, disable cleanup and callbacks.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    public static void RunBatch()
    {
        try
        {
            Validate();
            File.WriteAllText("story-validation-result.txt", "PASS: fixed defaults, timed hold, pause, concurrent requests, one-shot replay, return blend, collision pivot, missing subject, disable cleanup and callbacks.");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            File.WriteAllText("story-validation-result.txt", exception.ToString());
            EditorApplication.Exit(1);
        }
    }

    private static GameObject Child(Transform parent, string name)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
