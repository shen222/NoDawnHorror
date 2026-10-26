using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Collider))]
public class ZombieAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Animator animator; 
    NavMeshAgent agent;

    [Header("Sensors")]
    public float visionConeAngle = 45f;
    public float visionDistance = 8f;
    public float hearingRadius = 5f;

    [Header("Movement")]
    public float walkSpeed = 1.5f;
    public float turnSpeed = 120f;

    [Header("Combat")]
    public float attackRange = 1.5f;
    public int damage = 10;
    public float attackCooldown = 1.5f;

    [Header("Health")]
    public int maxHealth = 50;
    int currentHealth;

    [Header("AI Memory")]
    Vector3? lastKnownPosition = null;

    [Header("Sounds")]
    public AudioSource audioSource;
    public AudioClip[] footstepClips;
    [Min(0.01f)] public float soundMinDistance = 1.5f;
    [Min(0.02f)] public float soundMaxDistance = 18f;

    // Apply at runtime so existing scene overrides and spawned enemies get the fix.
    protected void ConfigureEnemyAudio()
    {
        if (audioSource == null) audioSource = GetComponentInChildren<AudioSource>(true);
        foreach (var source in GetComponentsInChildren<AudioSource>(true))
            ConfigureAudioSource(source);
        if (audioSource != null && audioSource.transform.IsChildOf(transform))
            ConfigureAudioSource(audioSource);
    }

    private void ConfigureAudioSource(AudioSource source)
    {
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = Mathf.Max(0.01f, soundMinDistance);
        source.maxDistance = Mathf.Max(source.minDistance + 0.01f, soundMaxDistance);
        source.dopplerLevel = 0f;
        source.spread = 0f;
    }


    bool isDead = false;
    bool canAttack = true;
    bool pursue = false; 

    protected virtual void Awake()
    {
        ConfigureEnemyAudio();
        agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;

        Collider collider = GetComponent<Collider>();
        collider.isTrigger = true; 
    }

    protected virtual void Update()
    {
        if (isDead) return;

        bool canHearPlayer = CanHearPlayer();
        bool canSeePlayer = CanSeePlayer();

        if (canHearPlayer)
        {
            TurnTowardsPlayer();
        }

        if (canSeePlayer)
        {
            lastKnownPosition = player.position; // Update last known position
            pursue = true;
        }
        else if (lastKnownPosition.HasValue)
        {
            pursue = true;
        }
        else
        {
            pursue = false;
        }

        animator.SetBool("Pursue", pursue);

        if (pursue)
        {
            agent.speed = walkSpeed;

            if (canSeePlayer)
            {
                agent.SetDestination(player.position);
            }
            else if (lastKnownPosition.HasValue)
            {
                agent.SetDestination(lastKnownPosition.Value);

                // Clear last known position if the zombie reaches it
                if (Vector3.Distance(transform.position, lastKnownPosition.Value) <= 0.5f)
                {
                    lastKnownPosition = null;
                }
            }
        }
        else
        {
            agent.ResetPath();
        }

        if (isDead || !canAttack) return;

        // Check if the player is within attack range
        if (player != null && Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            StartCoroutine(PerformAttack());
        }
    }
    
    IEnumerator PerformAttack()
    {
        canAttack = false;
        agent.isStopped = true; 
        pursue = false;

        // Rotate to face the player
        if (player != null)
        {
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(directionToPlayer.x, 0, directionToPlayer.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * turnSpeed);
        }

        animator.SetTrigger("Attack");
        
        // Wait for the attack animation to reach the damage frame
        yield return new WaitForSeconds(.5f);

        // Check if the player is still within range during the damage frame
        if (player != null && Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            HealthManager playerHealth = player.GetComponent<HealthManager>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
        }
        
        yield return new WaitForSeconds(attackCooldown);

        agent.isStopped = false; 
        canAttack = true;
    }

    public virtual void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            animator.SetTrigger("Stunned");
            agent.isStopped = true; 
            StartCoroutine(RecoverFromStun());
        }
    }

    public void PlayFootstep()
    {
        if (audioSource == null || footstepClips == null || footstepClips.Length == 0) return;

        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        audioSource.PlayOneShot(clip);
    }

    IEnumerator RecoverFromStun()
    {
        yield return new WaitForSeconds(1f); 
        agent.isStopped = false; 
    }

    void Die()
    {
        isDead = true;
        agent.isStopped = true;
        animator.SetTrigger("Dead");
        
        // Disable the collider
        Collider collider = GetComponent<Collider>();
        collider.enabled = false;
        
        //after a delay
       
        
        
        //sink into the ground
        StartCoroutine(SinkIntoGround());
        
        
        
    }
    
    IEnumerator SinkIntoGround()
    {
        float sinkDuration = 2f;
        float sinkSpeed = 0.5f;
        float elapsedTime = 0f;
        float delayBeforeSink = 3f;

        Vector3 initialPosition = transform.position;
        Vector3 targetPosition = initialPosition - new Vector3(0, 1f, 0);
        
        yield return new WaitForSeconds(delayBeforeSink);
        
        while (elapsedTime < sinkDuration)
        {
            transform.position = Vector3.Lerp(initialPosition, targetPosition, (elapsedTime / sinkDuration));
            elapsedTime += Time.deltaTime * sinkSpeed;
            yield return null;
        }

        transform.position = targetPosition;
        Destroy(gameObject);
    }

    bool CanSeePlayer()
    {
        if (player == null || isDead) return false;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        return angle <= visionConeAngle / 2 && Vector3.Distance(transform.position, player.position) <= visionDistance;
    }

    bool CanHearPlayer()
    {
        if (player == null || isDead) return false;

        return Vector3.Distance(transform.position, player.position) <= hearingRadius;
    }

    void TurnTowardsPlayer()
    {
        if (player == null) return;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(directionToPlayer.x, 0, directionToPlayer.z));
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, turnSpeed * Time.deltaTime);
    }

    void OnDrawGizmos()
    {
        // Draw vision cone
        Gizmos.color = Color.yellow;
        Vector3 leftBoundary = Quaternion.Euler(0, -visionConeAngle / 2, 0) * transform.forward * visionDistance;
        Vector3 rightBoundary = Quaternion.Euler(0, visionConeAngle / 2, 0) * transform.forward * visionDistance;
        Gizmos.DrawRay(transform.position, leftBoundary);
        Gizmos.DrawRay(transform.position, rightBoundary);
        Gizmos.DrawWireSphere(transform.position, visionDistance);
    
        // Draw hearing radius
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);
    
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}

