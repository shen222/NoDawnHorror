using System.Collections.Generic;
using UnityEngine;

public abstract class PickupItem : MonoBehaviour
{
    public static readonly HashSet<PickupItem> Active = new HashSet<PickupItem>();
    [Tooltip("Name shown in the pickup confirmation.")]
    public string itemName;
    [TextArea] public string description;
    [Min(.1f)] public float interactionRange = 1.8f;
    [Tooltip("Optional visual to show in the preview. Defaults to this item's meshes.")]
    public Transform previewModel;
    private bool collected;

    protected abstract string DefaultName { get; }
    public string DisplayName => string.IsNullOrWhiteSpace(itemName) ? DefaultName : itemName;
    public bool IsAvailable => isActiveAndEnabled && !collected;

    protected virtual void OnEnable() => Active.Add(this);
    protected virtual void OnDisable() => Active.Remove(this);

    public bool TryCollect(TankControls player, out string message)
    {
        message = "";
        if (!IsAvailable || player == null) return false;
        var health = player.GetComponent<HealthManager>();
        if (health == null || health.IsDead) return false;
        if (!GiveTo(player, out message)) return false;
        collected = true;
        gameObject.SetActive(false);
        Destroy(gameObject);
        return true;
    }

    protected abstract bool GiveTo(TankControls player, out string message);

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(.6f, .85f, .6f);
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
