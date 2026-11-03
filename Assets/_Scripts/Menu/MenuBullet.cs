using UnityEngine;

// Menu projectiles deliberately cannot damage actors or activate gameplay triggers.
public sealed class MenuBullet : MonoBehaviour
{
    public Vector3 direction;
    public float speed = 55f;
    public GunMenu owner;
    private float life;
    private bool resolved;

    private void Update()
    {
        if (resolved || owner == null) { Destroy(gameObject); return; }
        float distance = speed * Time.deltaTime;
        if (distance <= 0) return;
        ShootableMenuTarget nearest = null;
        float best = distance + 1;
        Vector3 point = transform.position;
        foreach (var hit in Physics.SphereCastAll(transform.position, 0.025f, direction, distance, ~0, QueryTriggerInteraction.Collide))
        {
            var target = hit.collider.GetComponent<ShootableMenuTarget>();
            if (target == null || target.owner != owner || target.IsBroken || hit.distance >= best) continue;
            nearest = target;
            point = hit.point;
            best = hit.distance;
        }
        if (nearest != null)
        {
            resolved = true;
            nearest.Hit(point);
            Destroy(gameObject);
            return;
        }
        transform.position += direction * distance;
        life += Time.deltaTime;
        if (life > 2f) Destroy(gameObject);
    }
}

