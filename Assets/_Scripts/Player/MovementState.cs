using UnityEngine;

public class MovementState : MonoBehaviour
{
    private TankControls tank;

    public bool IsMovingForward { get; private set; }
    public bool IsMovingBackward { get; private set; }
    public bool IsTurningLeft { get; private set; }
    public bool IsTurningRight { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool IsQuickTurning { get; private set; }
    public bool IsQuickTurnOnCooldown { get; private set; }
    public bool CanMove { get; private set; } = true;
    public bool IsAiming { get; private set; }

    void Start()
    {
        tank = GetComponent<TankControls>();
    }

    void Update()
    {
        float v = tank.VerticalInput;
        float h = tank.HorizontalInput;

        IsQuickTurning = tank.IsQuickTurning;
        IsQuickTurnOnCooldown = tank.IsQuickTurnOnCooldown;
        IsAiming = tank.IsAiming;

        if (IsAiming)
        {
            IsMovingForward = false;
            IsMovingBackward = false;
            IsTurningLeft = false;
            IsTurningRight = false;
            IsSprinting = false;
        }
        else if (CanMove)
        {
            IsMovingForward = v > 0.1f;
            IsMovingBackward = v < -0.1f;
            IsTurningLeft  = h < -0.1f;
            IsTurningRight = h > 0.1f;
            IsSprinting = tank.IsSprinting;
        }
        else
        {
            IsMovingForward = false;
            IsMovingBackward = false;
            IsTurningLeft = false;
            IsTurningRight = false;
            IsSprinting = false;
        }
    }
}
