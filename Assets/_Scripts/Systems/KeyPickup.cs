using UnityEngine;

public sealed class KeyPickup : PickupItem
{
    [Tooltip("Unique ID used by a future door or puzzle, for example courtyard_key.")]
    public string keyId;
    protected override string DefaultName => "鍵";

    protected override bool GiveTo(TankControls player, out string message)
    {
        message = "";
        if (string.IsNullOrWhiteSpace(keyId))
        {
            message = "この鍵はまだ設定されていない。";
            return false;
        }
        var inventory = player.GetComponent<KeyInventory>();
        if (inventory == null) inventory = player.gameObject.AddComponent<KeyInventory>();
        inventory.AddKey(keyId);
        return true;
    }
}
