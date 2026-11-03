using UnityEngine;

public sealed class ShootableMenuTarget : MonoBehaviour
{
    public GunMenu owner;
    public GunMenu.MenuAction action;
    public bool IsBroken { get; private set; }

    public bool Hit(Vector3 point)
    {
        if (IsBroken || owner == null || owner.IsTransitioning) return false;
        IsBroken = true;
        var collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = false;
        owner.TargetHit(this, point);
        return true;
    }
}

