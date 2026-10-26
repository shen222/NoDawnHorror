using UnityEngine;

public class Projectile : MonoBehaviour
{
    public GameObject hitTargetVFXPrefab;
    public GameObject hitEnemyVFXPrefab;
    public float speed = 20f;
    public float lifeTime = 20f;
    public int damage = 10;
    [Tooltip("Draw bullet travel and log the collider hit in the Console.")]
    public bool debugHits;

    private Transform owner;
    private Collider[] ownColliders;
    private float radius = 0.01f;
    private bool hit;

    private void Awake()
    {
        ownColliders = GetComponentsInChildren<Collider>();
        var sphere = GetComponent<SphereCollider>();
        if (sphere != null)
        {
            Vector3 scale = transform.lossyScale;
            radius = Mathf.Max(0.001f, sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        }
    }

    public void Initialize(Transform shooter)
    {
        owner = shooter;
        if (owner == null) return;
        foreach (var own in ownColliders)
            foreach (var shooterCollider in owner.GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(own, shooterCollider);
    }

    private void Start() { Destroy(gameObject, lifeTime); }

    private void Update()
    {
        if (hit || Time.timeScale == 0f) return;
        Vector3 start = transform.position;
        Vector3 direction = transform.forward;
        float distance = Mathf.Max(0f, speed * Time.deltaTime);
        if (distance == 0f) return;

        // Sphere casts do not detect initial overlaps. Check these separately.
        foreach (var other in Physics.OverlapSphere(start, radius, ~0, QueryTriggerInteraction.Collide))
        {
            if (!CanHit(other)) continue;
            ResolveHit(other, start);
            return;
        }

        // Sweep the entire movement segment, including enemy trigger colliders.
        RaycastHit[] hits = Physics.SphereCastAll(start, radius, direction, distance, ~0, QueryTriggerInteraction.Collide);
        int nearest = -1;
        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].distance >= nearestDistance || !CanHit(hits[i].collider)) continue;
            nearest = i;
            nearestDistance = hits[i].distance;
        }
        if (nearest >= 0)
        {
            transform.position = start + direction * nearestDistance;
            ResolveHit(hits[nearest].collider, hits[nearest].point);
            return;
        }
        transform.position = start + direction * distance;
    }

    private bool CanHit(Collider other)
    {
        if (other == null || !other.enabled || !other.gameObject.activeInHierarchy) return false;
        if (other.transform == transform || other.transform.IsChildOf(transform)) return false;
        if (owner != null && (other.transform == owner || other.transform.IsChildOf(owner))) return false;
        if (other.GetComponentInParent<Projectile>() != null) return false;
        if (other.GetComponentInParent<ZombieAI>() != null) return true;
        int layer = other.gameObject.layer;
        if (layer == LayerMask.NameToLayer("Target") || layer == LayerMask.NameToLayer("Enemy")) return true;
        // Camera zones and other non-combat triggers should not swallow bullets.
        return layer == LayerMask.NameToLayer("Default") && !other.isTrigger;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hit && CanHit(other)) ResolveHit(other, transform.position);
    }

    private void ResolveHit(Collider other, Vector3 position)
    {
        if (hit) return;
        hit = true;
        var zombie = other.GetComponentInParent<ZombieAI>();
        bool enemy = zombie != null || other.gameObject.layer == LayerMask.NameToLayer("Enemy");
        if (zombie != null) zombie.TakeDamage(damage);
        GameObject effect = enemy ? hitEnemyVFXPrefab :
            other.gameObject.layer == LayerMask.NameToLayer("Target") ? hitTargetVFXPrefab : null;
        if (effect != null) Destroy(Instantiate(effect, position, Quaternion.identity), 3f);
        if (debugHits) Debug.Log($"Bullet hit {other.name} (layer {LayerMask.LayerToName(other.gameObject.layer)}), enemy damage: {zombie != null}", other);
        foreach (var own in ownColliders) own.enabled = false;
        Destroy(gameObject);
    }
}
