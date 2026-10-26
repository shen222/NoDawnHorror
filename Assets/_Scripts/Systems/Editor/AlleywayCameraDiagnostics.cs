using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Unity.Cinemachine;
using UnityEditor.SceneManagement;

public static class AlleywayCameraDiagnostics
{
    [MenuItem("Tools/Cameras/Reframe Fixed Alleyway Camera")]
    public static void Reframe()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Stop Play mode", "Stop Play mode first so the camera adjustment can be saved.", "OK");
            return;
        }
        var root = GameObject.Find("TriggerZone alleyway");
        var zone = root != null ? root.GetComponent<CamSwitcher>() : null;
        var box = root != null ? root.GetComponent<BoxCollider>() : null;
        if (zone == null || box == null || zone.activeCamera == null)
        {
            Debug.LogError("Open MainGame with the alleyway trigger and its camera assigned.");
            return;
        }
        var camera = zone.activeCamera;
        var center = box.transform.TransformPoint(box.center);
        var size = Vector3.Scale(box.size, box.transform.lossyScale);
        bool alongX = Mathf.Abs(size.x) > Mathf.Abs(size.z);
        var axis = alongX ? box.transform.right : box.transform.forward;
        axis = Vector3.ProjectOnPlane(axis, Vector3.up).normalized;
        float length = Mathf.Abs(alongX ? size.x : size.z);
        float side = Vector3.Dot(camera.transform.position - center, axis) >= 0 ? 1 : -1;
        // Retain the authored end of the alley, but center the lens across its width.
        var position = center + axis * side * length * 0.4f;
        position.y = box.bounds.min.y + 1.8f;
        var target = center;
        target.y = box.bounds.min.y + 1.2f;
        if (Physics.CheckSphere(position, 0.35f, ~0, QueryTriggerInteraction.Ignore))
        {
            Debug.LogWarning("The proposed alleyway camera position overlaps scenery. Move the Scene view to a clear shot and align the camera manually instead.", camera);
            Selection.activeGameObject = camera.gameObject;
            return;
        }
        Undo.RecordObjects(new Object[] {camera.transform, camera, zone}, "Reframe fixed alleyway camera");
        camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position));
        var lens = camera.Lens;
        lens.Dutch = 0;
        camera.Lens = lens;
        zone.mode = CamSwitcher.CameraMode.Fixed;
        EditorUtility.SetDirty(camera);
        EditorUtility.SetDirty(zone);
        EditorSceneManager.MarkSceneDirty(zone.gameObject.scene);
        Selection.activeGameObject = camera.gameObject;
        Debug.Log("Alleyway camera reframed along the trigger's centerline. Preview the shot, then save the scene. Undo restores its previous framing.", camera);
    }

    [MenuItem("Tools/Cameras/Diagnose Alleyway Camera")]
    public static void Diagnose()
    {
        var report = new StringBuilder("Alleyway camera diagnosis\n");
        report.AppendLine("Playing: " + Application.isPlaying);
        var rig = Object.FindFirstObjectByType<ExplorationCameraRig>();
        var player = rig != null ? rig.player : Object.FindFirstObjectByType<TankControls>();
        var view = Camera.main;
        var brain = view != null ? view.GetComponent<CinemachineBrain>() : null;
        report.AppendLine("Player: " + (player != null ? player.transform.position.ToString("F3") : "missing"));
        report.AppendLine("Current zone: " + (rig != null && rig.CurrentZone != null ? rig.CurrentZone.name : "none"));
        report.AppendLine("Follow outside zones: " + (rig != null && rig.followOutsideZones));
        report.AppendLine("Aiming: " + (player != null && player.IsAiming));
        report.AppendLine("Story shot: " + StoryCameraDirector.IsPlaying);
        report.AppendLine("Brain enabled: " + (brain != null && brain.enabled));
        report.AppendLine("Live camera: " + (brain != null && brain.ActiveVirtualCamera != null ? brain.ActiveVirtualCamera.Name : "none"));
        foreach (var zone in Object.FindObjectsByType<CamSwitcher>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            bool inside = player != null && zone.Contains(player.transform.position + Vector3.up * 0.25f);
            if (!inside && !zone.name.ToLowerInvariant().Contains("alley")) continue;
            var collider = zone.GetComponent<Collider>();
            report.AppendLine($"Zone {zone.name}: inside={inside}, enabled={zone.isActiveAndEnabled}, mode={zone.mode}, priority={zone.zonePriority}, cut={zone.cutOnEnter}");
            report.AppendLine("  Camera: " + (zone.activeCamera != null ? zone.activeCamera.name + ", enabled=" + zone.activeCamera.isActiveAndEnabled + ", priority=" + zone.activeCamera.Priority.Value : "UNASSIGNED"));
            if (collider != null) report.AppendLine("  Bounds: " + collider.bounds);
        }
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/AlleywayCameraDiagnosis.txt", report.ToString());
        Debug.Log(report.ToString());
        EditorUtility.DisplayDialog("Alleyway camera diagnosis", report.ToString(), "OK");
    }
}
