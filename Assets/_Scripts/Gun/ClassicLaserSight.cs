using System;
using UnityEngine;
using UnityEngine.Rendering;

// Added by Gun for existing weapons, including already placed prefab instances.
[DefaultExecutionOrder(12000), DisallowMultipleComponent, RequireComponent(typeof(Gun))]
public sealed class ClassicLaserSight : MonoBehaviour
{
    [Min(.1f)] public float maximumDistance = 30f;
    [Min(.001f)] public float beamWidth = .015f;
    [Min(.001f)] public float dotDiameter = .045f;
    public Color laserColor = new Color(1f, .04f, .025f, .9f);
    private Gun gun;
    private TankControls player;
    private HealthManager health;
    private LineRenderer beam;
    private MeshRenderer dot;
    private Material material;
    private RaycastHit[] hits = new RaycastHit[32];

    private void Awake()
    {
        gun = GetComponent<Gun>();
        player = GetComponentInParent<TankControls>();
        health = GetComponentInParent<HealthManager>();
    }

    private void LateUpdate()
    {
        bool visible = gun != null && gun.isActiveAndEnabled && gun.firePoint != null &&
            player != null && player.isActiveAndEnabled && player.IsAiming &&
            player.CurrentShootingMode == ShootingMode.Classic && !gun.IsReloading &&
            (health == null || !health.IsDead) && Time.timeScale > 0 &&
            !DialogueSystem.BlocksPlayer && !StoryCameraDirector.IsPlaying;
        if (!visible) { Hide(); return; }
        if (beam == null && !CreateVisuals()) return;

        Vector3 start = gun.firePoint.position;
        Vector3 direction = gun.firePoint.forward;
        float range = Mathf.Max(.1f, maximumDistance);
        int count;
        while ((count = Physics.RaycastNonAlloc(start, direction, hits, range, ~0, QueryTriggerInteraction.Collide)) == hits.Length)
            Array.Resize(ref hits, hits.Length * 2);
        int nearest = -1;
        float distance = range;
        for (int i = 0; i < count; i++)
        {
            Collider other = hits[i].collider;
            if (other.transform == player.transform || other.transform.IsChildOf(player.transform) ||
                other.GetComponentInParent<Projectile>() != null) continue;
            int layer = other.gameObject.layer;
            if (other.isTrigger && other.GetComponentInParent<ZombieAI>() == null &&
                layer != LayerMask.NameToLayer("Enemy") && layer != LayerMask.NameToLayer("Target")) continue;
            if (hits[i].distance >= distance) continue;
            nearest = i;
            distance = hits[i].distance;
        }

        beam.startWidth = beam.endWidth = Mathf.Max(.001f, beamWidth);
        beam.startColor = beam.endColor = laserColor;
        material.SetColor("_Color", laserColor);
        beam.SetPosition(0, start);
        beam.SetPosition(1, start + direction * distance);
        beam.enabled = true;
        dot.enabled = nearest >= 0;
        if (nearest >= 0)
        {
            dot.transform.position = hits[nearest].point + hits[nearest].normal * .01f;
            // World scale is independent of the gun/model's authored scale.
            dot.transform.localScale = Vector3.one * Mathf.Max(.001f, dotDiameter);
        }
    }

    private bool CreateVisuals()
    {
        var shader = Resources.Load<Shader>("ClassicLaserSight");
        if (shader == null) return false;
        material = new Material(shader) { name = "Classic laser (runtime)", hideFlags = HideFlags.DontSave };
        var root = new GameObject("Classic Laser Beam");
        // Keep uniform world units even when the weapon has a nonuniform model scale.
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, gameObject.scene);
        beam = root.AddComponent<LineRenderer>();
        beam.useWorldSpace = true;
        beam.positionCount = 2;
        beam.alignment = LineAlignment.View;
        beam.numCapVertices = 4;
        beam.sharedMaterial = material;
        beam.shadowCastingMode = ShadowCastingMode.Off;
        beam.receiveShadows = false;
        beam.enabled = false;
        var dotObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dotObject.name = "Classic Laser Impact";
        dotObject.transform.SetParent(root.transform, false);
        var collider = dotObject.GetComponent<Collider>();
        collider.enabled = false;
        Destroy(collider);
        dot = dotObject.GetComponent<MeshRenderer>();
        dot.sharedMaterial = material;
        dot.shadowCastingMode = ShadowCastingMode.Off;
        dot.receiveShadows = false;
        dot.enabled = false;
        return true;
    }

    private void Hide()
    {
        if (beam != null) beam.enabled = false;
        if (dot != null) dot.enabled = false;
    }

    private void OnDisable() => Hide();
    private void OnDestroy()
    {
        if (beam != null) Destroy(beam.gameObject);
        if (material != null) Destroy(material);
    }
}
