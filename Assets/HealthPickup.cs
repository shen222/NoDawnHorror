using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            HealthManager healthManager = other.GetComponent<HealthManager>();
            if (healthManager != null && !healthManager.IsDead)
            {
                healthManager.Heal(50);
                // play pickup sound effect 
                // play pickup vfx
                Destroy(gameObject);
            }
        }
    }
}
