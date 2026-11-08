using System;
using UnityEngine;

// Arm-only triangle selections from the original skinned meshes; the live skeleton animates them.
public sealed class FirstPersonArmMeshes : ScriptableObject
{
    [Serializable] public struct Part { public string rendererName; public Mesh mesh; }
    public Part[] parts;
}