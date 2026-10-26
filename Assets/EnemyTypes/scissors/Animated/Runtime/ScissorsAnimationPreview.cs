using UnityEngine;

[RequireComponent(typeof(Animator))]
public sealed class ScissorsAnimationPreview : MonoBehaviour
{
    [Tooltip("Turn off when your enemy AI controls Pursue and Attack.")]
    public bool previewWalkAndAttack = true;
    private Animator animator;
    private float cycle;
    private bool attacking;

    private void Awake() { animator = GetComponent<Animator>(); }
    private void Update()
    {
        if (!previewWalkAndAttack) return;
        cycle += Time.deltaTime;
        if (!attacking)
        {
            animator.SetBool("Pursue", true);
            if (cycle >= 3f)
            {
                animator.SetBool("Pursue", false);
                animator.SetTrigger("Attack");
                cycle = 0f;
                attacking = true;
            }
        }
        else if (cycle >= 1.65f)
        {
            cycle = 0f;
            attacking = false;
        }
    }
    public void SetWalking(bool walking) { animator.SetBool("Pursue", walking); }
    public void Attack() { animator.SetTrigger("Attack"); }
}
