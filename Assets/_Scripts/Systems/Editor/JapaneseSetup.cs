using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class JapaneseSetup
{
    private const string Request = "Logs/JapaneseSetup.request";

    [InitializeOnLoadMethod]
    private static void Schedule() => EditorApplication.delayCall += ApplyRequested;

    private static void ApplyRequested()
    {
        if (!File.Exists(Request)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += ApplyRequested;
            return;
        }
        Apply();
        File.Delete(Request);
    }

    [MenuItem("Tools/Dialogue/Apply Japanese Text")]
    public static void Apply()
    {
        SetLines("Villager", new DialogueSequence.Line { speaker = "警官", text = "こんなところで、小さな女の子が何をしているの？" },
            new DialogueSequence.Line { speaker = "少女", text = "お父さんとお母さんを待ってるの。" });
        SetLines("Arrival", new DialogueSequence.Line { text = "どうしてこんなに霧が濃いの？ 何も見えない。", duration = 5 });
        SetLines("Inspect", new DialogueSequence.Line { text = "静かすぎる……みんなどこにいるの？", duration = 4 });
        const string fontPath = "Assets/Resources/Japanese/JapaneseFont.asset";
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
        if (font == null)
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Japanese/NotoSansJP.ttf");
            if (source == null) throw new InvalidOperationException("Wait for the Japanese font to finish importing.");
            font = TMP_FontAsset.CreateFontAsset(source, 48, 5, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
            font.name = "Japanese Noto Sans JP";
            font.isMultiAtlasTexturesEnabled = true;
            AssetDatabase.CreateAsset(font, fontPath);
            foreach (var atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, font);
            AssetDatabase.AddObjectToAsset(font.material, font);
            font.material.mainTexture = font.atlasTexture;
        }
        string corpus = "警官こんなところで、小さな女の子が何をしているの？少女お父さんとお母さんを待ってるの。どうしてこんなに霧が濃いの？何も見えない。静かすぎる……みんなどこにいるの？話す調べる次へ移動旋回走る振り向く構えるマウス照準左クリック射撃装填操作表示やり直す死亡しました一時停止再開中弾切れ武器なし体力で「」、または—…0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz /";
        if (!font.TryAddCharacters(corpus, out string missing))
            throw new InvalidOperationException("Japanese font is missing characters: " + missing);
        foreach (var atlas in font.atlasTextures)
        {
            if (!AssetDatabase.Contains(atlas)) AssetDatabase.AddObjectToAsset(atlas, font);
            EditorUtility.SetDirty(atlas);
        }
        EditorUtility.SetDirty(font);
        EditorUtility.SetDirty(font.material);
        AssetDatabase.SaveAssets();
        if (!AssetDatabase.LoadAllAssetsAtPath(fontPath).Any(asset => asset is Material))
            throw new InvalidOperationException("Japanese font material was not saved.");
        string report = "Japanese text applied: two-line girl conversation, arrival, inspection, HUD and prompts. Japanese font glyph coverage and saved material verified.";
        File.WriteAllText("Logs/JapaneseSetup.txt", report);
        Debug.Log(report);
    }

    private static void SetLines(string name, params DialogueSequence.Line[] lines)
    {
        var sequence = AssetDatabase.LoadAssetAtPath<DialogueSequence>("Assets/Resources/Dialogue/" + name + ".asset");
        if (sequence == null) throw new InvalidOperationException("Missing dialogue sequence: " + name);
        Undo.RecordObject(sequence, "Translate dialogue to Japanese");
        sequence.lines = lines;
        EditorUtility.SetDirty(sequence);
    }
}
