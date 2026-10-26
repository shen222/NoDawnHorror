using TMPro;
using UnityEngine;

public static class JapaneseText
{
    private static TMP_FontAsset font;
    public static TMP_FontAsset Font
    {
        get
        {
            if (font != null) return font;
            font = Resources.Load<TMP_FontAsset>("Japanese/JapaneseFont");
            if (font != null) return font;
            var source = Resources.Load<Font>("Japanese/NotoSansJP");
            if (source == null)
            {
                Debug.LogError("Japanese font is missing: Resources/Japanese/NotoSansJP.ttf");
                return TMP_Settings.defaultFontAsset;
            }
            font = TMP_FontAsset.CreateFontAsset(source, 48, 5, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
            font.name = "Japanese Noto Sans JP";
            font.isMultiAtlasTexturesEnabled = true;
            return font;
        }
    }

    public static string Prompt(string value)
    {
        if (value == "Talk") return "話す";
        if (value == "Inspect") return "調べる";
        return value;
    }
}
