using UnityEngine;
using System.Collections;
using Unity.VisualScripting;

public class TankControls : MonoBehaviour
{
    private CharacterController controller;
    private MovementState movementState;
    private AnimationDriver animationDriver;
    private HealthManager health;
    private ShoulderAim shoulderAim;

    [SerializeField] private Gun gun;
    public Gun EquippedGun => gun;

    [Header("Movement Settings")]
    public float speed = 10f;
    public float turnSpeed = 100f;
    public float sprintMultiplier = 1.5f;
    public float backwardMultiplier = 0.6f;
    public AudioSource audioSource;
    public AudioClip[] footstepClips;

    [Header("Quick Turn Settings")]
    public float quickTurnTime = 0.25f;
    public float quickTurnCooldown = 0.5f;

    public bool IsQuickTurning { get; private set; }
    public bool IsQuickTurnOnCooldown => quickTurnCooldownTimer > 0f;

    public float VerticalInput { get; private set; }
    public float HorizontalInput { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool IsAiming { get; private set; }
    public float AimVerticalInput { get; private set; }
    public float AimAngle { get; private set; } = 0f;
    public float aimAngleSpeed = 50f;

    private float quickTurnCooldownTimer = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        movementState = GetComponent<MovementState>();
        animationDriver = GetComponent<AnimationDriver>();
        health = GetComponent<HealthManager>();
        shoulderAim = GetComponent<ShoulderAim>();
        if (shoulderAim == null) shoulderAim = gameObject.AddComponent<ShoulderAim>();
        if (gun == null) gun = GetComponentInChildren<Gun>();
        if (FindFirstObjectByType<ExplorationCameraRig>() == null)
        {
            var rig = new GameObject("Exploration Camera Rig").AddComponent<ExplorationCameraRig>();
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(rig.gameObject, gameObject.scene);
            rig.player = this;
        }
    }

    void Update()
    {
        if (Time.timeScale == 0f || DialogueSystem.BlocksPlayer || StoryCameraDirector.BlocksPlayer || (health != null && health.IsDead))
        {
            VerticalInput = HorizontalInput = AimVerticalInput = 0f;
            IsSprinting = IsAiming = false;
            shoulderAim.EndAim();
            return;
        }
        HandleInput();
        HandleQuickTurn();
        HandleMovement();
        HandleAiming();
    }

    private void HandleInput()
    {
        VerticalInput = Input.GetAxis("Vertical");
        HorizontalInput = Input.GetAxis("Horizontal");
        IsSprinting = Input.GetKey(KeyCode.LeftShift) && VerticalInput > 0.1f;

        if (StoryCameraDirector.IsPlaying) IsAiming = false;
        else if (Input.GetKeyDown(KeyCode.Space) && !IsQuickTurning)
            IsAiming = !IsAiming;

        AimVerticalInput = 0f;
        if (IsAiming)
        {
            VerticalInput = HorizontalInput = 0f;
            IsSprinting = false;
        }
    }

    private void HandleQuickTurn()
    {
        // Reduce cooldown timer
        if (quickTurnCooldownTimer > 0f)
            quickTurnCooldownTimer -= Time.deltaTime;

        // Quick Turn input — only if allowed
        if (!IsQuickTurning && !IsAiming && Input.GetKeyDown(KeyCode.Q) && quickTurnCooldownTimer <= 0f)
        {
            StartCoroutine(QuickTurnCoroutine());
        }
    }

    private void HandleMovement()
    {
        if (IsQuickTurning) return;
        if (IsAiming)
        {
            // Mouse controls facing; aiming never translates the player.
            return;
        }
        else if (movementState.CanMove)
        {
            // Handle rotation
            transform.Rotate(0, HorizontalInput * turnSpeed * Time.deltaTime, 0);

            // Determine movement speed
            float currentSpeed = CalculateMovementSpeed();

            // Apply movement
            Vector3 moveDir = transform.forward * VerticalInput * currentSpeed;
            controller.Move(moveDir * Time.deltaTime - Vector3.up * 0.1f); // Gravity adjustment
        }
    }

    private void HandleAiming()
    {
        if (IsAiming)
        {
            if (!shoulderAim.UpdateAim()) { IsAiming = false; return; }
            AimAngle = -shoulderAim.Pitch;

            if (gun != null && (Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0)))
            {
                gun.TryShoot();
            }

            if (gun != null && (Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.R)))
            {
                gun.TryReload();

            }

        }
        else shoulderAim.EndAim();
    }

    private void OnDisable()
    {
        IsAiming = false;
        if (shoulderAim != null) shoulderAim.EndAim();
    }

    private float CalculateMovementSpeed()
    {
        if (VerticalInput > 0.1f)
            return IsSprinting ? speed * sprintMultiplier : speed;
        else if (VerticalInput < -0.1f)
            return speed * backwardMultiplier;

        return 0f;
    }

    public void PlayFootstep()
    {
        if (audioSource == null || footstepClips == null || footstepClips.Length == 0) return;

        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        audioSource.PlayOneShot(clip);
    }

    private IEnumerator QuickTurnCoroutine()
    {
        IsQuickTurning = true;

        Quaternion startRot = transform.rotation;
        Quaternion endRot = transform.rotation * Quaternion.Euler(0f, 180f, 0f);

        float elapsed = 0f;

        while (elapsed < quickTurnTime && (health == null || !health.IsDead))
        {
            if (StoryCameraDirector.BlocksPlayer || DialogueSystem.BlocksPlayer) { yield return null; continue; }
            float t = elapsed / quickTurnTime;
            transform.rotation = Quaternion.Slerp(startRot, endRot, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (health == null || !health.IsDead) transform.rotation = endRot;

        // Start cooldown
        quickTurnCooldownTimer = quickTurnCooldown;
        IsQuickTurning = false;
    }
}
