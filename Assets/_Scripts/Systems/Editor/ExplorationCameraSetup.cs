using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Cinemachine;

public static class ExplorationCameraSetup
{
    private const string Request = "Logs/ExplorationCameraSetup.request";
    [InitializeOnLoadMethod]
    private static void Schedule()
    {
        EditorApplication.delayCall += ApplyRequested;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }
    private static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode) => ApplyRequested();
    private static void ApplyRequested()
    {
        if (!File.Exists(Request)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        { EditorApplication.delayCall += ApplyRequested; return; }
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainGame") return;
        Setup();
        File.Delete(Request);
    }

    [MenuItem("Tools/Cameras/Set Up Exploration Cameras")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode first.");
        var player = UnityEngine.Object.FindFirstObjectByType<TankControls>();
        if (player == null) throw new InvalidOperationException("Open MainGame first.");
        var rig = UnityEngine.Object.FindFirstObjectByType<ExplorationCameraRig>();
        if (rig == null)
        {
            var root = new GameObject("Exploration Camera Rig");
            Undo.RegisterCreatedObjectUndo(root, "Create exploration camera rig");
            rig = Undo.AddComponent<ExplorationCameraRig>(root);
        }
        Undo.RecordObject(rig, "Configure exploration cameras");
        rig.player = player;
        EditorUtility.SetDirty(rig);
        var zones = UnityEngine.Object.FindObjectsByType<CamSwitcher>(FindObjectsSortMode.None)
            .Where(zone => zone.gameObject.scene == player.gameObject.scene && zone.activeCamera != null && zone.GetComponent<BoxCollider>() != null)
            .OrderByDescending(zone => zone.GetComponent<BoxCollider>().bounds.size.sqrMagnitude).ToArray();
        if (zones.Length > 0 && !zones.Any(zone => zone.mode == CamSwitcher.CameraMode.Moving))
        {
            var zone = zones[0];
            var bounds = zone.GetComponent<BoxCollider>().bounds;
            var cameraObject = new GameObject("Moving Exploration Camera");
            Undo.RegisterCreatedObjectUndo(cameraObject, "Create moving camera");
            cameraObject.transform.SetParent(rig.transform, false);
            cameraObject.transform.SetPositionAndRotation(zone.activeCamera.transform.position, zone.activeCamera.transform.rotation);
            var camera = cameraObject.AddComponent<CinemachineCamera>();
            camera.Lens = zone.activeCamera.Lens;
            camera.Priority = 0;
            Vector3 direction = bounds.size.x > bounds.size.z ? Vector3.right : Vector3.forward;
            float halfLength = Mathf.Max(bounds.extents.x, bounds.extents.z);
            Undo.RecordObject(zone, "Configure moving camera zone");
            zone.pathStart = Marker(rig.transform, "Player Path Start", bounds.center - direction * halfLength);
            zone.pathEnd = Marker(rig.transform, "Player Path End", bounds.center + direction * halfLength);
            zone.cameraStart = Marker(rig.transform, "Camera Start", cameraObject.transform.position - cameraObject.transform.right * 1.5f);
            zone.cameraEnd = Marker(rig.transform, "Camera End", cameraObject.transform.position + cameraObject.transform.right * 1.5f);
            zone.activeCamera = camera;
            zone.mode = CamSwitcher.CameraMode.Moving;
            EditorUtility.SetDirty(zone);
        }
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        Validate();
        string report = $"Exploration cameras ready: {zones.Length} existing zones, follow camera outside zones, moving-camera example and shoulder aiming preserved. Camera path and rotated-volume checks passed. Save MainGame to keep the setup.";
        File.WriteAllText("Logs/ExplorationCameraSetup.txt", report);
        Debug.Log(report);
    }

    private static Transform Marker(Transform parent, string name, Vector3 position)
    {
        var marker = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(marker, "Create camera path marker");
        marker.transform.SetParent(parent, false);
        marker.transform.position = position;
        return marker.transform;
    }

    [MenuItem("Tools/Cameras/Validate Exploration Cameras")]
    public static void Validate()
    {
        if (CamSwitcher.PathProgress(Vector3.forward * -2, Vector3.zero, Vector3.forward * 10) != 0 ||
            CamSwitcher.PathProgress(Vector3.forward * 12, Vector3.zero, Vector3.forward * 10) != 1 ||
            !Mathf.Approximately(CamSwitcher.PathProgress(new Vector3(8, 3, 5), Vector3.zero, Vector3.forward * 10), 0.5f) ||
            CamSwitcher.PathProgress(Vector3.one, Vector3.zero, Vector3.zero) != 0)
            throw new InvalidOperationException("Camera path projection check failed.");
        var test = new GameObject("Camera Volume Check");
        try
        {
            test.transform.SetPositionAndRotation(new Vector3(10000, 0, 10000), Quaternion.Euler(0, 45, 0));
            var box = test.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(2, 4, 6);
            var zone = test.AddComponent<CamSwitcher>();
            zone.mode = CamSwitcher.CameraMode.Follow;
            Physics.SyncTransforms();
            if (!zone.Contains(test.transform.position) || zone.Contains(test.transform.position + Vector3.right * 10))
                throw new InvalidOperationException("Rotated camera volume check failed.");
        }
        finally { UnityEngine.Object.DestroyImmediate(test); }
    }
}
