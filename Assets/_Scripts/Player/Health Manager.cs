using UnityEngine;

public class HealthManager : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip bloodHitSFX;
    [SerializeField] private AudioClip reactionSFX;

    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private Animator animator;
    [SerializeField] private ParticleSystem bloodParticlePrefab;
    private int currentHealth;

    public bool IsDead { get; private set; }
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    public delegate void HealthChanged(int currentHealth, int maxHealth);
    public event HealthChanged OnHealthChanged;

    public delegate void Death();
    public event Death OnDeath;

    void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        currentHealth = maxHealth;
        IsDead = false;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        if (animator != null) animator.SetTrigger("TakeDamage");
        // Play blood splatter effect

        // Play reaction sound
        if (audioSource != null && reactionSFX != null)
        {
            audioSource.PlayOneShot(reactionSFX);
        }

        // Spawn blood effect
        if (bloodParticlePrefab != null)
        {
            ParticleSystem bloodEffect = Instantiate(
                bloodParticlePrefab,
                transform.position + Vector3.up * 1f,
                Quaternion.identity
            );

            bloodEffect.Play();
            Destroy(bloodEffect.gameObject, 5f);
        }


        if (currentHealth <= 0)
        {
            Die();
        }

        if (audioSource != null && bloodHitSFX != null) audioSource.PlayOneShot(bloodHitSFX, 2.0f);
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // send a debug message to the console
        Debug.Log("Healed for " + amount + " points. Current health: " + currentHealth);
    }

    private void Die()
    {
        IsDead = true;
        OnDeath?.Invoke();
        if (animator != null) animator.SetTrigger("Death");
        // Additional death logic (e.g., triggering animations, disabling controls) can be added here.
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        IsDead = false;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
