using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class GameFontSetup
{
    [MenuItem("Tools/Fonts/Prepare Game Fonts")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode before preparing fonts.");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Fonts")) AssetDatabase.CreateFolder("Assets/Resources", "Fonts");
        var japanese = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Resources/Japanese/JapaneseFont.asset");
        if (japanese == null) throw new InvalidOperationException("The existing Japanese fallback font is missing.");
        var jpSettings = new SerializedObject(japanese);
        jpSettings.FindProperty("m_ClearDynamicDataOnBuild").boolValue = true;
        jpSettings.ApplyModifiedPropertiesWithoutUndo();
        var pixelta = Prepare("Assets/Fonts/Pixelta.ttf", "Assets/Resources/Fonts/Pixelta.asset", "Pixelta UI", japanese);
        var title = Prepare("Assets/Fonts/Plane Crash.ttf", "Assets/Resources/Fonts/PlaneCrash.asset", "Plane Crash Title", pixelta);
        var settings = new SerializedObject(TMP_Settings.instance);
        settings.FindProperty("m_defaultFontAsset").objectReferenceValue = pixelta;
        settings.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(TMP_Settings.instance);
        AssetDatabase.SaveAssets();
        foreach (var asset in new[] { pixelta, title })
        {
            if (!AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(asset)).Any(a => a is Material))
                throw new InvalidOperationException("Font material was not saved: " + asset.name);
            if (!asset.HasCharacters(asset == title ? "no dawn" : "Play Settings Quit 12/48 Right-click to shoot"))
                throw new InvalidOperationException("Essential Latin glyphs are missing: " + asset.name);
        }
        if (!pixelta.HasCharacter('警', true, true)) throw new InvalidOperationException("Japanese fallback failed.");
        Directory.CreateDirectory("Logs/Fonts");
        File.WriteAllText("Logs/Fonts/setup-result.txt", "PASS: saved static Pixelta and Plane Crash TMP assets, atlases and materials; Pixelta is the TMP default, Plane Crash reserved for title. Essential Latin glyphs and Japanese fallback verified.\n" +
            "Pixelta baked characters: " + pixelta.characterTable.Count + "; Plane Crash baked characters: " + title.characterTable.Count + ".\n");
        Debug.Log("Game fonts ready: Plane Crash title; Pixelta UI with Japanese fallback.");
    }

    private static TMP_FontAsset Prepare(string sourcePath, string assetPath, string name, TMP_FontAsset fallback)
    {
        var source = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
        if (source == null) throw new InvalidOperationException("Font has not imported: " + sourcePath);
        var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (asset == null)
        {
            asset = TMP_FontAsset.CreateFontAsset(source, 80, 8, GlyphRenderMode.SDF16, 1024, 1024, AtlasPopulationMode.Dynamic, false);
            if (asset == null) throw new InvalidOperationException("Cannot create TMP font: " + sourcePath);
            asset.name = name;
            string characters = new string(Enumerable.Range(32, 95).Select(c => (char)c).ToArray()) + "−·…—“”‘’";
            asset.TryAddCharacters(characters, out string missing);
            // Unsupported symbols use the fallback; Latin glyphs stay baked and need no runtime atlas work.
            asset.atlasPopulationMode = AtlasPopulationMode.Static;
            asset.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };
            AssetDatabase.CreateAsset(asset, assetPath);
            asset.material.name = name + " Material";
            foreach (var atlas in asset.atlasTextures)
            {
                atlas.name = name + " Atlas";
                AssetDatabase.AddObjectToAsset(atlas, asset);
                EditorUtility.SetDirty(atlas);
            }
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            asset.material.mainTexture = asset.atlasTexture;
        }
        asset.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };
        EditorUtility.SetDirty(asset);
        EditorUtility.SetDirty(asset.material);
        return asset;
    }
}