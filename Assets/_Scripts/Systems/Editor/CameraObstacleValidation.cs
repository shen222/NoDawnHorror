using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CameraObstacleValidation
{
    [MenuItem("Tools/Cameras/Validate Camera Obstacle Avoidance")]
    public static void Validate()
    {
        var root = new GameObject("Camera obstacle validation");
        root.hideFlags = HideFlags.HideAndDontSave;
        root.transform.position = new Vector3(10000, 10000, 10000);
        try
        {
            Vector3 origin = root.transform.position;
            var player = Child(root.transform, "Player", Vector3.zero);
            var camera = Child(root.transform, "Camera", Vector3.back * 6);
            var view = camera.AddComponent<Camera>();
            view.enabled = false;
            var solver = camera.AddComponent<CameraObstacleAvoidance>();
            solver.enabled = false;
            solver.player = player.transform;
            solver.obstacleLayers = 1 << 30;
            var wall = Child(root.transform, "Wall", Vector3.back * 3);
            wall.layer = 30;
            wall.AddComponent<BoxCollider>().size = new Vector3(10, 10, 1);
            var body = Child(player.transform, "Player body", Vector3.zero);
            body.layer = 30;
            body.AddComponent<SphereCollider>().radius = 0.75f;
            Physics.SyncTransforms();
            Vector3 safe = solver.ResolvePosition(origin, origin + Vector3.back * 6, 0.25f);
            Check(safe.z > origin.z - 2.3f && safe.z < origin.z - 1.5f, "Wall obstruction must pull camera to the player's side");
            Check(!Physics.CheckSphere(safe, 0.25f, 1 << 30, QueryTriggerInteraction.Ignore), "Resolved camera must not overlap the wall");
            Vector3 inside = origin + Vector3.back * 3;
            safe = solver.ResolvePosition(inside, inside, 0.25f);
            Check(!Physics.CheckSphere(safe, 0.25f, 1 << 30, QueryTriggerInteraction.Ignore), "Initial overlap must be pushed out; actual offset " + (safe - origin));
            wall.GetComponent<BoxCollider>().isTrigger = true;
            Physics.SyncTransforms();
            Vector3 desired = origin + Vector3.back * 6;
            safe = solver.ResolvePosition(origin, desired, 0.25f);
            Check((safe - desired).sqrMagnitude < 0.0001f, "Ignore trigger volumes and the player's own colliders");
            wall.GetComponent<BoxCollider>().isTrigger = false;
            solver.obstacleLayers = 0;
            Physics.SyncTransforms();
            safe = solver.ResolvePosition(origin, desired, 0.25f);
            Check((safe - desired).sqrMagnitude < 0.0001f, "Respect the obstacle layer mask");
            view.nearClipPlane = 0.5f;
            view.fieldOfView = 90;
            view.aspect = 2;
            Check(solver.GetProtectionRadius() >= Mathf.Sqrt(1.5f) - 0.001f, "Radius must cover the near-plane corners");
            // Reproduce a handoff while the old camera is recovering from a wall.
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(CameraObstacleAvoidance).GetField("previousPosition", flags).SetValue(solver, origin + Vector3.back * 20);
            typeof(CameraObstacleAvoidance).GetField("hasPreviousPosition", flags).SetValue(solver, true);
            typeof(CameraObstacleAvoidance).GetField("recovering", flags).SetValue(solver, true);
            solver.preserveAuthoredPosition = true;
            solver.transitionInProgress = true;
            camera.transform.position = origin + Vector3.right * 8;
            Vector3 authoredShot = camera.transform.position;
            typeof(CameraObstacleAvoidance).GetMethod("LateUpdate", flags).Invoke(solver, null);
            Check((camera.transform.position - authoredShot).sqrMagnitude < 0.0001f,
                "A camera handoff must discard the previous shot's obstacle recovery");
            Check(!(bool)typeof(CameraObstacleAvoidance).GetField("recovering", flags).GetValue(solver),
                "A clear authored shot must not retain recovery from the old shot");
            // A Brain handoff can finish a frame after the zone changes. Fixed
            // shots must remain independent of the previous position even then.
            solver.fixedShot = true;
            solver.transitionInProgress = false;
            typeof(CameraObstacleAvoidance).GetField("previousPosition", flags).SetValue(solver, origin + Vector3.back * 20);
            typeof(CameraObstacleAvoidance).GetField("hasPreviousPosition", flags).SetValue(solver, true);
            typeof(CameraObstacleAvoidance).GetField("recovering", flags).SetValue(solver, true);
            camera.transform.position = authoredShot;
            typeof(CameraObstacleAvoidance).GetMethod("LateUpdate", flags).Invoke(solver, null);
            Check((camera.transform.position - authoredShot).sqrMagnitude < 0.0001f,
                "A settled fixed shot must not sweep or ease back toward the previous camera");
            Debug.Log("PASS: wall obstruction, overlap recovery, player exclusion, trigger exclusion, layer mask and near-plane protection.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    public static void RunBatch()
    {
        try
        {
            Validate();
            File.WriteAllText("validation-result.txt", "PASS: wall obstruction, overlap recovery, player exclusion, trigger exclusion, layer mask and near-plane protection.");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            File.WriteAllText("validation-result.txt", exception.ToString());
            EditorApplication.Exit(1);
        }
    }

    private static GameObject Child(Transform parent, string name, Vector3 localPosition)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = localPosition;
        return child;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
