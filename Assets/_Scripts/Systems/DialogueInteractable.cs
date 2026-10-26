using System.Collections.Generic;
using UnityEngine;

public sealed class DialogueInteractable : MonoBehaviour
{
    public static readonly HashSet<DialogueInteractable> Active = new HashSet<DialogueInteractable>();
    public DialogueSequence dialogue;
    public string prompt = "Talk";
    [Min(0.1f)] public float range = 2.5f;
    public bool playOnce;
    private bool consumed;

    public bool IsAvailable => isActiveAndEnabled && !consumed && dialogue != null;
    private void OnEnable() => Active.Add(this);
    private void OnDisable() => Active.Remove(this);

    private void Start()
    {
        foreach (var animation in GetComponentsInChildren<Animation>(true))
        {
            if (animation.clip == null || animation.clip.legacy || !animation.enabled || !animation.playAutomatically) continue;
            if (animation.GetComponent<NpcIdleAnimation>() == null)
                animation.gameObject.AddComponent<NpcIdleAnimation>();
        }
    }

    public bool Interact(DialogueSystem system)
    {
        if (!IsAvailable || !system.Play(dialogue)) return false;
        if (playOnce) consumed = true;
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
