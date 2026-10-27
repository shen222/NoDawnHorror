using UnityEngine;
using UnityEngine.AI;

// Keeps compatibility with Projectile's ZombieAI damage lookup.
[RequireComponent(typeof(NavMeshAgent), typeof(CapsuleCollider))]
public sealed class DogEnemyAI : ZombieAI
{
    public enum Behaviour { Idle, Roaming, Chasing, Biting, Hurt, Dead }

    [Header("Dog territory")]
    [Min(1)] public float roamRadius = 10f;
    [Min(1)] public float leashRadius = 24f;
    [Min(0)] public float runSpeed = 3.8f;
    [Min(0)] public float memoryDuration = 5f;
    public Vector2 idleDuration = new Vector2(1.5f, 3.5f);
    [Header("Jump attack")]
    [Tooltip("Seconds from the start of the crouch to the airborne damage check.")]
    [Min(0)] public float biteWindup = 0.65f;
    [Min(0.1f)] public float biteDuration = 1.15f;
    [Min(0)] public float jumpDistance = 1.1f;
    [Min(.1f)] public float jumpHitRange = 1.25f;
    public const float JumpClipDuration = 1.15f;
    public const float JumpHeight = .62f;
    [Range(10, 180)] public float biteAngle = 90f;
    [Min(0)] public float hurtDuration = 0.45f;
    public LayerMask obstructionLayers = ~0;

    public Behaviour State { get; private set; }
    public int CurrentHealth { get; private set; }
    public bool IsDead => State == Behaviour.Dead;
    public Vector3 HomePosition { get; private set; }
    private NavMeshAgent navigation;
    private HealthManager targetHealth;
    private TankControls targetControls;
    private NavMeshPath path;
    private Vector3 rememberedPosition;
    private float stateUntil, nextBite, nextSense, lastSensed = float.NegativeInfinity;
    private float nextRepath, nextFindPlayer, diedAt;
    private bool sawPlayer, heardPlayer, biteConsumed;
    private Vector3 deathPosition;
    private CapsuleCollider body;
    private Vector3 bodyCenter, jumpDirection;
    private float jumpStarted, jumpTravel, jumpProgress;
    private bool jumpBlocked, previousUpdateRotation;

    // Shared by the authored skeleton animation and the moving damage collider.
    public static float JumpHeightAt(float normalizedTime)
    {
        float flight = Mathf.InverseLerp(.22f, .72f, normalizedTime);
        return JumpHeight * 4f * flight * (1f - flight);
    }

    protected override void Awake()
    {
        navigation = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        ConfigureEnemyAudio();
        CurrentHealth = maxHealth;
        path = new NavMeshPath();
        GetComponent<CapsuleCollider>().isTrigger = true;
        body = GetComponent<CapsuleCollider>();
        bodyCenter = body.center;
        if (animator != null) animator.applyRootMotion = false;
    }

    private void Start()
    {
        // Only recover a nearby placement; never teleport to another floor/area.
        if (navigation.enabled && !navigation.isOnNavMesh &&
            NavMesh.SamplePosition(transform.position, out var hit, 2f, Filter))
            navigation.Warp(hit.position);
        HomePosition = transform.position;
        FindPlayer();
        EnterIdle();
        if (!Ready) Debug.LogWarning("Dog enemy is off the baked NavMesh. Place it on a walkable surface.", this);
    }

    private NavMeshQueryFilter Filter => new NavMeshQueryFilter
        { agentTypeID = navigation.agentTypeID, areaMask = navigation.areaMask };
    private bool Ready => navigation != null && navigation.enabled && navigation.isOnNavMesh;

    private void FindPlayer()
    {
        nextFindPlayer = Time.time + 1f;
        if (player == null)
        {
            var controls = FindFirstObjectByType<TankControls>();
            if (controls != null) player = controls.transform;
        }
        targetHealth = player != null ? player.GetComponentInParent<HealthManager>() : null;
        if (player != null && targetHealth == null) targetHealth = player.GetComponentInChildren<HealthManager>();
        targetControls = player != null ? player.GetComponentInParent<TankControls>() : null;
    }

    protected override void Update()
    {
        if (Time.timeScale == 0f) return;
        if (IsDead)
        {
            float sink = Mathf.Clamp01((Time.time - diedAt - 8f) / 2f);
            transform.position = deathPosition + Vector3.down * sink;
            if (sink >= 1f) Destroy(gameObject);
            return;
        }
        if (!Ready) return;
        if ((player == null || targetHealth == null) && Time.time >= nextFindPlayer) FindPlayer();
        if (animator != null) animator.SetFloat("Speed", navigation.velocity.magnitude, 0.12f, Time.deltaTime);

        if (State == Behaviour.Hurt)
        {
            if (Time.time >= stateUntil) EnterIdle();
            return;
        }
        if (State == Behaviour.Biting)
        {
            AdvanceJump();
            if (!biteConsumed && Time.time >= stateUntil - biteDuration + biteWindup)
            {
                biteConsumed = true;
                if (CanBite()) targetHealth.TakeDamage(damage);
            }
            if (Time.time >= stateUntil) { EndJump(); EnterIdle(); }
            return;
        }

        bool aliveTarget = player != null && targetHealth != null && !targetHealth.IsDead;
        if (Time.time >= nextSense)
        {
            nextSense = Time.time + 0.15f;
            sawPlayer = heardPlayer = false;
            if (aliveTarget && (player.position - HomePosition).sqrMagnitude <= leashRadius * leashRadius)
            {
                Vector3 offset = player.position - transform.position;
                offset.y = 0;
                sawPlayer = offset.sqrMagnitude <= visionDistance * visionDistance &&
                    Vector3.Angle(transform.forward, offset) <= visionConeAngle * 0.5f && ClearPathToPlayer();
                // Hearing reacts to movement, not a motionless player behind scenery.
                bool moving = targetControls != null &&
                    (Mathf.Abs(targetControls.VerticalInput) > 0.15f || targetControls.IsQuickTurning);
                heardPlayer = moving && offset.sqrMagnitude <= hearingRadius * hearingRadius;
                if (sawPlayer || heardPlayer)
                {
                    rememberedPosition = player.position;
                    lastSensed = Time.time;
                }
            }
        }
        bool hunting = aliveTarget && Time.time - lastSensed <= memoryDuration &&
            (player.position - HomePosition).sqrMagnitude <= leashRadius * leashRadius;
        if (hunting)
        {
            if (Time.time >= nextBite && CanBite(true))
            {
                StopMoving();
                State = Behaviour.Biting;
                biteConsumed = false;
                stateUntil = Time.time + biteDuration;
                nextBite = Time.time + Mathf.Max(biteDuration, attackCooldown);
                jumpStarted = Time.time;
                jumpProgress = 0;
                jumpBlocked = false;
                Vector3 offset = player.position - transform.position;
                offset.y = 0;
                jumpDirection = offset.sqrMagnitude > .001f ? offset.normalized : transform.forward;
                transform.rotation = Quaternion.LookRotation(jumpDirection);
                jumpTravel = Mathf.Min(jumpDistance, Mathf.Max(0, offset.magnitude - .65f));
                previousUpdateRotation = navigation.updateRotation;
                navigation.updateRotation = false;
                if (animator != null)
                {
                    animator.SetFloat("AttackSpeed", JumpClipDuration / Mathf.Max(.1f, biteDuration));
                    animator.SetTrigger("Attack");
                }
                return;
            }
            State = Behaviour.Chasing;
            if (Time.time >= nextRepath)
            {
                nextRepath = Time.time + 0.25f;
                Vector3 target = sawPlayer || heardPlayer ? player.position : rememberedPosition;
                if (!MoveTo(target, runSpeed))
                {
                    // Reject partial paths instead of running into an unreachable wall forever.
                    lastSensed = float.NegativeInfinity;
                    EnterIdle();
                }
            }
            return;
        }
        if (State == Behaviour.Chasing) EnterIdle();
        if (State == Behaviour.Roaming)
        {
            if (Time.time >= stateUntil || (!navigation.pathPending && (!navigation.hasPath ||
                navigation.pathStatus != NavMeshPathStatus.PathComplete || navigation.remainingDistance <= navigation.stoppingDistance + .1f))) EnterIdle();
        }
        else if (Time.time >= stateUntil) ChooseRoamPoint();
    }

    private void ChooseRoamPoint()
    {
        for (int attempt = 0; attempt < 16; attempt++)
        {
            Vector2 circle = Random.insideUnitCircle * roamRadius;
            Vector3 candidate = HomePosition + new Vector3(circle.x, 0, circle.y);
            if (!NavMesh.SamplePosition(candidate, out var hit, 1.5f, Filter) ||
                Mathf.Abs(hit.position.y - HomePosition.y) > 2f ||
                (hit.position - HomePosition).sqrMagnitude > roamRadius * roamRadius ||
                (hit.position - transform.position).sqrMagnitude < 2f) continue;
            if (MoveTo(hit.position, walkSpeed)) { State = Behaviour.Roaming; stateUntil = Time.time + 12f; return; }
        }
        // Return toward home if a chase led beyond the roaming territory.
        if ((transform.position - HomePosition).sqrMagnitude > roamRadius * roamRadius && MoveTo(HomePosition, walkSpeed))
        { State = Behaviour.Roaming; stateUntil = Time.time + 12f; }
        else EnterIdle();
    }

    private bool MoveTo(Vector3 destination, float speed)
    {
        if (!Ready || !NavMesh.SamplePosition(destination, out var hit, 1.5f, Filter) ||
            !navigation.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete) return false;
        navigation.speed = speed;
        navigation.stoppingDistance = speed <= walkSpeed ? .15f : attackRange * .65f;
        navigation.isStopped = false;
        return navigation.SetPath(path);
    }

    private void EnterIdle()
    {
        StopMoving();
        State = Behaviour.Idle;
        stateUntil = Time.time + Random.Range(idleDuration.x, Mathf.Max(idleDuration.x, idleDuration.y));
    }

    private void StopMoving()
    {
        if (!Ready) return;
        navigation.isStopped = true;
        navigation.ResetPath();
        if (animator != null) animator.SetFloat("Speed", 0f);
    }

    private void AdvanceJump()
    {
        float t = Mathf.Clamp01((Time.time - jumpStarted) / Mathf.Max(.1f, biteDuration));
        body.center = bodyCenter + Vector3.up * JumpHeightAt(t);
        float progress = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.22f, .72f, t));
        float distance = (progress - jumpProgress) * jumpTravel;
        jumpProgress = progress;
        if (jumpBlocked || distance <= 0 || !Ready) return;

        // Keep the ground path on the NavMesh. The skeleton supplies the vertical arc.
        // Direction is committed at takeoff, so the player can sidestep the pounce.
        Vector3 from = transform.position;
        if (NavMesh.Raycast(from, from + jumpDirection * distance, out var edge, Filter))
        {
            distance = Mathf.Max(0, Vector3.Distance(from, edge.position) - .04f);
            jumpBlocked = true;
        }
        // Sweep the body along the ground path as well, to respect solid scenery
        // even when the baked NavMesh is older than a newly placed obstacle.
        foreach (var hit in Physics.CapsuleCastAll(from + Vector3.up * .35f, from + Vector3.up * .9f,
                     navigation.radius, jumpDirection, distance, obstructionLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
            if (hit.collider.GetComponentInParent<HealthManager>() == targetHealth) continue;
            if (hit.distance >= distance) continue;
            distance = Mathf.Max(0, hit.distance - .04f);
            jumpBlocked = true;
        }
        if (distance > 0) navigation.Move(jumpDirection * distance);
    }

    private void EndJump()
    {
        if (body != null) body.center = bodyCenter;
        if (navigation != null) navigation.updateRotation = previousUpdateRotation;
    }

    private void OnDisable()
    {
        if (State == Behaviour.Biting)
        {
            EndJump();
            biteConsumed = true;
            State = Behaviour.Idle;
            StopMoving();
            if (animator != null && animator.isActiveAndEnabled)
            { animator.ResetTrigger("Attack"); animator.Play("Locomotion", 0, 0); }
        }
    }

    private bool CanBite(bool starting = false)
    {
        if (player == null || targetHealth == null || targetHealth.IsDead) return false;
        Vector3 direction = player.position - transform.position;
        if (Mathf.Abs(direction.y) > 1.5f) return false;
        direction.y = 0;
        float range = starting ? attackRange : jumpHitRange;
        return direction.sqrMagnitude <= range * range &&
            Vector3.Angle(transform.forward, direction) <= biteAngle * 0.5f && ClearPathToPlayer();
    }

    private bool ClearPathToPlayer()
    {
        if (player == null) return false;
        Vector3 start = transform.position + Vector3.up * 0.65f;
        Vector3 end = player.position + Vector3.up * 0.65f;
        foreach (var hit in Physics.RaycastAll(start, (end - start).normalized, Vector3.Distance(start, end),
                     obstructionLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
            if (hit.collider.GetComponentInParent<HealthManager>() == targetHealth) continue;
            if (hit.transform == player || hit.transform.IsChildOf(player)) continue;
            return false;
        }
        return true;
    }

    public override void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0) return;
        if (State == Behaviour.Biting) EndJump();
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        StopMoving();
        biteConsumed = true;
        if (animator != null) animator.ResetTrigger("Attack");
        if (CurrentHealth == 0)
        {
            State = Behaviour.Dead;
            diedAt = Time.time;
            deathPosition = transform.position;
            if (animator != null) { animator.ResetTrigger("Hurt"); animator.SetBool("Dead", true); }
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
            if (navigation != null) navigation.enabled = false;
            return;
        }
        State = Behaviour.Hurt;
        stateUntil = Time.time + hurtDuration;
        nextBite = Mathf.Max(nextBite, stateUntil + 0.3f);
        if (animator != null) animator.SetTrigger("Hurt");
        if (player != null) { rememberedPosition = player.position; lastSensed = Time.time; }
    }
}
