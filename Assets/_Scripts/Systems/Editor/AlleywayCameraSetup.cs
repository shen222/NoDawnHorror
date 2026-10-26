using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Cinemachine;

public static class AlleywayCameraSetup
{
    [MenuItem("Tools/Cameras/Set Up Smooth Alleyway And Deadman Discovery")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode first.");
        var player = UnityEngine.Object.FindFirstObjectByType<TankControls>();
        var alley = GameObject.Find("TriggerZone alleyway");
        var corpseRoot = GameObject.Find("deadman");
        var bodyCameraRoot = GameObject.Find("CinemachineCamera Deadbody");
        if (player == null || alley == null || corpseRoot == null || bodyCameraRoot == null)
            throw new InvalidOperationException("Open MainGame with TriggerZone alleyway, deadman and CinemachineCamera Deadbody present.");
        var zone = alley.GetComponent<CamSwitcher>();
        var box = alley.GetComponent<BoxCollider>();
        var bodyCamera = bodyCameraRoot.GetComponent<CinemachineCamera>();
        if (zone == null || box == null || zone.activeCamera == null || bodyCamera == null)
            throw new InvalidOperationException("The authored alley trigger and cameras must be assigned before setup.");
        var renderers = corpseRoot.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) throw new InvalidOperationException("The deadman object has no visible body renderer.");
        Bounds bodyBounds = renderers[0].bounds;
        foreach (var renderer in renderers) bodyBounds.Encapsulate(renderer.bounds);
        // The container's pivot is far from its visible model. Anchor proximity
        // and focus to the actual body, not the container's transform.
        Transform subject = renderers[0].transform;
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Smooth alleyway and deadman discovery");
        var rig = UnityEngine.Object.FindFirstObjectByType<ExplorationCameraRig>();
        if (rig == null)
        {
            var root = new GameObject("Exploration Camera Rig");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, player.gameObject.scene);
            Undo.RegisterCreatedObjectUndo(root, "Create exploration rig");
            rig = root.AddComponent<ExplorationCameraRig>();
        }
        Undo.RecordObject(rig, "Calm camera transitions");
        rig.player = player;
        rig.followOutsideZones = false;
        rig.minimumBlendDuration = 1.4f;
        rig.blendDuration = 1.4f;
        rig.minimumShotDuration = 1.6f;
        rig.switchConfirmationTime = 0.3f;
        rig.smoothZoneTransitions = true;
        EditorUtility.SetDirty(rig);

        Vector3 size = Vector3.Scale(box.size, box.transform.lossyScale);
        bool alongX = Mathf.Abs(size.x) > Mathf.Abs(size.z);
        Vector3 direction = alongX ? box.transform.right : box.transform.forward;
        direction = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
        Vector3 towardCamera = Vector3.ProjectOnPlane(-zone.activeCamera.transform.forward, Vector3.up);
        if (Vector3.Dot(direction, towardCamera) < 0) direction = -direction;
        float length = Mathf.Abs(alongX ? size.x : size.z);
        Vector3 center = box.transform.TransformPoint(box.center);
        center.y = player.transform.position.y;
        Undo.RecordObject(zone, "Configure alley dolly");
        if (zone.pathStart == null || zone.pathEnd == null || zone.cameraStart == null || zone.cameraEnd == null)
        {
            var markers = new GameObject("Alleyway Dolly Markers");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(markers, player.gameObject.scene);
            Undo.RegisterCreatedObjectUndo(markers, "Create alley dolly markers");
            Vector3 bodyOnRoute = center + direction * Vector3.Dot(bodyBounds.center - center, direction);
            zone.pathStart = Marker(markers.transform, "Player Path Start", center - direction * length * 0.45f);
            zone.pathEnd = Marker(markers.transform, "Player Path End", bodyOnRoute - direction * 1.6f);
            zone.cameraStart = Marker(markers.transform, "Camera Start", bodyOnRoute - direction * 2f + Vector3.up * 1.8f);
            zone.cameraEnd = Marker(markers.transform, "Camera End", bodyOnRoute - direction * 0.2f + Vector3.up * 3.2f);
        }
        zone.mode = CamSwitcher.CameraMode.Moving;
        zone.player = player.transform;
        zone.zonePriority = 20;
        zone.cutOnEnter = false;
        zone.blendDurationOverride = 1.6f;
        zone.exitDelay = 0.5f;
        zone.movementDamping = 0.8f;
        zone.rotationDamping = 0.65f;
        zone.maximumTravelSpeed = 2f;
        zone.maximumTurnSpeed = 25f;
        EditorUtility.SetDirty(zone);
        Undo.RecordObject(zone.activeCamera, "Set comfortable alley field of view");
        var alleyLens = zone.activeCamera.Lens;
        alleyLens.FieldOfView = 52;
        zone.activeCamera.Lens = alleyLens;
        EditorUtility.SetDirty(zone.activeCamera);

        var storyRoot = GameObject.Find("Deadman Discovery");
        if (storyRoot == null)
        {
            storyRoot = new GameObject("Deadman Discovery");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(storyRoot, player.gameObject.scene);
            Undo.RegisterCreatedObjectUndo(storyRoot, "Create deadman discovery");
        }
        var shot = storyRoot.GetComponent<StoryFocusShot>();
        if (shot == null) shot = Undo.AddComponent<StoryFocusShot>(storyRoot);
        Undo.RecordObject(shot, "Configure deadman story shot");
        shot.player = player;
        shot.subject = subject;
        shot.subjectOffset = bodyBounds.center - subject.position;
        if (shot.cameraAnchor == null)
            shot.cameraAnchor = Marker(storyRoot.transform, "Deadman Camera Position", bodyCamera.transform.position);
        shot.holdDuration = 1.5f;
        shot.blendInTime = shot.blendOutTime = 1.2f;
        shot.fieldOfView = 52;
        shot.lockPlayer = shot.playOnce = shot.playOnEnter = true;
        shot.triggerByDistance = true;
        shot.proximityRadius = 3f;
        shot.waitForDialogue = true;
        shot.dialogue = DiscoveryDialogue();
        EditorUtility.SetDirty(shot);
        // The old persistent body-camera zone would otherwise repeat this reveal.
        foreach (var oldZone in UnityEngine.Object.FindObjectsByType<CamSwitcher>(FindObjectsSortMode.None))
        {
            if (oldZone.activeCamera != bodyCamera) continue;
            Undo.RecordObject(oldZone, "Use one-time discovery instead of a repeated body-camera zone");
            oldZone.enabled = false;
            EditorUtility.SetDirty(oldZone);
        }
        Undo.RecordObject(bodyCamera, "Reserve body camera for story composition");
        bodyCamera.Priority = -100;
        EditorUtility.SetDirty(bodyCamera);
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        Selection.activeObject = zone;
        Debug.Log("Alleyway configured: eased 1.4–1.6s transitions, stable zones, slow retreat/rise. Deadman discovery is bound to the visible body, plays once within 3m, and holds Japanese dialogue before returning. Review and save MainGame.", zone);
    }

    private static Transform Marker(Transform parent, string name, Vector3 position)
    {
        var marker = new GameObject(name).transform;
        marker.SetParent(parent, false);
        marker.position = position;
        return marker;
    }

    private static DialogueSequence DiscoveryDialogue()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Dialogue")) AssetDatabase.CreateFolder("Assets/Resources", "Dialogue");
        const string path = "Assets/Resources/Dialogue/DeadmanDiscovery.asset";
        var dialogue = AssetDatabase.LoadAssetAtPath<DialogueSequence>(path);
        if (dialogue != null) return dialogue;
        dialogue = ScriptableObject.CreateInstance<DialogueSequence>();
        dialogue.lockPlayer = true;
        dialogue.autoAdvance = true;
        dialogue.lines = new[] {new DialogueSequence.Line {text = "死んでる……ここで何があったの？", duration = 4.5f}};
        AssetDatabase.CreateAsset(dialogue, path);
        return dialogue;
    }
}
