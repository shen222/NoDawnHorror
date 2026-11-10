using UnityEngine;

public class AmmoPickup : PickupItem
{
    public int ammoAmount = 5;

    protected override string DefaultName => "ハンドガンの弾";

    protected override bool GiveTo(TankControls player, out string message)
    {
        Gun gun = player.EquippedGun != null ? player.EquippedGun : player.GetComponentInChildren<Gun>();
        message = "";
        if (gun == null)
        {
            message = "この弾を使える武器を持っていない。";
            return false;
        }
        if (ammoAmount <= 0) return false;
        gun.AddReserveAmmo(ammoAmount);
        return true;
    }
}
