using System.Collections;
using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("Gun Settings")]
    public float fireRate = 0.8f;
    [Tooltip("Minimum seconds between shots, including guns with older prefab firing speeds.")]
    [Min(0.01f)] public float minimumShotInterval = 0.8f;
    public int magSize = 12;
    public int reserveAmmo = 48;
    public float reloadTime = 1.2f;

    [Header("References")]
    public Transform firePoint;
    public GameObject shootVFXPrefabA;
    public GameObject shootVFXPrefabB;
    public GameObject projectilePrefab;
    public Transform magazineEjectPoint;
    public GameObject magazinePrefab;
    public Animator characterAnimator;
    public Animator gunAnimator;
    public GameObject shellPrefab;
    public Transform shellEjectPoint;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip[] gunshotClips;
    public AudioClip reloadClip;
    public AudioClip emptyClip;

    private float nextFireTime;
    private int currentAmmo;
    private bool isReloading = false;
    private HealthManager ownerHealth;

    public int CurrentAmmo => currentAmmo;
    public bool IsReloading => isReloading;

    void Start()
    {
        currentAmmo = magSize;
        ownerHealth = GetComponentInParent<HealthManager>();
        // The original gun prefab includes a visible green laser guide mesh.
        // Keep the muzzle transform for firing, but hide its guide in gameplay.
        if (firePoint != null)
        {
            var guide = firePoint.Find("Cylinder");
            if (guide != null)
            {
                var guideRenderer = guide.GetComponent<MeshRenderer>();
                if (guideRenderer != null && guideRenderer.sharedMaterial != null &&
                    guideRenderer.sharedMaterial.name == "Laser")
                    guideRenderer.enabled = false;
            }
        }
    }

    public void TryShoot()
    {
        if (Time.timeScale == 0f || isReloading || (ownerHealth != null && ownerHealth.IsDead)) return;
        if (projectilePrefab == null || firePoint == null) return;

        if (!CanShoot()) return;

        Shoot();
    }

    private bool CanShoot()
    {
        if (Time.time < nextFireTime)
            return false;

        if (currentAmmo <= 0)
        {
            PlayOutOfAmmoSFX();
            return false;
        }

        return true;
    }

    private void Shoot()
    {
        if (characterAnimator != null) characterAnimator.SetTrigger("Shoot");
        if (gunAnimator != null) gunAnimator.SetTrigger("Shoot");

        if (shellPrefab != null && shellEjectPoint != null)
            Destroy(Instantiate(shellPrefab, shellEjectPoint.position, shellEjectPoint.rotation), 20f);

        nextFireTime = Time.time + Mathf.Max(fireRate, minimumShotInterval);

        currentAmmo--;

        PlayShootSFX();

        Quaternion shotRotation = firePoint.rotation;
        var shoulderAim = GetComponentInParent<ShoulderAim>();
        if (shoulderAim != null && shoulderAim.IsActive)
        {
            Vector3 direction = shoulderAim.GetAimPoint() - firePoint.position;
            if (direction.sqrMagnitude > 0.0001f) shotRotation = Quaternion.LookRotation(direction);
        }
        var shot = Instantiate(projectilePrefab, firePoint.position, shotRotation);
        var projectile = shot.GetComponent<Projectile>();
        if (projectile != null)
        {
            var playerControls = GetComponentInParent<TankControls>();
            Transform owner = playerControls != null ? playerControls.transform :
                ownerHealth != null ? ownerHealth.transform : transform.root;
            projectile.Initialize(owner);
        }

        GameObject selectedVFX = Random.value > 0.5f ? shootVFXPrefabA : shootVFXPrefabB;

        if (selectedVFX != null && firePoint != null)
        {
            GameObject vfx = Instantiate(selectedVFX, firePoint.position, firePoint.rotation);

            // Randomly adjust the scale
            float randomScale = 1f + Random.Range(-0.1f, 0.1f);
            vfx.transform.localScale *= randomScale;

            Destroy(vfx, 2f);
        }
    }

    public void TryReload()
    {
        if (Time.timeScale == 0f || isReloading || (ownerHealth != null && ownerHealth.IsDead)) return;
        if (currentAmmo == magSize) return;
        if (reserveAmmo <= 0) return;

        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        if (characterAnimator != null) characterAnimator.SetTrigger("Reload");
        if (gunAnimator != null) gunAnimator.SetTrigger("Reload");
        isReloading = true;

        if (magazinePrefab != null && magazineEjectPoint != null)
            Destroy(Instantiate(magazinePrefab, magazineEjectPoint.position, magazineEjectPoint.rotation), 20f);

        PlayReloadSFX();

        yield return new WaitForSeconds(reloadTime);

        if (ownerHealth != null && ownerHealth.IsDead)
        {
            isReloading = false;
            yield break;
        }

        int needed = magSize - currentAmmo;
        int ammoToLoad = Mathf.Min(needed, reserveAmmo);

        currentAmmo += ammoToLoad;
        reserveAmmo -= ammoToLoad;

        isReloading = false;
    }

    public void AddReserveAmmo(int amount)
    {
        reserveAmmo += amount;
    }

    private void PlayShootSFX()
    {
        if (audioSource == null || gunshotClips == null || gunshotClips.Length == 0)
            return;

        AudioClip clip = gunshotClips[Random.Range(0, gunshotClips.Length)];

        audioSource.pitch = Random.Range(0.98f, 1.02f);
        audioSource.PlayOneShot(clip);
    }

    private void PlayReloadSFX()
    {
        if (audioSource == null || reloadClip == null)
            return;

        audioSource.pitch = 1f;
        audioSource.PlayOneShot(reloadClip);
    }

    private void PlayOutOfAmmoSFX()
    {
        if (audioSource == null || emptyClip == null)
            return;

        audioSource.pitch = 1f;
        audioSource.PlayOneShot(emptyClip);
    }
}
