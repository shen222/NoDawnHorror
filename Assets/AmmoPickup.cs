using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    public int ammoAmount = 5;

    private void OnTriggerEnter(Collider other)
    {
        Gun gun = other.GetComponentInChildren<Gun>();
        if (gun != null)
        {
            gun.AddReserveAmmo(ammoAmount);
            //play reload sound effect
            Debug.Log($"Ammo picked up! {ammoAmount} bullets added to reserve.");
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("No gun found on player to add ammo.");
        }
    }
}
