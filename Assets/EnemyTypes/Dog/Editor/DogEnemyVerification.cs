using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public static class DogEnemyVerification
{
    [MenuItem("Tools/Enemies/Dog/Validate Rig And Preview Animations")]
    public static void ValidateAndPreview()
    {
        ValidatePoses(false);
    }

    [MenuItem("Tools/Enemies/Dog/Validate And Preview Jump Attack")]
    public static void ValidateJumpAttack()
    {
        ValidatePoses(true);
    }

    private static void ValidatePoses(bool jump)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DogEnemySetup.PrefabPath);
        Check(prefab != null, "Dog enemy prefab exists");
        var skin = prefab.GetComponent<SkinnedMeshRenderer>();
        Check(skin != null && skin.bones.Length == 21, "21-bone skinned rig exists");
        Check(skin.sharedMesh.bindposes.Length == skin.bones.Length, "Bind poses match skeleton");
        Check(skin.sharedMesh.boneWeights.Length == skin.sharedMesh.vertexCount, "Every vertex has skin weights");
        foreach (var weight in skin.sharedMesh.boneWeights)
            Check(Mathf.Abs(weight.weight0 + weight.weight1 + weight.weight2 + weight.weight3 - 1) < .001f, "Skin weights are normalized");
        Check(prefab.GetComponent<DogEnemyAI>() is ZombieAI, "Existing bullets can damage the dog");
        Check(prefab.GetComponent<NavMeshAgent>().radius > 0, "Navigation agent exists");
        Check(Mathf.Abs(prefab.GetComponent<NavMeshAgent>().baseOffset) < .001f, "Dog feet use the NavMesh ground height");
        var clips = prefab.GetComponent<Animator>().runtimeAnimatorController.animationClips.Distinct().ToArray();
        Check(clips.Length == 6, "All six animation clips are assigned");
        var dog = UnityEngine.Object.Instantiate(prefab);
        var cameraRoot = new GameObject("Dog rig preview camera");
        var lightRoot = new GameObject("Dog rig preview light");
        var render = new RenderTexture(600, 440, 24);
        var sheet = new Texture2D(1800, 880, TextureFormat.RGB24, false);
        var baked = new Mesh();
        var previous = RenderTexture.active;
        try
        {
            dog.GetComponent<DogEnemyAI>().enabled = false;
            dog.GetComponent<NavMeshAgent>().enabled = false;
            dog.GetComponent<Animator>().enabled = false;
            dog.transform.position = new Vector3(10000, 10000, 10000);
            foreach (var node in dog.GetComponentsInChildren<Transform>()) node.gameObject.layer = 31;
            var camera = cameraRoot.AddComponent<Camera>();
            camera.cullingMask = 1 << 31;
            camera.transform.position = dog.transform.position + new Vector3(3.5f, 1.35f, 1.5f);
            camera.transform.LookAt(dog.transform.position + Vector3.up * (jump ? .85f : .55f));
            camera.orthographic = true;
            camera.orthographicSize = 1.25f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.2f, .22f, .24f);
            camera.targetTexture = render;
            var light = lightRoot.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 2.8f;
            light.cullingMask = 1 << 31;
            light.transform.rotation = Quaternion.Euler(40, -60, 0);
            string[] names = jump ? Enumerable.Repeat("Bite", 6).ToArray() : new[] {"Idle", "Walk", "Run", "Bite", "Hurt", "Death"};
            float[] times = jump ? new[] {.12f, .29f, .45f, .65f, .84f, 1.15f} : new[] {0, .25f, .16f, .65f, .2f, 1.1f};
            // Render the freshly baked pose so multiple samples in one editor frame
            // cannot reuse Unity's cached GPU skinning matrices from the first pose.
            var snapshot = new GameObject("Baked pose preview", typeof(MeshFilter), typeof(MeshRenderer));
            snapshot.transform.SetParent(dog.transform, false);
            snapshot.layer = 31;
            snapshot.GetComponent<MeshFilter>().sharedMesh = baked;
            snapshot.GetComponent<MeshRenderer>().sharedMaterials = dog.GetComponent<SkinnedMeshRenderer>().sharedMaterials;
            dog.GetComponent<SkinnedMeshRenderer>().enabled = false;
            Vector3[] rest = null;
            float largestDeformation = 0;
            for (int i = 0; i < names.Length; i++)
            {
                clips.First(c => c.name.Replace(" ", "") == "DogIdle").SampleAnimation(dog, 0);
                clips.First(c => c.name.Replace(" ", "") == "Dog" + names[i]).SampleAnimation(dog, times[i]);
                dog.GetComponent<SkinnedMeshRenderer>().BakeMesh(baked);
                var vertices = baked.vertices;
                if (jump && i == 2) Check(vertices.Min(v => v.y) > .2f, "All four paws leave the ground during the pounce");
                if (jump && i == 5) Check(Mathf.Abs(vertices.Min(v => v.y)) < .04f, "Paws return to the ground after landing");
                if (i == 0) rest = vertices;
                else for (int j = 0; j < vertices.Length; j++) largestDeformation = Mathf.Max(largestDeformation, Vector3.Distance(rest[j], vertices[j]));
                Check(vertices.All(v => !float.IsNaN(v.x) && !float.IsInfinity(v.x)), "Skin deformation is finite");
                camera.Render();
                RenderTexture.active = render;
                sheet.ReadPixels(new Rect(0, 0, 600, 440), i % 3 * 600, (1 - i / 3) * 440);
            }
            Check(largestDeformation > .15f, "Skeleton actually deforms the dog mesh");
            sheet.Apply();
            System.IO.Directory.CreateDirectory("Logs/DogEnemy");
            System.IO.File.WriteAllBytes(jump ? "Logs/DogEnemy/jump-attack-poses.png" : "Logs/DogEnemy/animation-poses.png", sheet.EncodeToPNG());
            Debug.Log($"PASS dog rig: {skin.sharedMesh.vertexCount} weighted vertices; 21 bones; six clips; bullet compatibility; deformation {largestDeformation:F2}m. Preview order: Idle, Walk, Run / Bite, Hurt, Death.");
        }
        finally
        {
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(dog);
            UnityEngine.Object.DestroyImmediate(cameraRoot);
            UnityEngine.Object.DestroyImmediate(lightRoot);
            UnityEngine.Object.DestroyImmediate(render);
            UnityEngine.Object.DestroyImmediate(sheet);
            UnityEngine.Object.DestroyImmediate(baked);
        }
    }

    private static void Check(bool result, string message)
    {
        if (!result) throw new InvalidOperationException("Dog verification failed: " + message);
    }
}
