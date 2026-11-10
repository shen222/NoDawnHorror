using System.Collections.Generic;
using UnityEngine;

// Keys belong to this player and reset when the scene/player is recreated.
public sealed class KeyInventory : MonoBehaviour
{
    private readonly HashSet<string> keys = new HashSet<string>();
    public bool HasKey(string id) => !string.IsNullOrWhiteSpace(id) && keys.Contains(id);
    public bool TryUseKey(string id) => !string.IsNullOrWhiteSpace(id) && keys.Remove(id);
    public void AddKey(string id)
    {
        if (!string.IsNullOrWhiteSpace(id)) keys.Add(id);
    }
}
