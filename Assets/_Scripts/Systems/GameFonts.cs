using TMPro;
using UnityEngine;

public static class GameFonts
{
    private static TMP_FontAsset ui, title;
    public static TMP_FontAsset UI => ui != null ? ui : (ui = Load("Fonts/Pixelta", JapaneseText.Font));
    public static TMP_FontAsset Title => title != null ? title : (title = Load("Fonts/PlaneCrash", UI));

    private static TMP_FontAsset Load(string path, TMP_FontAsset fallback)
    {
        var asset = Resources.Load<TMP_FontAsset>(path);
        if (asset != null) return asset;
        Debug.LogError("Missing game font asset: Resources/" + path + ". Run Tools/Fonts/Prepare Game Fonts.");
        return fallback;
    }
}