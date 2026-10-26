using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Unity.Cinemachine;

public static class CameraZoneValidation
{
    [MenuItem("Tools/Cameras/Validate Camera Zone Switching")]
    public static void Validate()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode before validation.");
        var root = new GameObject("Camera zone validation") { hideFlags = HideFlags.HideAndDontSave };
        root.transform.position = new Vector3(100000, 100000, 100000);
        try
        {
            var player = Child(root, "Player").AddComponent<TankControls>();
            var rig = Child(root, "Rig").AddComponent<ExplorationCameraRig>();
            rig.enabled = false;
            rig.player = player;
            var brain = Child(root, "Output").AddComponent<Camera>().gameObject.AddComponent<CinemachineBrain>();
            var follow = Child(root, "Follow").AddComponent<CinemachineCamera>();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(ExplorationCameraRig).GetField("brain", flags).SetValue(rig, brain);
            typeof(ExplorationCameraRig).GetField("initialized", flags).SetValue(rig, true);
            typeof(ExplorationCameraRig).GetField("<FollowCamera>k__BackingField", flags).SetValue(rig, follow);
            var advance = typeof(ExplorationCameraRig).GetMethod("AdvanceExploration", flags);
            var a = Zone(root, "First zone");
            var b = Zone(root, "Second zone");
            b.transform.localPosition = Vector3.right * 10;
            Physics.SyncTransforms();
            advance.Invoke(rig, new object[] {0.02f});
            Check(rig.CurrentZone == a, "Initial zone must select its camera");
            Check(!follow.enabled, "The fallback follow camera must be disabled in a fixed zone");
            // The first zone still contains the player when the second is entered.
            b.transform.localPosition = Vector3.zero;
            b.activeCamera.enabled = false;
            Physics.SyncTransforms();
            advance.Invoke(rig, new object[] {0.02f});
            Check(rig.CurrentZone == b, "Newly entered equal-priority zone must win an overlap");
            Check(b.activeCamera.enabled && b.activeCamera.Priority.Value > a.activeCamera.Priority.Value,
                "The selected camera must be enabled and have the winning priority");
            Check(brain.DefaultBlend.Style == CinemachineBlendDefinition.Styles.Cut,
                "Fixed-camera handoff must cut immediately");
            advance.Invoke(rig, new object[] {0.02f});
            Check(rig.CurrentZone == b, "Remaining in both zones must not alternate cameras");
            a.zonePriority = b.zonePriority + 1;
            advance.Invoke(rig, new object[] {0.02f});
            Check(rig.CurrentZone == a, "Explicit higher priority must beat entry order");
            a.gameObject.SetActive(false);
            advance.Invoke(rig, new object[] {0.02f});
            Check(rig.CurrentZone == b, "Disabling the winning zone must release its camera");
            b.mode = CamSwitcher.CameraMode.Follow;
            typeof(ExplorationCameraRig).GetMethod("SelectZone", flags).Invoke(rig, new object[] {b});
            Check(follow.enabled, "An explicit follow zone must re-enable the follow camera");
            b.mode = CamSwitcher.CameraMode.Fixed;
            typeof(ExplorationCameraRig).GetMethod("SelectZone", flags).Invoke(rig, new object[] {b});
            Check(!follow.enabled && b.activeCamera.enabled,
                "Returning to fixed mode must disable follow and keep the zone camera enabled");
            Debug.Log("PASS: zone entry, overlap handoff, disabled-camera activation, instant cuts, stable selection, explicit priority, and disabled-zone exit.");
        }
        finally
        {
            var registry = (System.Collections.IList)typeof(CamSwitcher).GetField("Zones", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            foreach (var zone in root.GetComponentsInChildren<CamSwitcher>(true)) registry.Remove(zone);
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static GameObject Child(GameObject parent, string name)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    private static CamSwitcher Zone(GameObject root, string name)
    {
        var child = Child(root, name);
        child.AddComponent<BoxCollider>().size = Vector3.one * 4;
        var zone = child.AddComponent<CamSwitcher>();
        zone.activeCamera = Child(root, name + " Camera").AddComponent<CinemachineCamera>();
        // Ordinary MonoBehaviour OnEnable does not run in Edit mode. Register
        // these test-only zones explicitly, matching Play mode's lifecycle.
        var registry = (System.Collections.IList)typeof(CamSwitcher).GetField("Zones", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        if (!registry.Contains(zone)) registry.Add(zone);
        return zone;
    }

    private static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
