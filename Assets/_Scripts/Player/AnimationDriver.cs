using UnityEngine;

public class AnimationDriver : MonoBehaviour
{
    public Animator animator;
    private MovementState movement;
    private TankControls tank;
    private Gun gun;

    void Start()
    {
        movement = GetComponent<MovementState>();
        tank = GetComponent<TankControls>();
        gun = tank.GetComponent<Gun>();
    }

    void Update()
    {
        if(movement.CanMove)
        // Normal movement animations
        animator.SetBool("QuickTurn", movement.IsQuickTurning);
        animator.SetBool("MovingForward", movement.IsMovingForward);
        animator.SetBool("MovingBackward", movement.IsMovingBackward);
        animator.SetBool("TurningLeft", movement.IsTurningLeft);
        animator.SetBool("TurningRight", movement.IsTurningRight);
        animator.SetBool("Sprinting", movement.IsSprinting);
        
        // Aiming animations
        animator.SetBool("IsAiming", movement.IsAiming);

        if (movement.IsAiming)
        {
            animator.SetFloat("AimAngle", tank.AimAngle);
            // AimAngle debug message
            animator.SetBool("MovingForward", false);
            animator.SetBool("MovingBackward", false);
            animator.SetBool("Sprinting", false);


        }
        if (movement.IsQuickTurning)
        {
            // Block all other anims during quick turn
            
            animator.SetBool("MovingForward", false);
            animator.SetBool("MovingBackward", false);
            animator.SetBool("TurningLeft", false);
            animator.SetBool("TurningRight", false);
            animator.SetBool("Sprinting", false);
            
        }
    }

    public void TriggerDamageAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("TakeDamage");
        }
    }

    public void TriggerDeathAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }
    }
}
