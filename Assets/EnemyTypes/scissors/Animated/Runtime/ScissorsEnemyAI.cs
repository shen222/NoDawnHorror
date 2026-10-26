using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Inherits ZombieAI so the existing projectile damage lookup works for this enemy.
[RequireComponent(typeof(Animator), typeof(NavMeshAgent), typeof(CapsuleCollider))]
public sealed class ScissorsEnemyAI : ZombieAI
{
    [Header("Scissors combat")]
    [Range(10f, 180f)] public float attackConeAngle = 90f;
    public float attackDuration = 1.6f;
    public float stunDuration = 1f;
    public LayerMask obstructionLayers = ~0;
    public bool autoFindPlayer = true;

    public bool IsDead => dead;
    public bool IsAttacking => attacking;
    public int CurrentHealth => health;
    private NavMeshAgent navigation;
    private Collider bodyCollider;
    private HealthManager targetHealth;
    private bool dead, stunned, attacking, strikeConsumed;
    private int health;
    private float nextAttackTime;
    private Vector3? rememberedPosition;

    protected override void Awake()
    {
        ConfigureEnemyAudio();
        navigation = GetComponent<NavMeshAgent>();
        bodyCollider = GetComponent<Collider>();
        if (animator == null) animator = GetComponent<Animator>();
        health = maxHealth;
        bodyCollider.isTrigger = true;
        animator.applyRootMotion = false;
        animator.SetBool("Alive", true);
        var preview = GetComponent<ScissorsAnimationPreview>();
        if (preview != null) preview.previewWalkAndAttack = false;
    }

    private void Start()
    {
        FindPlayer();
        if (navigation.enabled && !navigation.isOnNavMesh)
        {
            var filter = new NavMeshQueryFilter { agentTypeID = navigation.agentTypeID, areaMask = navigation.areaMask };
            if (NavMesh.SamplePosition(transform.position, out var hit, 1f, filter)) navigation.Warp(hit.position);
            else Debug.LogWarning("Scissors enemy needs to be placed on the baked NavMesh.", this);
        }
    }

    private void FindPlayer()
    {
        if (player == null && autoFindPlayer)
        {
            var healthManager = FindFirstObjectByType<HealthManager>();
            if (healthManager != null) player = healthManager.transform;
        }
        targetHealth = player != null ? player.GetComponentInParent<HealthManager>() : null;
        if (targetHealth == null && player != null) targetHealth = player.GetComponentInChildren<HealthManager>();
    }

    protected override void Update()
    {
        if (dead || stunned || attacking) return;
        if (player == null) FindPlayer();
        if (player == null || targetHealth == null || targetHealth.IsDead)
        {
            StopMoving();
            return;
        }
        if (!navigation.enabled || !navigation.isOnNavMesh)
        {
            animator.SetBool("Pursue", false);
            return;
        }

        Vector3 direction = player.position - transform.position;
        direction.y = 0;
        bool inVision = direction.sqrMagnitude <= visionDistance * visionDistance &&
            Vector3.Angle(transform.forward, direction) <= visionConeAngle * 0.5f && ClearStrikePath();
        bool heard = direction.sqrMagnitude <= hearingRadius * hearingRadius;
        if (inVision || heard) rememberedPosition = player.position;
        if (rememberedPosition.HasValue && Time.time >= nextAttackTime && CanStrike())
        {
            StartCoroutine(PerformScissorsAttack());
            return;
        }
        if (rememberedPosition.HasValue)
        {
            navigation.isStopped = false;
            navigation.speed = walkSpeed;
            navigation.angularSpeed = turnSpeed;
            navigation.SetDestination(rememberedPosition.Value);
            if (!inVision && !heard && !navigation.pathPending && navigation.hasPath &&
                navigation.remainingDistance <= navigation.stoppingDistance + 0.15f)
                rememberedPosition = null;
            bool moving = navigation.velocity.sqrMagnitude > 0.01f;
            animator.SetBool("Pursue", moving);
            if (!moving) FacePlayer();
        }
        else StopMoving();
    }

    private IEnumerator PerformScissorsAttack()
    {
        attacking = true;
        strikeConsumed = false;
        nextAttackTime = Time.time + Mathf.Max(attackCooldown, attackDuration);
        StopMoving();
        FacePlayer(true);
        animator.ResetTrigger("Stunned");
        animator.SetTrigger("Attack");
        yield return new WaitForSeconds(attackDuration);
        attacking = false;
    }

    // Called once by the Attack clip at frame 17 (the blades snap closed).
    public void ScissorsStrike()
    {
        if (!attacking || dead || stunned || strikeConsumed) return;
        strikeConsumed = true;
        if (CanStrike()) targetHealth.TakeDamage(damage);
    }

    private bool CanStrike()
    {
        if (player == null || targetHealth == null || targetHealth.IsDead) return false;
        Vector3 direction = player.position - transform.position;
        if (Mathf.Abs(direction.y) > 1.8f) return false;
        direction.y = 0;
        return direction.sqrMagnitude <= attackRange * attackRange &&
            Vector3.Angle(transform.forward, direction) <= attackConeAngle * 0.5f && ClearStrikePath();
    }

    private bool ClearStrikePath()
    {
        if (player == null) return false;
        Vector3 start = transform.position + Vector3.up * 1.2f;
        Vector3 end = player.position + Vector3.up;
        Vector3 direction = end - start;
        foreach (var hit in Physics.RaycastAll(start, direction.normalized, direction.magnitude,
                     obstructionLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
            if (hit.collider.GetComponentInParent<HealthManager>() == targetHealth) continue;
            if (hit.transform == player || hit.transform.IsChildOf(player)) continue;
            return false;
        }
        return true;
    }

    private void FacePlayer(bool immediately = false)
    {
        if (player == null) return;
        Vector3 direction = player.position - transform.position;
        direction.y = 0;
        if (direction.sqrMagnitude < 0.0001f) return;
        var rotation = Quaternion.LookRotation(direction);
        transform.rotation = immediately ? rotation : Quaternion.RotateTowards(transform.rotation, rotation, turnSpeed * Time.deltaTime);
    }

    private void StopMoving()
    {
        animator.SetBool("Pursue", false);
        if (navigation.enabled && navigation.isOnNavMesh)
        {
            navigation.isStopped = true;
            navigation.ResetPath();
        }
    }

    public override void TakeDamage(int amount)
    {
        if (dead || amount <= 0) return;
        health = Mathf.Max(0, health - amount);
        StopAllCoroutines();
        attacking = false;
        strikeConsumed = true;
        StopMoving();
        animator.ResetTrigger("Attack");
        if (health == 0)
        {
            dead = true;
            stunned = false;
            animator.ResetTrigger("Stunned");
            animator.SetBool("Alive", false);
            animator.SetTrigger("Dead");
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
            navigation.enabled = false;
            StartCoroutine(RemoveBody());
        }
        else
        {
            stunned = true;
            animator.SetTrigger("Stunned");
            StartCoroutine(Recover());
        }
    }

    private IEnumerator Recover()
    {
        yield return new WaitForSeconds(stunDuration);
        stunned = false;
        nextAttackTime = Mathf.Max(nextAttackTime, Time.time + 0.2f);
    }

    private IEnumerator RemoveBody()
    {
        yield return new WaitForSeconds(3f);
        Vector3 from = transform.position;
        for (float elapsed = 0; elapsed < 2f; elapsed += Time.deltaTime)
        {
            transform.position = from + Vector3.down * (elapsed / 2f);
            yield return null;
        }
        Destroy(gameObject);
    }
}
