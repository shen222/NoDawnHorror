using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Sequence")]
public sealed class DialogueSequence : ScriptableObject
{
    [System.Serializable]
    public struct Line
    {
        public string speaker;
        [TextArea(2, 5)] public string text;
        [Min(0)] public float duration;
    }

    public bool lockPlayer = true;
    public bool autoAdvance;
    public Line[] lines = new Line[0];
}
