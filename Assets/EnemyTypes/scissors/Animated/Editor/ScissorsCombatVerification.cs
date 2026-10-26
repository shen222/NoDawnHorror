using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public sealed class ScissorsCombatVerification
{
    private static bool requested;
    private readonly List<string> results = new List<string>();
    private GameObject playerObject, enemyObject, wall;
    private NavMeshDataInstance meshInstance;
    private NavMeshData meshData;
    private ScissorsEnemyAI enemy;
    private HealthManager health;

    [MenuItem("Tools/Scissors/Run Combat Checks")]
    public static void Run()
    {
        requested = true;
        if (EditorApplication.isPlaying) Spawn();
        else
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
            EditorApplication.EnterPlaymode();
        }
    }
    private static void OnPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !requested) return;
        EditorApplication.playModeStateChanged -= OnPlayMode;
        Spawn();
    }
    private static void Spawn()
    {
        requested = false;
        UnityEngine.Object.FindFirstObjectByType<HealthManager>().StartCoroutine(new ScissorsCombatVerification().CheckCombat());
    }
    private void Check(bool passed, string description)
    {
        results.Add((passed ? "PASS " : "FAIL ") + description);
        if (!passed) Debug.LogError("SCISSORS_CHECK_FAIL " + description);
    }
    private void BeginAttack()
    {
        var method = typeof(ScissorsEnemyAI).GetMethod("PerformScissorsAttack", BindingFlags.Instance | BindingFlags.NonPublic);
        enemy.StartCoroutine((IEnumerator)method.Invoke(enemy, null));
    }
    private IEnumerator CheckCombat()
    {
        Time.timeScale = 1f;
        yield return null;
        foreach (var sceneEnemy in UnityEngine.Object.FindObjectsByType<ScissorsEnemyAI>(FindObjectsSortMode.None))
        {
            var sceneAgent = sceneEnemy.GetComponent<NavMeshAgent>();
            Check(sceneAgent.isOnNavMesh && sceneEnemy.player != null, "scene scissors has a player target and is on the existing NavMesh");
        }
        var origin = new Vector3(10000, 0, 10000);
        var source = new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box,
            size = new Vector3(20, 0.1f, 20), transform = Matrix4x4.TRS(origin + Vector3.down * 0.05f, Quaternion.identity, Vector3.one), area = 0 };
        meshData = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(0), new List<NavMeshBuildSource> { source },
            new Bounds(origin, new Vector3(24, 5, 24)), Vector3.zero, Quaternion.identity);
        meshInstance = NavMesh.AddNavMeshData(meshData);
        playerObject = new GameObject("Scissors test target");
        playerObject.transform.position = origin + Vector3.forward * 2.7f;
        health = playerObject.AddComponent<HealthManager>();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EnemyTypes/scissors/Animated/ScissorsAnimated.prefab");
        enemyObject = UnityEngine.Object.Instantiate(prefab, origin, Quaternion.identity);
        enemy = enemyObject.GetComponent<ScissorsEnemyAI>();
        enemy.autoFindPlayer = false; enemy.player = playerObject.transform;
        enemy.visionDistance = 0; enemy.hearingRadius = 0;
        yield return null;
        health.ResetHealth();
        var agent = enemy.GetComponent<NavMeshAgent>();
        var path = new NavMeshPath();
        Check(agent.isOnNavMesh && agent.CalculatePath(origin + Vector3.forward * 5, path) && path.status == NavMeshPathStatus.PathComplete,
            "agent has a complete path on a baked test surface");

        BeginAttack();
        yield return new WaitForSeconds(0.5f);
        Check(health.CurrentHealth == 100, "no damage before blade snap");
        yield return new WaitForSeconds(0.45f);
        Check(health.CurrentHealth == 90, "animation event deals 10 damage at blade snap");
        enemy.ScissorsStrike();
        Check(health.CurrentHealth == 90, "duplicate event cannot deal damage twice");
        yield return new WaitForSeconds(0.8f);

        health.ResetHealth(); BeginAttack();
        playerObject.transform.position = origin + Vector3.forward * 8;
        yield return new WaitForSeconds(1.75f);
        Check(health.CurrentHealth == 100, "escaping range avoids damage");
        playerObject.transform.position = origin + Vector3.forward * 2.7f;
        wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position = origin + new Vector3(0, 1, 1.4f);
        wall.transform.localScale = new Vector3(3, 3, 0.2f);
        Physics.SyncTransforms(); BeginAttack();
        yield return new WaitForSeconds(1.75f);
        Check(health.CurrentHealth == 100, "wall blocks melee damage");
        Destroy(wall); yield return null;

        BeginAttack(); yield return new WaitForSeconds(0.2f);
        enemy.TakeDamage(10); yield return new WaitForSeconds(0.3f);
        Check(enemy.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Stunned"), "hit enters Stunned animation");
        yield return new WaitForSeconds(1f);
        Check(health.CurrentHealth == 100 && !enemy.IsAttacking, "stun cancels pending attack damage");

        BeginAttack(); yield return new WaitForSeconds(0.2f);
        ZombieAI projectileTarget = enemy;
        projectileTarget.TakeDamage(100);
        yield return new WaitForSeconds(0.9f);
        Check(enemy.IsDead && enemy.CurrentHealth == 0 && !agent.enabled && !enemy.GetComponent<Collider>().enabled,
            "projectile-compatible damage disables dead enemy agent and collider");
        Check(enemy.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Dead") && health.CurrentHealth == 100,
            "death animation is terminal and cancels pending attack damage");
        string report = "SCISSORS_COMBAT_CHECKS\n" + string.Join("\n", results);
        Debug.Log(report);
        System.IO.File.WriteAllText("C:/Users/student/Documents/Codex/2026-10-06/unity-plugin-unity-openai-curated-remote/outputs/Scissors-Combat-Checks.txt", report);
        Destroy(playerObject); Destroy(enemyObject); meshInstance.Remove(); Destroy(meshData);
        EditorApplication.delayCall += () => EditorApplication.ExitPlaymode();
    }
    private void OnDestroy()
    {
        if (playerObject != null) Destroy(playerObject);
        if (enemyObject != null) Destroy(enemyObject);
        if (wall != null) Destroy(wall);
        if (meshInstance.valid) meshInstance.Remove();
        if (meshData != null) Destroy(meshData);
    }
    private static void Destroy(UnityEngine.Object obj) { UnityEngine.Object.Destroy(obj); }
}
