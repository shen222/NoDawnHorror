using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class RetroScreenSetup
{
    static RetroScreenSetup() { EditorApplication.delayCall += EnsureSettings; }

    private static void EnsureSettings()
    {
        const string path = "Assets/Resources/RetroScreenSettings.asset";
        if (AssetDatabase.LoadAssetAtPath<RetroScreenSettings>(path) != null) return;
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<RetroScreenSettings>(), path);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/REimagined/PS1 Screen Settings")]
    public static void SelectSettings()
    {
        EnsureSettings();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<RetroScreenSettings>("Assets/Resources/RetroScreenSettings.asset");
    }
}
