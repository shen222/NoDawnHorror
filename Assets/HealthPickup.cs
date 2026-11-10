using UnityEngine;

public class HealthPickup : PickupItem
{
    [Min(1)] public int healAmount = 50;
    protected override string DefaultName => "回復薬";

    protected override bool GiveTo(TankControls player, out string message)
    {
        var health = player.GetComponent<HealthManager>();
        message = "";
        if (health.CurrentHealth >= health.MaxHealth)
        {
            message = "今は回復する必要がない。";
            return false;
        }
        if (healAmount <= 0) return false;
        health.Heal(healAmount);
        return true;
    }
}
