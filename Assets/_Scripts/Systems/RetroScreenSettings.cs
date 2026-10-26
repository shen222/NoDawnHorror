using UnityEngine;

[CreateAssetMenu(menuName = "REimagined/Retro Screen Settings")]
public sealed class RetroScreenSettings : ScriptableObject
{
    public bool effectEnabled = true;
    [Range(120, 720)] public int verticalResolution = 240;
    [Range(8, 256)] public int colorLevels = 32;
    [Range(0, 1)] public float dithering = 0.35f;
    [Header("Silent Hill inspired atmosphere")]
    [Range(0, 1)] public float atmosphere = 1;
    [Range(0, 1)] public float saturation = 0.45f;
    public Color colorTint = new Color(0.91f, 0.95f, 0.88f, 1);
    public Color fogColor = new Color(0.48f, 0.51f, 0.48f, 1);
    [Min(0)] public float fogStart = 8;
    [Min(0.1f)] public float fogEnd = 32;
}
