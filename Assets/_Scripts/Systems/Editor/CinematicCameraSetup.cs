using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Cinemachine;

// Shots are scene objects with independent markers, so moving the camera never moves its rail.
public static class CinematicCameraSetup
{
    private const string Request = "Logs/CinematicCameraSetup.request";

    [InitializeOnLoadMethod]
    private static void ScheduleSetup()
    {
        EditorApplication.delayCall += ApplyRequested;
        EditorSceneManager.sceneOpened += (_, __) => ApplyRequested();
    }

    private static void ApplyRequested()
    {
        if (!File.Exists(Request)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += ApplyRequested;
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainGame") return;
        // Consume the request before creating anything, so an error cannot repeat scene edits.
        File.Delete(Request);
        try
        {
            Validate();
            File.WriteAllText("Logs/CinematicCameraSetup.txt", "PASS: cinematic movement checks. Fixed exploration remains the default; cinematic movement is opt-in. Create separate timed story shots with Tools/Cameras/Create Story Focus Shot.");
        }
        catch (Exception exception)
        {
            File.WriteAllText("Logs/CinematicCameraSetup.txt", exception.ToString());
            Debug.LogException(exception);
        }
    }

    [MenuItem("Tools/Cameras/Create Cinematic Rising Shot")]
    public static void CreateRisingShot() => CreateShot(null);

    [MenuItem("Tools/Cameras/Create Cinematic Movement With Object Focus")]
    public static void CreateObjectReveal()
    {
        Transform focus = Selection.activeTransform;
        if (focus == null || !focus.gameObject.scene.IsValid())
            throw new InvalidOperationException("Select the scene object you want the camera to reveal first.");
        CreateShot(focus);
    }

    private static void CreateShot(Transform focus)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play mode before creating a cinematic shot.");
        var player = UnityEngine.Object.FindFirstObjectByType<TankControls>();
        if (player == null) throw new InvalidOperationException("Open your gameplay scene first.");
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Create cinematic camera shot");
        var root = new GameObject(focus == null ? "Cinematic Shot - Rising" : "Cinematic Shot - Object Reveal");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, player.gameObject.scene);
        Undo.RegisterCreatedObjectUndo(root, "Create cinematic shot");
        Vector3 forward = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 center = player.transform.position + forward * 5;
        root.transform.SetPositionAndRotation(center, Quaternion.LookRotation(forward));
        var volume = root.AddComponent<BoxCollider>();
        volume.isTrigger = true;
        volume.center = Vector3.up * 2;
        volume.size = new Vector3(8, 6, 16);
        var zone = root.AddComponent<CamSwitcher>();
        zone.mode = CamSwitcher.CameraMode.Cinematic;
        zone.player = player.transform;
        zone.zonePriority = 20;
        zone.progressMode = CamSwitcher.ProgressMode.ApproachMarker;
        zone.farDistance = 12;
        zone.nearDistance = 2;
        zone.movementDamping = 0.6f;
        zone.rotationDamping = 0.45f;
        zone.approachMarker = Marker(root.transform, "Approach Point", center + forward * 5);
        zone.pathStart = Marker(root.transform, "Player Path Start", center - forward * 7);
        zone.pathEnd = Marker(root.transform, "Player Path End", center + forward * 7);
        Vector3 start = center + right * 5 - forward * 3 + Vector3.up * 2.2f;
        Vector3 end = center + right * 4 + forward * 2 + Vector3.up * 6;
        zone.cameraStart = Marker(root.transform, "Camera Start", start);
        zone.cameraEnd = Marker(root.transform, "Camera End", end);
        var cameraRoot = new GameObject("Shot Camera");
        cameraRoot.transform.SetParent(root.transform, false);
        cameraRoot.transform.SetPositionAndRotation(start, Quaternion.LookRotation(player.transform.position + Vector3.up * 1.3f - start));
        zone.activeCamera = cameraRoot.AddComponent<CinemachineCamera>();
        zone.activeCamera.Priority = 0;
        var lens = zone.activeCamera.Lens;
        lens.FieldOfView = 52;
        zone.activeCamera.Lens = lens;
        zone.focusTarget = focus;
        // Hold attention on the player during the first part, then reveal the object.
        zone.focusWeight = AnimationCurve.EaseInOut(0.25f, 0, 1, 1);
        var rig = UnityEngine.Object.FindFirstObjectByType<ExplorationCameraRig>();
        if (rig == null)
        {
            var rigRoot = new GameObject("Exploration Camera Rig");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(rigRoot, player.gameObject.scene);
            Undo.RegisterCreatedObjectUndo(rigRoot, "Create camera rig");
            rig = rigRoot.AddComponent<ExplorationCameraRig>();
            rig.player = player;
        }
        Undo.RecordObject(rig, "Enable cinematic movement zones");
        rig.enableCinematicZones = true;
        EditorUtility.SetDirty(rig);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        Selection.activeGameObject = root;
        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log("Cinematic shot created ahead of the player. Move Camera Start / End and Approach Point to compose the shot. Assign Focus Target to reveal an object, or change Progress Mode to Along Path. Save the scene when ready.", root);
    }

    private static Transform Marker(Transform parent, string name, Vector3 position)
    {
        var marker = new GameObject(name).transform;
        marker.SetParent(parent, false);
        marker.position = position;
        return marker;
    }

    [MenuItem("Tools/Cameras/Validate Cinematic Cameras")]
    public static void Validate()
    {
        var root = new GameObject("Cinematic Camera Validation");
        root.SetActive(false);
        try
        {
            var zone = root.AddComponent<CamSwitcher>();
            zone.mode = CamSwitcher.CameraMode.Cinematic;
            zone.progressMode = CamSwitcher.ProgressMode.ApproachMarker;
            zone.farDistance = 10;
            zone.nearDistance = 2;
            zone.movementDamping = zone.rotationDamping = 0;
            zone.approachMarker = Marker(root.transform, "Approach", Vector3.zero);
            zone.cameraStart = Marker(root.transform, "Start", new Vector3(5, 2, -4));
            zone.cameraEnd = Marker(root.transform, "End", new Vector3(5, 6, 4));
            var target = Marker(root.transform, "Player", new Vector3(0, 30, -10));
            zone.activeCamera = Marker(root.transform, "Camera", zone.cameraStart.position).gameObject.AddComponent<CinemachineCamera>();
            zone.focusTarget = Marker(root.transform, "Object", new Vector3(12, 4, 3));
            zone.animateFieldOfView = true;
            var update = typeof(CamSwitcher).GetMethod("UpdateMovingCamera", BindingFlags.Instance | BindingFlags.NonPublic);
            if (update == null) throw new InvalidOperationException("Missing camera update method.");
            Action evaluate = () => update.Invoke(zone, new object[] { target, true });
            if (!Mathf.Approximately(CamSwitcher.ApproachProgress(target.position, Vector3.zero, 10, 2), 0))
                throw new InvalidOperationException("Approach should ignore player height.");
            evaluate();
            AssertPosition(zone.activeCamera.transform.position, zone.cameraStart.position, "Far endpoint");
            target.position = Vector3.back * 2;
            evaluate();
            AssertPosition(zone.activeCamera.transform.position, zone.cameraEnd.position, "Near endpoint");
            if (Vector3.Dot(zone.activeCamera.transform.forward, (zone.focusTarget.position - zone.cameraEnd.position).normalized) < 0.999f)
                throw new InvalidOperationException("Object reveal did not face the target.");
            if (!Mathf.Approximately(zone.activeCamera.Lens.FieldOfView, zone.endFieldOfView))
                throw new InvalidOperationException("Lens did not reach its endpoint.");
            target.position = Vector3.back * 6;
            zone.cameraControl = Marker(root.transform, "Curve", new Vector3(10, 10, 0));
            evaluate();
            AssertPosition(zone.activeCamera.transform.position, 0.25f * zone.cameraStart.position + 0.5f * zone.cameraControl.position + 0.25f * zone.cameraEnd.position, "Curve midpoint");
            target.position = Vector3.back * 20;
            evaluate();
            AssertPosition(zone.activeCamera.transform.position, zone.cameraStart.position, "Reverse travel");
            zone.mode = CamSwitcher.CameraMode.Moving;
            zone.pathStart = Marker(root.transform, "Path start", Vector3.zero);
            zone.pathEnd = Marker(root.transform, "Path end", Vector3.forward * 10);
            target.position = Vector3.forward * 5;
            evaluate();
            AssertPosition(zone.activeCamera.transform.position, (zone.cameraStart.position + zone.cameraEnd.position) / 2, "Legacy movement");
            Debug.Log("Cinematic camera validation passed.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static void AssertPosition(Vector3 actual, Vector3 expected, string check)
    {
        if ((actual - expected).sqrMagnitude > 0.0001f)
            throw new InvalidOperationException(check + " failed: " + actual + " expected " + expected);
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
    private static void DrawShot(CamSwitcher zone, GizmoType type)
    {
        if (zone.mode != CamSwitcher.CameraMode.Cinematic) return;
        if (zone.cameraStart != null && zone.cameraEnd != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 previous = zone.cameraStart.position;
            for (int i = 1; i <= 24; i++)
            {
                float t = i / 24f;
                Vector3 next = Vector3.Lerp(zone.cameraStart.position, zone.cameraEnd.position, t);
                if (zone.cameraControl != null)
                    next = (1-t)*(1-t)*zone.cameraStart.position + 2*(1-t)*t*zone.cameraControl.position + t*t*zone.cameraEnd.position;
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
            Gizmos.DrawWireSphere(zone.cameraStart.position, 0.25f);
            Gizmos.DrawWireSphere(zone.cameraEnd.position, 0.25f);
            Handles.Label(zone.cameraStart.position, "Camera start (far)");
            Handles.Label(zone.cameraEnd.position, "Camera end (near)");
        }
        if (zone.progressMode == CamSwitcher.ProgressMode.ApproachMarker && zone.approachMarker != null)
        {
            Handles.color = Color.yellow;
            Handles.DrawWireDisc(zone.approachMarker.position, Vector3.up, zone.farDistance);
            Handles.DrawWireDisc(zone.approachMarker.position, Vector3.up, zone.nearDistance);
            Handles.Label(zone.approachMarker.position, "Approach point");
        }
        if (zone.focusTarget != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(zone.focusTarget.position + zone.focusOffset, 0.3f);
            Handles.Label(zone.focusTarget.position + zone.focusOffset, "Reveal focus");
        }
    }
}
