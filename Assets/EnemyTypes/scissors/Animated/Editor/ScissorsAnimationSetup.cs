using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEditor.SceneManagement;

// Combat setup revision 3: explicitly create missing Unity components.
public static class ScissorsAnimationSetup
{
    private const string Folder = "Assets/EnemyTypes/scissors/Animated";
    private const string ModelPath = Folder + "/ScissorsAnimated.fbx";

    [MenuItem("Tools/Scissors/Create Animated Prefab")]
    public static void Create()
    {
        var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (importer == null) throw new InvalidOperationException("ScissorsAnimated.fbx was not imported.");
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.animationCompression = ModelImporterAnimationCompression.Off;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.globalScale = 1f;
        var clips = importer.defaultClipAnimations;
        foreach (var clip in clips)
        {
            string shortName = clip.name.Split('|').Last();
            clip.name = shortName;
            clip.loopTime = shortName == "Walk" || shortName == "Idle";
            clip.loopPose = clip.loopTime;
            clip.lockRootPositionXZ = true;
            clip.lockRootHeightY = true;
            clip.lockRootRotation = true;
            clip.keepOriginalPositionXZ = true;
            clip.keepOriginalPositionY = true;
            clip.keepOriginalOrientation = true;
            // Model importer stores event time as a fraction of the clip duration.
            if (shortName == "Attack") clip.events = new[] { new AnimationEvent { time = 17f / (clip.lastFrame - clip.firstFrame), functionName = "ScissorsStrike" } };
        }
        importer.clipAnimations = clips;
        importer.SaveAndReimport();
        var animations = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
        AnimationClip FindClip(string name) => animations.FirstOrDefault(c => c.name == name || c.name.EndsWith("|" + name));
        var idle = FindClip("Idle"); var walk = FindClip("Walk"); var attack = FindClip("Attack");
        var stunned = FindClip("Stunned"); var dead = FindClip("Dead");
        if (idle == null || walk == null || attack == null || stunned == null || dead == null)
            throw new InvalidOperationException("Expected five scissors clips. Imported: " + string.Join(", ", animations.Select(c => c.name)));
        string controllerPath = Folder + "/ScissorsAnimator.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        {
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            controller.AddParameter("Pursue", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Stunned", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Alive", AnimatorControllerParameterType.Bool);
            var parameters = controller.parameters;
            parameters.First(p => p.name == "Alive").defaultBool = true;
            controller.parameters = parameters;
            var machine = controller.layers[0].stateMachine;
            foreach (var t in machine.anyStateTransitions) machine.RemoveAnyStateTransition(t);
            foreach (var state in machine.states) machine.RemoveState(state.state);
            var idleState = machine.AddState("Idle", new Vector3(200, 50)); idleState.motion = idle;
            var walkState = machine.AddState("Walk", new Vector3(450, 50)); walkState.motion = walk;
            var attackState = machine.AddState("Attack", new Vector3(320, 220)); attackState.motion = attack;
            var stunnedState = machine.AddState("Stunned", new Vector3(550, 220)); stunnedState.motion = stunned;
            var deadState = machine.AddState("Dead", new Vector3(320, 400)); deadState.motion = dead;
            machine.defaultState = idleState;
            Transition(idleState, walkState, "Pursue", AnimatorConditionMode.If, false);
            Transition(walkState, idleState, "Pursue", AnimatorConditionMode.IfNot, false);
            var toDead = machine.AddAnyStateTransition(deadState);
            toDead.hasExitTime = false; toDead.duration = 0.05f; toDead.canTransitionToSelf = false;
            toDead.AddCondition(AnimatorConditionMode.If, 0, "Dead");
            var toStunned = machine.AddAnyStateTransition(stunnedState);
            toStunned.hasExitTime = false; toStunned.duration = 0.05f; toStunned.canTransitionToSelf = true;
            toStunned.AddCondition(AnimatorConditionMode.If, 0, "Stunned");
            toStunned.AddCondition(AnimatorConditionMode.If, 0, "Alive");
            var toAttack = machine.AddAnyStateTransition(attackState);
            toAttack.hasExitTime = false; toAttack.duration = 0.08f; toAttack.canTransitionToSelf = false;
            toAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            toAttack.AddCondition(AnimatorConditionMode.If, 0, "Alive");
            Transition(attackState, walkState, "Pursue", AnimatorConditionMode.If, true);
            Transition(attackState, idleState, "Pursue", AnimatorConditionMode.IfNot, true);
            Transition(stunnedState, walkState, "Pursue", AnimatorConditionMode.If, true);
            Transition(stunnedState, idleState, "Pursue", AnimatorConditionMode.IfNot, true);
        }
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/EnemyTypes/scissors/Material.mat");
        if (material == null)
        {
            string materialPath = Folder + "/ScissorsAnimatedMaterial.mat";
            material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                var shader = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null ?
                    Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
                material = new Material(shader);
                material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Material_Base_color.png");
                AssetDatabase.CreateAsset(material, materialPath);
            }
        }
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        try
        {
            instance.name = "ScissorsAnimated";
            var animator = instance.GetComponent<Animator>();
            if (animator == null) animator = instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                renderer.sharedMaterials = Enumerable.Repeat(material, Math.Max(1, renderer.sharedMaterials.Length)).ToArray();
            if (instance.GetComponent<ScissorsAnimationPreview>() == null) instance.AddComponent<ScissorsAnimationPreview>();
            instance.GetComponent<ScissorsAnimationPreview>().previewWalkAndAttack = false;
            var capsule = instance.GetComponent<CapsuleCollider>();
            if (capsule == null) capsule = instance.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0, 1.2f, 0); capsule.height = 2.4f; capsule.radius = 0.35f; capsule.isTrigger = true;
            var agent = instance.GetComponent<NavMeshAgent>();
            if (agent == null) agent = instance.AddComponent<NavMeshAgent>();
            agent.agentTypeID = 0; agent.radius = 0.35f; agent.height = 2.4f; agent.baseOffset = 0;
            agent.speed = 1.5f; agent.angularSpeed = 120f; agent.acceleration = 8f;
            agent.stoppingDistance = 2.4f; agent.autoBraking = true;
            var ai = instance.GetComponent<ScissorsEnemyAI>();
            if (ai == null) ai = instance.AddComponent<ScissorsEnemyAI>();
            ai.animator = animator; ai.attackRange = 3.2f; ai.damage = 10; ai.attackCooldown = 2.2f;
            ai.attackDuration = 1.6f; ai.stunDuration = 1f; ai.autoFindPlayer = true;
            ai.audioSource = instance.GetComponent<AudioSource>();
            if (ai.audioSource == null) ai.audioSource = instance.AddComponent<AudioSource>();
            ai.audioSource.playOnAwake = false;
            ai.audioSource.spatialBlend = 1f;
            ai.audioSource.rolloffMode = AudioRolloffMode.Linear;
            ai.audioSource.minDistance = ai.soundMinDistance;
            ai.audioSource.maxDistance = ai.soundMaxDistance;
            ai.audioSource.dopplerLevel = 0f;
            ai.footstepClips = Array.Empty<AudioClip>();
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0) foreach (var t in instance.GetComponentsInChildren<Transform>()) t.gameObject.layer = enemyLayer;
            string prefabPath = Folder + "/ScissorsAnimated.prefab";
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            EditorGUIUtility.PingObject(Selection.activeObject);
            Debug.Log($"Scissors combat ready: {prefabPath}; five clips, agent and hit collider; {ai.damage} damage at Attack frame 17 ({17f/24f:F2}s). Auto-finds player HealthManager.");
        }
        finally { UnityEngine.Object.DestroyImmediate(instance); }
    }

    [MenuItem("Tools/Scissors/Inspect Scene Setup")]
    public static void InspectScene()
    {
        var surfaces = UnityEngine.Object.FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var agents = UnityEngine.Object.FindObjectsByType<NavMeshAgent>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var obstacles = UnityEngine.Object.FindObjectsByType<NavMeshObstacle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var links = UnityEngine.Object.FindObjectsByType<NavMeshLink>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var modifiers = UnityEngine.Object.FindObjectsByType<NavMeshModifier>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var players = UnityEngine.Object.FindObjectsByType<HealthManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        string report = "SCISSORS_SCENE_INSPECTION\n" + string.Join("\n", surfaces.Select(s => $"Surface {s.name}, agent {s.agentTypeID}, baked {s.navMeshData != null}, enabled {s.isActiveAndEnabled}")) +
            $"\nAgents {agents.Length}; obstacles {obstacles.Length}; links {links.Length}; modifiers {modifiers.Length}; NavMesh vertices {NavMesh.CalculateTriangulation().vertices.Length}\n" +
            string.Join("\n", players.Select(p => $"Player {p.name} {p.transform.position}"));
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name == "scissors" || t.name == "ScissorsAnimated"))
        {
            report += $"\nModel {t.name}, pos {t.position}, rotation {t.eulerAngles}, scale {t.lossyScale}";
            foreach (var r in t.GetComponentsInChildren<Renderer>()) report += $"\nRenderer {r.name}, bounds {r.bounds}";
        }
        Debug.Log(report);
    }

    private static void Transition(AnimatorState from, AnimatorState to, string parameter, AnimatorConditionMode mode, bool exit)
    {
        var t = from.AddTransition(to); t.hasExitTime = exit; t.exitTime = 0.95f; t.duration = 0.12f;
        t.AddCondition(mode, 0, parameter);
    }
}
