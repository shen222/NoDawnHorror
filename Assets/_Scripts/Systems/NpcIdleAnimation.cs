using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// Supports imported Generic clips assigned to the old Animation component.
public sealed class NpcIdleAnimation : MonoBehaviour
{
    public AnimationClip clip;
    private PlayableGraph graph;
    private Animation legacyAnimation;
    private Animator animator;
    private bool legacyWasEnabled, animatorWasEnabled, rootMotionWasEnabled;

    private void OnEnable()
    {
        legacyAnimation = GetComponent<Animation>();
        var idleClip = clip != null ? clip : legacyAnimation != null ? legacyAnimation.clip : null;
        if (idleClip == null || idleClip.legacy) return;
        animator = GetComponent<Animator>();
        // An authored Animator controller takes precedence over this fallback.
        if (animator != null && animator.runtimeAnimatorController != null) return;
        if (animator == null) animator = gameObject.AddComponent<Animator>();
        animatorWasEnabled = animator.enabled;
        rootMotionWasEnabled = animator.applyRootMotion;
        if (legacyAnimation != null)
        {
            legacyWasEnabled = legacyAnimation.enabled;
            legacyAnimation.enabled = false;
        }
        animator.enabled = true;
        animator.applyRootMotion = false;
        graph = PlayableGraph.Create("NPC Idle: " + name);
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        var playable = AnimationClipPlayable.Create(graph, idleClip);
        playable.SetApplyFootIK(false);
        var output = AnimationPlayableOutput.Create(graph, "Idle", animator);
        output.SetSourcePlayable(playable);
        graph.Play();
    }

    private void OnDisable()
    {
        if (!graph.IsValid()) return;
        graph.Destroy();
        if (legacyAnimation != null) legacyAnimation.enabled = legacyWasEnabled;
        if (animator != null)
        {
            animator.enabled = animatorWasEnabled;
            animator.applyRootMotion = rootMotionWasEnabled;
        }
    }
}
