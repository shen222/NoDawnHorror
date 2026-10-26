using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class StoryCameraSetup
{
    [MenuItem("Tools/Cameras/Use Fixed Exploration Cameras")]
    public static void UseFixedCameras()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode first.");
        var player = UnityEngine.Object.FindFirstObjectByType<TankControls>();
        if (player == null) throw new InvalidOperationException("Open your gameplay scene first.");
        var rig = GetRig(player);
        Undo.RecordObject(rig, "Use fixed exploration cameras");
        rig.followOutsideZones = false;
        rig.enableCinematicZones = false;
        EditorUtility.SetDirty(rig);
        EditorSceneManager.MarkSceneDirty(rig.gameObject.scene);
        Selection.activeObject = rig;
    }

    [MenuItem("Tools/Cameras/Create Story Focus Shot")]
    public static void CreateStoryShot()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode first.");
        Transform subject = Selection.activeTransform;
        if (subject == null || !subject.gameObject.scene.IsValid())
            throw new InvalidOperationException("Select the scene object you want to show in the story shot.");
        var player = UnityEngine.Object.FindFirstObjectByType<TankControls>();
        if (player == null || player.gameObject.scene != subject.gameObject.scene)
            throw new InvalidOperationException("Select a subject in the player's gameplay scene.");
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Create story focus shot");
        GetRig(player);
        var root = new GameObject("Story Focus - " + subject.name);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, player.gameObject.scene);
        Undo.RegisterCreatedObjectUndo(root, "Create story focus shot");
        root.transform.position = player.transform.position + player.transform.forward * 2;
        var volume = root.AddComponent<BoxCollider>();
        volume.isTrigger = true;
        volume.size = new Vector3(3, 4, 3);
        volume.center = Vector3.up;
        var shot = root.AddComponent<StoryFocusShot>();
        shot.player = player;
        shot.subject = subject;
        var anchor = new GameObject("Story Camera Position").transform;
        anchor.SetParent(root.transform, false);
        anchor.position = subject.position - player.transform.forward * 4 + player.transform.right * 2 + Vector3.up * 2;
        Vector3 direction = subject.position + shot.subjectOffset - anchor.position;
        anchor.rotation = Quaternion.LookRotation(direction);
        shot.cameraAnchor = anchor;
        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(root.scene);
        Undo.CollapseUndoOperations(group);
        Debug.Log("Story shot created: entering this volume shows the subject for 1 second, then returns to exploration. Compose Story Camera Position and resize the trigger. Disable Play On Enter to call PlayShot from a story event instead. Save the scene when ready.", root);
    }

    [MenuItem("Tools/Cameras/Align Selected Story Shot to Scene View")]
    public static void AlignToSceneView()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode first.");
        var shot = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<StoryFocusShot>() : null;
        var view = SceneView.lastActiveSceneView;
        if (shot == null || shot.cameraAnchor == null || view == null)
            throw new InvalidOperationException("Select a story shot and compose the view in Scene view first.");
        Undo.RecordObject(shot.cameraAnchor, "Compose story shot");
        shot.cameraAnchor.SetPositionAndRotation(view.camera.transform.position, view.camera.transform.rotation);
        EditorSceneManager.MarkSceneDirty(shot.gameObject.scene);
    }

    private static ExplorationCameraRig GetRig(TankControls player)
    {
        foreach (var rig in UnityEngine.Object.FindObjectsByType<ExplorationCameraRig>(FindObjectsSortMode.None))
            if (rig.gameObject.scene == player.gameObject.scene) return rig;
        var root = new GameObject("Exploration Camera Rig");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, player.gameObject.scene);
        Undo.RegisterCreatedObjectUndo(root, "Create camera rig");
        var created = root.AddComponent<ExplorationCameraRig>();
        created.player = player;
        return created;
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
    private static void DrawShot(StoryFocusShot shot, GizmoType type)
    {
        if (shot.cameraAnchor == null || shot.subject == null) return;
        Gizmos.color = Color.magenta;
        Vector3 focus = shot.subject.position + shot.subjectOffset;
        Gizmos.DrawLine(shot.cameraAnchor.position, focus);
        Gizmos.DrawWireSphere(focus, 0.25f);
        Handles.Label(shot.cameraAnchor.position, "Story camera");
        Handles.Label(focus, "Story subject (" + shot.holdDuration + "s)");
        if (shot.cameraEnd != null) Gizmos.DrawLine(shot.cameraAnchor.position, shot.cameraEnd.position);
    }
}
