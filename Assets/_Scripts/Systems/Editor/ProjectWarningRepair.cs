using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ProjectWarningRepair
{
    private const string Request = "Logs/ProjectWarningRepair.request";

    [InitializeOnLoadMethod]
    private static void ScheduleRequestedRepair()
    {
        EditorApplication.delayCall += RunRequestedRepair;
    }

    private static void RunRequestedRepair()
    {
        if (!File.Exists(Request)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += RunRequestedRepair;
            return;
        }
        Repair();
        File.Delete(Request);
    }

    [MenuItem("Tools/Project/Fix NPC and Terrain Tree Warnings")]
    public static void Repair()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        int animations = 0, meshes = 0, treeLods = 0;
        foreach (var legacy in Object.FindObjectsByType<Animation>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (EditorUtility.IsPersistent(legacy) || !legacy.gameObject.scene.IsValid() ||
                legacy.clip == null || legacy.clip.legacy) continue;
            var animator = legacy.GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null) continue;
            var idle = legacy.GetComponent<NpcIdleAnimation>();
            if (idle == null) idle = Undo.AddComponent<NpcIdleAnimation>(legacy.gameObject);
            Undo.RecordObject(idle, "Migrate NPC idle animation");
            idle.clip = legacy.clip;
            EditorUtility.SetDirty(idle);
            PrefabUtility.RecordPrefabInstancePropertyModifications(idle);
            var scene = legacy.gameObject.scene;
            Undo.DestroyObjectImmediate(legacy);
            EditorSceneManager.MarkSceneDirty(scene);
            animations++;
        }

        var treePaths = new HashSet<string>();
        foreach (var terrain in Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (terrain.terrainData == null) continue;
            foreach (var prototype in terrain.terrainData.treePrototypes)
            {
                string path = AssetDatabase.GetAssetPath(prototype.prefab);
                if (path.EndsWith(".prefab")) treePaths.Add(path);
            }
        }
        foreach (string path in treePaths)
        {
            // Do not overwrite a prefab the user is currently editing.
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.assetPath == path)
            {
                Debug.LogWarning("Finish editing " + path + ", then run Tools/Project/Fix NPC and Terrain Tree Warnings again.");
                continue;
            }
            var root = PrefabUtility.LoadPrefabContents(path);
            bool changed = false;
            try
            {
                foreach (var mesh in root.GetComponentsInChildren<MeshCollider>(true))
                {
                    bool supportedCollider = false;
                    foreach (var collider in mesh.GetComponents<Collider>())
                        if (!(collider is MeshCollider) && collider.enabled && !collider.isTrigger) supportedCollider = true;
                    if (!supportedCollider && mesh.sharedMesh != null)
                    {
                        var bounds = mesh.sharedMesh.bounds;
                        var capsule = mesh.gameObject.AddComponent<CapsuleCollider>();
                        capsule.center = bounds.center;
                        capsule.direction = 1;
                        capsule.radius = Mathf.Max(0.05f, Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.25f);
                        capsule.height = Mathf.Max(capsule.radius * 2, bounds.size.y);
                        capsule.sharedMaterial = mesh.sharedMaterial;
                        capsule.isTrigger = mesh.isTrigger;
                        capsule.enabled = mesh.enabled;
                    }
                    Object.DestroyImmediate(mesh);
                    meshes++;
                    changed = true;
                }
                // Undo only the single full-detail LOD added by the previous repair.
                // It bypassed terrain billboards and kept expensive meshes visible too far away.
                var previousLod = root.GetComponent<LODGroup>();
                if (previousLod != null)
                {
                    var levels = previousLod.GetLODs();
                    if (levels.Length == 1 && Mathf.Approximately(levels[0].screenRelativeTransitionHeight, 0.001f))
                    {
                        Object.DestroyImmediate(previousLod);
                        treeLods++;
                        changed = true;
                    }
                }
                if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        foreach (var terrain in Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (terrain.terrainData != null) terrain.terrainData.RefreshPrototypes();
        string report = $"Warning repair: migrated {animations} NPC animation(s), replaced {meshes} unsupported tree mesh collider(s), removed {treeLods} full-detail tree LOD group(s) from the previous repair to restore terrain billboarding. Save modified scenes to keep their repairs.";
        File.WriteAllText("Logs/ProjectWarningRepair.txt", report);
        Debug.Log(report);
    }
}
