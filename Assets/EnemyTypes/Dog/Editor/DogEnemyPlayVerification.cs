using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

// Runs only in the editor and only when explicitly requested in Play mode.
public static class DogEnemyPlayVerification
{
    public static string Status { get; private set; } = "Not run";
    private static IEnumerator<float> routine;
    private static double nextStep;
    private static DogEnemyAI dog;
    private static TankControls player;
    private static HealthManager health;
    private static Vector3 home, originalPlayerPosition;
    private static GameObject wall;
    private static readonly List<string> passed = new List<string>();

    public static void Begin()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play mode first.");
        if (routine != null) throw new InvalidOperationException("Dog verification is already running.");
        passed.Clear();
        Status = "Running";
        routine = Run();
        nextStep = 0;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup < nextStep) return;
        try
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Play mode ended during verification.");
            if (!routine.MoveNext())
            {
                Status = "PASS: " + string.Join("; ", passed);
                Finish();
                return;
            }
            nextStep = EditorApplication.timeSinceStartup + routine.Current;
        }
        catch (Exception exception)
        {
            Status = "FAIL: " + exception.Message + " | Passed: " + string.Join("; ", passed);
            Finish();
            Debug.LogException(exception);
        }
    }

    private static void Finish()
    {
        EditorApplication.update -= Tick;
        routine?.Dispose();
        routine = null;
        Time.timeScale = 1;
        if (wall != null) UnityEngine.Object.Destroy(wall);
        if (dog != null) UnityEngine.Object.Destroy(dog.gameObject);
        if (player != null) player.transform.position = originalPlayerPosition;
        System.IO.Directory.CreateDirectory("Logs/DogEnemy");
        System.IO.File.WriteAllText("Logs/DogEnemy/play-verification.txt", Status);
        Debug.Log(Status);
    }

    private static IEnumerator<float> Run()
    {
        player = UnityEngine.Object.FindFirstObjectByType<TankControls>();
        health = player.GetComponent<HealthManager>();
        originalPlayerPosition = player.transform.position;
        home = originalPlayerPosition;
        if (!NavMesh.SamplePosition(home, out var hit, 2, NavMesh.AllAreas)) throw new InvalidOperationException("Player area has no NavMesh.");
        home = hit.position;
        foreach (var enemy in UnityEngine.Object.FindObjectsByType<ZombieAI>(FindObjectsSortMode.None)) enemy.gameObject.SetActive(false);
        player.enabled = false;
        var controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        foreach (var dialogue in UnityEngine.Object.FindObjectsByType<DialogueSystem>(FindObjectsSortMode.None)) dialogue.enabled = false;
        foreach (var director in UnityEngine.Object.FindObjectsByType<StoryCameraDirector>(FindObjectsSortMode.None)) director.CancelShot();
        player.transform.position = home + Vector3.forward * 60;
        Spawn();
        dog.visionDistance = dog.hearingRadius = 0;
        dog.idleDuration = new Vector2(.1f, .1f);
        int frame = Time.frameCount;
        yield return .4f;
        var groundAgent = dog.GetComponent<NavMeshAgent>();
        Check(Mathf.Abs(groundAgent.baseOffset) < .001f && Mathf.Abs(dog.transform.position.y - groundAgent.nextPosition.y) < .05f,
            "dog stands at NavMesh ground height");
        Vector3 start = dog.transform.position;
        yield return 3f;
        Check(Time.frameCount > frame + 3, "player loop advances");
        Check(dog.GetComponent<NavMeshAgent>().isOnNavMesh && Vector3.Distance(start, dog.transform.position) > .4f, "patrol travels on baked NavMesh");
        Vector3 first = dog.transform.position;
        yield return 3f;
        Check(Vector3.Distance(first, dog.transform.position) > .2f || dog.State == DogEnemyAI.Behaviour.Idle, "patrol arrives or continues instead of waiting short of a waypoint");

        Spawn();
        yield return .1f;
        Vector3 target = FindClearTarget(4f);
        player.transform.position = target;
        dog.transform.rotation = Quaternion.LookRotation(target - dog.transform.position);
        start = dog.transform.position;
        yield return 1f;
        Check(dog.State == DogEnemyAI.Behaviour.Chasing || dog.State == DogEnemyAI.Behaviour.Biting, "visible player triggers pursuit");
        Check(Vector3.Distance(start, dog.transform.position) > .3f, "pursuit advances toward player");

        Spawn();
        yield return .1f;
        PutPlayerInBiteRange(1.9f);
        health.ResetHealth();
        yield return .2f;
        Check(dog.State == DogEnemyAI.Behaviour.Biting, "jump starts with a crouch windup");
        int before = health.CurrentHealth;
        Check(before == health.MaxHealth, "crouch deals no early damage");
        start = dog.transform.position;
        yield return .3f;
        Check(Vector3.Distance(start, dog.transform.position) > .2f, "jump lunges forward toward its committed target");
        Check(dog.GetComponent<CapsuleCollider>().center.y > 1f, "damage collider follows the airborne body");
        Check(dog.transform.Find("Rig/Hips").localPosition.y > 1.15f, "animated skeleton rises into the air");
        Time.timeScale = 0;
        Vector3 airbornePosition = dog.transform.position;
        float airborneCollider = dog.GetComponent<CapsuleCollider>().center.y;
        yield return .2f;
        Check(Vector3.Distance(airbornePosition, dog.transform.position) < .001f &&
            Mathf.Abs(airborneCollider - dog.GetComponent<CapsuleCollider>().center.y) < .001f, "pause freezes an airborne pounce");
        Time.timeScale = 1;
        yield return .35f;
        Check(health.CurrentHealth == before - dog.damage, "jump impact deals exactly 15 damage once");
        yield return .25f;
        Check(health.CurrentHealth == before - dog.damage, "bite cannot hit twice during recovery");
        yield return .3f;
        Check(Mathf.Abs(dog.GetComponent<CapsuleCollider>().center.y - .62f) < .01f &&
            dog.GetComponent<NavMeshAgent>().isOnNavMesh, "landing restores the ground collider and navigation");

        Spawn();
        yield return .1f;
        PutPlayerInBiteRange();
        health.ResetHealth();
        yield return .2f;
        Check(dog.State == DogEnemyAI.Behaviour.Biting, "dodge test reaches windup");
        Vector3 lockedDirection = dog.transform.forward;
        player.transform.position += dog.transform.right * 4;
        yield return .6f;
        Check(health.CurrentHealth == health.MaxHealth, "sidestepping the committed jump avoids damage");
        Check(Vector3.Dot(lockedDirection, dog.transform.forward) > .99f, "jump does not home toward a dodging player");

        Spawn();
        yield return .1f;
        PutPlayerInBiteRange();
        health.ResetHealth();
        yield return .2f;
        Check(dog.State == DogEnemyAI.Behaviour.Biting, "obstruction test reaches windup");
        wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Temporary dog bite obstruction";
        wall.transform.position = dog.transform.position + dog.transform.forward * .45f + Vector3.up * .7f;
        wall.transform.rotation = dog.transform.rotation;
        wall.transform.localScale = new Vector3(1.5f, 2, .12f);
        Physics.SyncTransforms();
        start = dog.transform.position;
        yield return .6f;
        Check(health.CurrentHealth == health.MaxHealth, "wall blocks bite at its damage frame");
        Check(Vector3.Distance(start, dog.transform.position) < .2f, "lunge cannot move through a newly placed wall");
        UnityEngine.Object.Destroy(wall);
        wall = null;

        Spawn();
        yield return .1f;
        PutPlayerInBiteRange();
        health.ResetHealth();
        yield return .45f;
        Check(dog.GetComponent<CapsuleCollider>().center.y > .9f, "interruption test reaches the airborne phase");
        dog.TakeDamage(10);
        Check(dog.CurrentHealth == 30 && dog.State == DogEnemyAI.Behaviour.Hurt, "damage removes health and interrupts bite");
        Check(Mathf.Abs(dog.GetComponent<CapsuleCollider>().center.y - .62f) < .01f, "interrupting a jump restores its damage collider");
        start = dog.transform.position;
        player.transform.position += dog.transform.forward * 6;
        yield return .4f;
        Check(health.CurrentHealth == health.MaxHealth, "interrupted bite deals no damage");
        Check(Vector3.Distance(start, dog.transform.position) < .05f, "hit reaction stops the jump's forward travel");

        Spawn();
        yield return .1f;
        player.transform.position = home + Vector3.forward * 60;
        dog.enabled = false;
        var agent = dog.GetComponent<NavMeshAgent>();
        agent.isStopped = true;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/projectile.prefab");
        Vector3 aim = dog.transform.position + Vector3.up * .62f;
        var shot = UnityEngine.Object.Instantiate(prefab, aim + dog.transform.forward * 1.5f, Quaternion.LookRotation(-dog.transform.forward));
        var projectile = shot.GetComponent<Projectile>();
        projectile.damage = 10;
        projectile.speed = 20;
        projectile.Initialize(player.transform);
        yield return .3f;
        Check(dog.CurrentHealth == 30, "existing projectile sweep damages the dog");
        dog.TakeDamage(-10);
        Check(dog.CurrentHealth == 30, "invalid damage cannot heal the dog");
        dog.enabled = true;
        Time.timeScale = 0;
        Vector3 pausedPosition = dog.transform.position;
        float clipTime = dog.animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        yield return .3f;
        Check(Vector3.Distance(pausedPosition, dog.transform.position) < .001f &&
            Mathf.Abs(clipTime - dog.animator.GetCurrentAnimatorStateInfo(0).normalizedTime) < .01f, "pause freezes movement and animation");
        Time.timeScale = 1;
        dog.TakeDamage(100);
        yield return .2f;
        Check(dog.IsDead && dog.CurrentHealth == 0 && !agent.enabled &&
            dog.GetComponentsInChildren<Collider>().All(c => !c.enabled), "death disables navigation and hitboxes");
        dog.TakeDamage(100);
        Check(dog.CurrentHealth == 0, "dead dog ignores additional damage");
        yield return 1.1f;
        Check(dog.animator.GetCurrentAnimatorStateInfo(0).IsName("Death"), "death animation remains on its final pose");
    }

    private static void Spawn()
    {
        if (dog != null) { dog.gameObject.SetActive(false); UnityEngine.Object.Destroy(dog.gameObject); }
        dog = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(DogEnemySetup.PrefabPath), home, Quaternion.identity).GetComponent<DogEnemyAI>();
        dog.name = "Dog enemy verification";
        dog.player = player.transform;
        dog.visionConeAngle = 360;
        dog.GetComponent<NavMeshAgent>().Warp(home);
    }

    private static Vector3 FindClearTarget(float distance)
    {
        for (int i = 0; i < 16; i++)
        {
            Vector3 offset = Quaternion.Euler(0, i * 22.5f, 0) * Vector3.forward * distance;
            if (!NavMesh.SamplePosition(home + offset, out var hit, 1f, NavMesh.AllAreas)) continue;
            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(home, hit.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) continue;
            if (Physics.Linecast(home + Vector3.up * .65f, hit.position + Vector3.up * .65f, ~0, QueryTriggerInteraction.Ignore)) continue;
            return hit.position;
        }
        throw new InvalidOperationException("No clear test destination near player.");
    }

    private static void PutPlayerInBiteRange(float distance = 1.1f)
    {
        var agent = dog.GetComponent<NavMeshAgent>();
        agent.Warp(home);
        dog.transform.rotation = Quaternion.LookRotation(FindClearTarget(2f) - home);
        player.transform.position = home + dog.transform.forward * distance;
        Physics.SyncTransforms();
    }

    private static void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
        passed.Add(description);
    }
}
