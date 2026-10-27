using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

public static class DogEnemySetup
{
    public const string Folder = "Assets/EnemyTypes/Dog";
    public const string PrefabPath = Folder + "/DogEnemy.prefab";
    private const string ModelPath = "Assets/SketchFabs/Dog/Dog.fbx";

    private sealed class Joint
    {
        public Transform bone;
        public Vector3 end;
        public Joint(Transform bone, Vector3 end) { this.bone = bone; this.end = end; }
    }

    [MenuItem("Tools/Enemies/Dog/Create Rigged Enemy And Replace Scene Dog")]
    public static void Create()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode before creating the dog enemy.");
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (source == null) throw new InvalidOperationException("Dog.fbx is missing.");
        System.IO.Directory.CreateDirectory(Folder + "/Animations");
        AssetDatabase.Refresh();
        // Readability is needed only for authoring skin weights, not for gameplay.
        var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
        bool wasReadable = importer.isReadable;
        if (!wasReadable) { importer.isReadable = true; importer.SaveAndReimport(); }
        GameObject root = null;
        try
        {
            source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var sourceFilter = source.GetComponentInChildren<MeshFilter>();
            if (sourceFilter == null) throw new InvalidOperationException("Dog mesh was not found.");
            var mesh = UnityEngine.Object.Instantiate(sourceFilter.sharedMesh);
            mesh.name = "Dog weighted quadruped mesh";
            var vertices = mesh.vertices;
            Matrix4x4 original = sourceFilter.transform.localToWorldMatrix;
            var bounds = new Bounds(original.MultiplyPoint3x4(vertices[0]), Vector3.zero);
            for (int i = 1; i < vertices.Length; i++) bounds.Encapsulate(original.MultiplyPoint3x4(vertices[i]));
            float scale = 1.15f / bounds.size.y;
            Vector3 offset = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            for (int i = 0; i < vertices.Length; i++) vertices[i] = (original.MultiplyPoint3x4(vertices[i]) - offset) * scale;
            mesh.vertices = vertices;
            var normals = mesh.normals;
            for (int i = 0; i < normals.Length; i++) normals[i] = original.inverse.transpose.MultiplyVector(normals[i]).normalized;
            mesh.normals = normals;
            var tangents = mesh.tangents;
            for (int i = 0; i < tangents.Length; i++)
            {
                Vector3 tangent = original.MultiplyVector(tangents[i]).normalized;
                tangents[i] = new Vector4(tangent.x, tangent.y, tangent.z, tangents[i].w);
            }
            mesh.tangents = tangents;

            root = new GameObject("Dog Enemy");
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0) root.layer = enemyLayer;
            var rig = Child(root.transform, "Rig", Vector3.zero);
            var joints = new List<Joint>();
            Transform Bone(string name, Transform parent, Vector3 position, Vector3 end)
            {
                var bone = Child(parent, name, Vector3.zero);
                bone.position = position;
                joints.Add(new Joint(bone, end));
                return bone;
            }
            var hips = Bone("Hips", rig, new Vector3(0, .86f, -.43f), new Vector3(0, .89f, -.08f));
            var spine = Bone("Spine", hips, new Vector3(0, .89f, -.08f), new Vector3(0, .81f, .35f));
            var chest = Bone("Chest", spine, new Vector3(0, .81f, .35f), new Vector3(0, .84f, .55f));
            var neck = Bone("Neck", chest, new Vector3(0, .84f, .55f), new Vector3(0, .79f, .75f));
            var head = Bone("Head", neck, new Vector3(0, .79f, .75f), new Vector3(0, .67f, .94f));
            Bone("Jaw", head, new Vector3(0, .67f, .81f), new Vector3(0, .59f, .94f));
            var tail = Bone("Tail", hips, new Vector3(0, .81f, -.63f), new Vector3(0, .48f, -.81f));
            var tailMid = Bone("TailMid", tail, new Vector3(0, .48f, -.81f), new Vector3(0, .31f, -1.02f));
            Bone("TailTip", tailMid, new Vector3(0, .31f, -1.02f), new Vector3(0, .33f, -1.16f));
            foreach (int side in new[] {-1, 1})
            {
                string suffix = side < 0 ? "L" : "R";
                float x = .21f * side;
                var upper = Bone("FrontUpper" + suffix, chest, new Vector3(x, .66f, .36f), new Vector3(x, .36f, .35f));
                var lower = Bone("FrontLower" + suffix, upper, new Vector3(x, .36f, .35f), new Vector3(x, .10f, .39f));
                Bone("FrontPaw" + suffix, lower, new Vector3(x, .10f, .39f), new Vector3(x, .04f, .54f));
                upper = Bone("HindUpper" + suffix, hips, new Vector3(x, .70f, -.48f), new Vector3(x, .41f, -.37f));
                lower = Bone("HindLower" + suffix, upper, new Vector3(x, .41f, -.37f), new Vector3(x, .13f, -.56f));
                Bone("HindPaw" + suffix, lower, new Vector3(x, .13f, -.56f), new Vector3(x, .04f, -.42f));
            }
            var weights = new BoneWeight[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 point = vertices[i];
                var scores = new List<KeyValuePair<int, float>>();
                for (int j = 0; j < joints.Count; j++)
                {
                    var joint = joints[j];
                    string name = joint.bone.name;
                    bool limb = name.StartsWith("Front") || name.StartsWith("Hind");
                    // Prevent cross-body skinning and keep the wrapped head away from the forelegs.
                    if (limb && ((point.x < -.025f && name.EndsWith("R")) || (point.x > .025f && name.EndsWith("L")))) continue;
                    if (limb && point.y > .80f) continue;
                    if (name.StartsWith("Tail") && (point.z > -.60f || point.y > .86f)) continue;
                    if ((name == "Head" || name == "Jaw" || name == "Neck") && point.z < .35f) continue;
                    if (name == "Jaw" && point.y > .7f) continue;
                    float distance = SegmentDistance(point, joint.bone.position, joint.end);
                    float score = 1f / Mathf.Pow(Mathf.Max(.025f, distance), 4f);
                    scores.Add(new KeyValuePair<int, float>(j, score));
                }
                var closest = scores.OrderByDescending(p => p.Value).Take(4).ToArray();
                if (closest.Length < 4) Array.Resize(ref closest, 4);
                float total = closest.Sum(p => p.Value);
                weights[i] = new BoneWeight
                {
                    boneIndex0 = closest[0].Key, weight0 = closest[0].Value / total,
                    boneIndex1 = closest[1].Key, weight1 = closest[1].Value / total,
                    boneIndex2 = closest[2].Key, weight2 = closest[2].Value / total,
                    boneIndex3 = closest[3].Key, weight3 = closest[3].Value / total
                };
            }
            mesh.boneWeights = weights;
            mesh.bindposes = joints.Select(j => j.bone.worldToLocalMatrix * root.transform.localToWorldMatrix).ToArray();
            mesh.RecalculateBounds();
            mesh = SaveAsset(mesh, Folder + "/DogSkinnedMesh.asset");
            var skin = root.AddComponent<SkinnedMeshRenderer>();
            skin.sharedMesh = mesh;
            skin.sharedMaterials = sourceFilter.GetComponent<Renderer>().sharedMaterials;
            skin.bones = joints.Select(j => j.bone).ToArray();
            skin.rootBone = hips;
            skin.localBounds = new Bounds(new Vector3(0, .55f, 0), new Vector3(2.2f, 2.8f, 3.3f));
            skin.updateWhenOffscreen = true;
            skin.quality = SkinQuality.Bone4;
            var animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = CreateAnimations(root, joints);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.radius = .32f;
            capsule.height = 1.8f;
            capsule.center = new Vector3(0, .62f, 0);
            capsule.direction = 2; // Long horizontal body, not a human-shaped upright capsule.
            capsule.isTrigger = true;
            var agent = root.AddComponent<NavMeshAgent>();
            agent.agentTypeID = 0;
            agent.radius = .35f;
            agent.height = 1.15f;
            agent.baseOffset = 0f;
            agent.speed = 1.5f;
            agent.angularSpeed = 300;
            agent.acceleration = 10;
            agent.stoppingDistance = .85f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            var enemy = root.AddComponent<DogEnemyAI>();
            enemy.animator = animator;
            enemy.maxHealth = 40;
            enemy.damage = 15;
            enemy.attackRange = 2f;
            enemy.attackCooldown = 1.6f;
            enemy.visionDistance = 12;
            enemy.visionConeAngle = 130;
            enemy.hearingRadius = 5;
            enemy.walkSpeed = 1.5f;
            enemy.turnSpeed = 300;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            ReplaceSceneDog(prefab, source);
            AssetDatabase.SaveAssets();
            Debug.Log("Dog enemy created: 21 weighted bones, six animation clips, 40 HP, 15-damage bite, NavMesh roaming and pursuit.");
        }
        finally
        {
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            if (!wasReadable) { importer.isReadable = false; importer.SaveAndReimport(); }
        }
    }

    private static Transform Child(Transform parent, string name, Vector3 position)
    {
        var child = new GameObject(name).transform;
        child.SetParent(parent, false);
        child.localPosition = position;
        return child;
    }

    private static float SegmentDistance(Vector3 point, Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        float t = direction.sqrMagnitude > .00001f ? Mathf.Clamp01(Vector3.Dot(point - start, direction) / direction.sqrMagnitude) : 0;
        return Vector3.Distance(point, start + direction * t);
    }

    private static T SaveAsset<T>(T value, string path) where T : UnityEngine.Object
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing == null) { AssetDatabase.CreateAsset(value, path); return value; }
        EditorUtility.CopySerialized(value, existing);
        UnityEngine.Object.DestroyImmediate(value);
        EditorUtility.SetDirty(existing);
        return existing;
    }

    private static RuntimeAnimatorController CreateAnimations(GameObject root, List<Joint> joints)
    {
        var clips = new Dictionary<string, AnimationClip>();
        foreach (string name in new[] {"Idle", "Walk", "Run", "Bite", "Hurt", "Death"})
        {
            clips.Add(name, CreateClip(name, root, joints));
        }
        return CreateController(clips);
    }

    private static AnimationClip CreateClip(string name, GameObject root, List<Joint> joints)
    {
            float duration = name == "Idle" ? 2f : name == "Walk" ? 1f : name == "Run" ? .65f : name == "Bite" ? DogEnemyAI.JumpClipDuration : name == "Hurt" ? .45f : 1.1f;
            var clip = new AnimationClip { name = "Dog" + name, frameRate = 30 };
            bool loop = name == "Idle" || name == "Walk" || name == "Run";
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            foreach (var joint in joints)
            {
                string path = AnimationUtility.CalculateTransformPath(joint.bone, root.transform);
                var rotationCurves = new[] { new AnimationCurve(), new AnimationCurve(), new AnimationCurve(), new AnimationCurve() };
                var positionCurves = new[] { new AnimationCurve(), new AnimationCurve(), new AnimationCurve() };
                int frames = Mathf.CeilToInt(duration * 30);
                Quaternion previous = Quaternion.identity;
                for (int frame = 0; frame <= frames; frame++)
                {
                    float time = duration * frame / frames;
                    Pose(name, joint.bone.name, time / duration, out Vector3 euler, out Vector3 movement);
                    Quaternion rotation = Quaternion.Euler(euler);
                    if (Quaternion.Dot(previous, rotation) < 0) rotation = new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w);
                    previous = rotation;
                    for (int axis = 0; axis < 4; axis++) rotationCurves[axis].AddKey(time, rotation[axis]);
                    for (int axis = 0; axis < 3; axis++) positionCurves[axis].AddKey(time, joint.bone.localPosition[axis] + movement[axis]);
                }
                for (int axis = 0; axis < 4; axis++) clip.SetCurve(path, typeof(Transform), "m_LocalRotation." + "xyzw"[axis], rotationCurves[axis]);
                for (int axis = 0; axis < 3; axis++) clip.SetCurve(path, typeof(Transform), "m_LocalPosition." + "xyz"[axis], positionCurves[axis]);
            }
            clip.EnsureQuaternionContinuity();
            return SaveAsset(clip, Folder + "/Animations/Dog" + name + ".anim");
    }

    private static RuntimeAnimatorController CreateController(Dictionary<string, AnimationClip> clips)
    {
        string controllerPath = Folder + "/DogAnimator.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var machine = controller.layers[0].stateMachine;
        foreach (var t in machine.anyStateTransitions) machine.RemoveAnyStateTransition(t);
        foreach (var state in machine.states) machine.RemoveState(state.state);
        // Remove old embedded blend trees on a deliberate rebuild.
        foreach (var tree in AssetDatabase.LoadAllAssetsAtPath(controllerPath).OfType<BlendTree>()) UnityEngine.Object.DestroyImmediate(tree, true);
        controller.parameters = Array.Empty<AnimatorControllerParameter>();
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);
        controller.AddParameter(new AnimatorControllerParameter { name = "AttackSpeed", type = AnimatorControllerParameterType.Float, defaultFloat = 1 });
        var locomotion = machine.AddState("Locomotion");
        var blend = new BlendTree { name = "Idle Walk Run", blendParameter = "Speed", useAutomaticThresholds = false };
        AssetDatabase.AddObjectToAsset(blend, controller);
        blend.AddChild(clips["Idle"], 0);
        blend.AddChild(clips["Walk"], 1.5f);
        blend.AddChild(clips["Run"], 3.8f);
        locomotion.motion = blend;
        machine.defaultState = locomotion;
        foreach (string name in new[] {"Bite", "Hurt", "Death"})
        {
            var state = machine.AddState(name);
            state.motion = clips[name];
            if (name == "Bite") { state.speedParameter = "AttackSpeed"; state.speedParameterActive = true; }
            var enter = machine.AddAnyStateTransition(state);
            enter.hasExitTime = false;
            enter.duration = name == "Death" ? .1f : .06f;
            enter.canTransitionToSelf = false;
            enter.AddCondition(AnimatorConditionMode.If, 0, name == "Bite" ? "Attack" : name == "Death" ? "Dead" : "Hurt");
            if (name != "Death")
            {
                enter.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");
                var exit = state.AddTransition(locomotion);
                exit.hasExitTime = true;
                exit.exitTime = 1;
                exit.duration = .08f;
            }
        }
        EditorUtility.SetDirty(controller);
        return controller;
    }

    [MenuItem("Tools/Enemies/Dog/Upgrade Jump Attack")]
    public static void UpgradeJumpAttack()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode before upgrading the attack.");
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var bones = root.GetComponent<SkinnedMeshRenderer>().bones.Select(b => new Joint(b, b.position)).ToList();
            var clip = CreateClip("Bite", root, bones);
            var controller = (AnimatorController)root.GetComponent<Animator>().runtimeAnimatorController;
            if (!controller.parameters.Any(p => p.name == "AttackSpeed"))
                controller.AddParameter(new AnimatorControllerParameter { name = "AttackSpeed", type = AnimatorControllerParameterType.Float, defaultFloat = 1 });
            var state = controller.layers[0].stateMachine.states.First(s => s.state.name == "Bite").state;
            state.motion = clip;
            state.speedParameter = "AttackSpeed";
            state.speedParameterActive = true;
            var enemy = root.GetComponent<DogEnemyAI>();
            enemy.biteWindup = .65f;
            enemy.biteDuration = DogEnemyAI.JumpClipDuration;
            enemy.attackRange = 2f;
            enemy.jumpDistance = 1.1f;
            enemy.jumpHitRange = 1.25f;
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        Debug.Log("Dog jump attack upgraded: crouch, push-off, airborne reach, impact, landing and recovery.");
    }

    private static void Pose(string clip, string bone, float t, out Vector3 euler, out Vector3 offset)
    {
        euler = offset = Vector3.zero;
        float wave = Mathf.Sin(t * Mathf.PI * 2);
        bool front = bone.StartsWith("Front"), hind = bone.StartsWith("Hind");
        if (clip == "Idle")
        {
            if (bone == "Spine") euler.x = wave * 1.2f;
            if (bone == "Hips") offset.y = wave * .006f;
            if (bone == "Head") euler.y = wave * 2f;
        }
        if (clip == "Walk" || clip == "Run")
        {
            bool running = clip == "Run";
            if (front || hind)
            {
                // Walk uses diagonal pairs; running compresses and extends in a bound.
                float phase = running ? (front ? 0 : .5f) : ((front == bone.EndsWith("L")) ? 0 : .5f);
                float cycle = Mathf.Sin((t + phase) * Mathf.PI * 2);
                if (bone.Contains("Upper")) euler.x = cycle * (running ? 32 : 19);
                if (bone.Contains("Lower")) euler.x = Mathf.Max(0, -cycle) * (front ? 32 : -38);
                if (bone.Contains("Paw")) euler.x = -cycle * 12;
            }
            if (bone == "Hips") { offset.y = (1 - Mathf.Cos(t * Mathf.PI * 4)) * (running ? .025f : .009f); euler.x = wave * (running ? 5 : 1); }
            if (bone == "Spine") euler.x = wave * (running ? -7 : -2);
            if (bone == "Head") euler.x = wave * (running ? 4 : 2);
        }
        if (bone.StartsWith("Tail") && clip != "Death") euler.y = wave * (clip == "Idle" ? 5 : 9);
        if (clip == "Bite")
        {
            // Anticipate, push off, tuck, reach, absorb impact, then recover.
            // Horizontal travel belongs to the collision-checked NavMesh lunge.
            float crouch = Mathf.Sin(Mathf.Clamp01(t / .22f) * Mathf.PI);
            float flight = Mathf.InverseLerp(.22f, .72f, t);
            float airborne = Mathf.Sin(flight * Mathf.PI);
            float launch = Mathf.Sin(Mathf.Clamp01((t - .16f) / .22f) * Mathf.PI);
            float landing = Mathf.Sin(Mathf.Clamp01((t - .68f) / .22f) * Mathf.PI);
            float tuck = Mathf.Sin(Mathf.Clamp01((t - .27f) / .30f) * Mathf.PI);
            float reach = Mathf.Sin(Mathf.Clamp01((t - .47f) / .27f) * Mathf.PI);
            if (bone == "Hips") { offset.y = DogEnemyAI.JumpHeightAt(t) - crouch * .09f - landing * .09f; euler.x = -launch * 9 + reach * 7; }
            if (bone == "Spine") euler.x = crouch * 8 - airborne * 6 + landing * 8;
            if (bone == "Chest") euler.x = -launch * 10 + reach * 8;
            if (bone == "Neck") euler.x = -airborne * 8 - reach * 10;
            if (bone == "Head") euler.x = -crouch * 5 + reach * 12;
            if (bone == "Jaw") euler.x = airborne * 18;
            if (bone.StartsWith("FrontUpper")) euler.x = crouch * 20 - launch * 36 + tuck * 20 - reach * 45 + landing * 24;
            if (bone.StartsWith("FrontLower")) euler.x = -crouch * 40 - tuck * 48 + reach * 20 - landing * 44;
            if (bone.StartsWith("FrontPaw")) euler.x = crouch * 20 + tuck * 24 + reach * 12 + landing * 20;
            if (bone.StartsWith("HindUpper")) euler.x = -crouch * 25 + launch * 30 - tuck * 32 - landing * 25;
            if (bone.StartsWith("HindLower")) euler.x = crouch * 50 - launch * 18 + tuck * 65 + landing * 50;
            if (bone.StartsWith("HindPaw")) euler.x = -crouch * 25 - tuck * 30 - landing * 25;
            if (bone.StartsWith("Tail")) euler.x = -launch * 16 + reach * 12;
        }
        if (clip == "Hurt")
        {
            float recoil = Mathf.Sin(t * Mathf.PI);
            if (bone == "Hips") offset.z = -recoil * .09f;
            if (bone == "Spine") euler.z = recoil * 8;
            if (bone == "Head") euler.y = -recoil * 15;
        }
        if (clip == "Death")
        {
            float fall = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .7f));
            if (bone == "Hips") { offset.y = -fall * .63f; euler.z = fall * 82; }
            if (front || hind) euler.x = (front ? -28 : 32) * fall;
            if (bone == "Head") euler.y = -18 * fall;
        }
    }

    private static void ReplaceSceneDog(GameObject prefab, GameObject model)
    {
        var scene = EditorSceneManager.GetActiveScene();
        var existing = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(t => t.gameObject.scene == scene && PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject) &&
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) == ModelPath).ToArray();
        int count = 0;
        foreach (var old in existing)
        {
            var replacement = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Undo.RegisterCreatedObjectUndo(replacement, "Make dog an enemy");
            replacement.transform.SetParent(old.parent, true);
            Vector3 position = old.position;
            Vector3 forward = old.rotation * Quaternion.Inverse(model.transform.rotation) * Vector3.forward;
            forward.y = 0;
            if (forward.sqrMagnitude > .001f) replacement.transform.rotation = Quaternion.LookRotation(forward);
            if (NavMesh.SamplePosition(position, out var hit, 3f, NavMesh.AllAreas)) position = hit.position;
            replacement.transform.position = position;
            replacement.GetComponent<DogEnemyAI>().player = UnityEngine.Object.FindFirstObjectByType<TankControls>()?.transform;
            Undo.DestroyObjectImmediate(old.gameObject);
            count++;
        }
        if (count > 0) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
        Debug.Log($"Replaced {count} static scene dog(s). Prefab: {PrefabPath}");
    }
}
