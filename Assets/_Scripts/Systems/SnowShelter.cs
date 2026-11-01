using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(ParticleSystem))]
public sealed class SnowShelter : MonoBehaviour
{
    public LayerMask shelterLayers = ~0;
    [Min(1)] public float roofCheckHeight = 100f;
    private ParticleSystem snow;
    private ParticleSystem.Particle[] particles;
    private HashSet<uint> previousSeeds = new HashSet<uint>();
    private HashSet<uint> currentSeeds = new HashSet<uint>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (var system in FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (system.gameObject.scene != scene || system.name != "snowEffect") continue;
            if (system.GetComponent<SnowShelter>() == null) system.gameObject.AddComponent<SnowShelter>();
        }
    }

    private void Awake()
    {
        snow = GetComponent<ParticleSystem>();
        foreach (string layer in new[] { "Player", "Enemy" })
        {
            int index = LayerMask.NameToLayer(layer);
            if (index >= 0) shelterLayers &= ~(1 << index);
        }
        var collision = snow.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;
        collision.mode = ParticleSystemCollisionMode.Collision3D;
        collision.quality = ParticleSystemCollisionQuality.High;
        collision.collidesWith = shelterLayers;
        collision.lifetimeLoss = 1f;
        collision.bounce = 0f;
        collision.sendCollisionMessages = false;
    }

    private void LateUpdate()
    {
        if (Time.timeScale == 0 || !snow.isPlaying) return;
        var main = snow.main;
        if (particles == null || particles.Length < main.maxParticles)
            particles = new ParticleSystem.Particle[main.maxParticles];
        int count = snow.GetParticles(particles);
        currentSeeds.Clear();
        bool changed = false;
        Transform space = main.simulationSpace == ParticleSystemSimulationSpace.Custom ? main.customSimulationSpace : transform;
        for (int i = 0; i < count; i++)
        {
            uint seed = particles[i].randomSeed;
            currentSeeds.Add(seed);
            // Only check newly emitted flakes; ordinary world collision handles their travel.
            if (previousSeeds.Contains(seed)) continue;
            Vector3 point = main.simulationSpace == ParticleSystemSimulationSpace.World ? particles[i].position :
                space != null ? space.TransformPoint(particles[i].position) : particles[i].position;
            // Cast downward onto roof tops, so single-sided roof meshes also work.
            if (!Physics.Raycast(point + Vector3.up * roofCheckHeight, Vector3.down,
                roofCheckHeight, shelterLayers, QueryTriggerInteraction.Ignore)) continue;
            particles[i].remainingLifetime = 0;
            changed = true;
        }
        if (changed) snow.SetParticles(particles, count);
        var swap = previousSeeds;
        previousSeeds = currentSeeds;
        currentSeeds = swap;
    }

    private void OnDisable() { previousSeeds.Clear(); currentSeeds.Clear(); }
}
