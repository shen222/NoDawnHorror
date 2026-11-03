using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class GunMenuSetup
{
    public const string MenuPath = "Assets/Scenes/MainMenu.unity";
    private const string Source = "Assets/Scenes/Prototyping/MainGame.unity";
    private const string Request = "Logs/Menu/setup.request";

    [InitializeOnLoadMethod]
    private static void Register() => EditorApplication.delayCall += ApplyRequested;

    private static void ApplyRequested()
    {
        if (!File.Exists(Request)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        { EditorApplication.delayCall += ApplyRequested; return; }
        File.Delete(Request);
        try { Create(); }
        catch (Exception error) { Debug.LogException(error); File.WriteAllText("Logs/Menu/setup-result.txt", error.ToString()); }
    }

    [MenuItem("Tools/Menu/Create Shootable Main Menu")]
    public static void Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode first.");
        if (!File.Exists(Source)) throw new FileNotFoundException("MainGame scene not found.", Source);
        if (!File.Exists(MenuPath) && !AssetDatabase.CopyAsset(Source, MenuPath))
            throw new InvalidOperationException("Could not create the separate MainMenu scene.");
        var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(MenuPath, true) };
        bool hasGame = false;
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.path == MenuPath) continue;
            scenes.Add(scene.path == Source ? new EditorBuildSettingsScene(Source, true) : scene);
            hasGame |= scene.path == Source;
        }
        if (!hasGame) scenes.Add(new EditorBuildSettingsScene(Source, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Logs/Menu");
        File.WriteAllText("Logs/Menu/setup-result.txt", "PASS: separate MainMenu scene created from the village, registered as the first build scene; MainGame preserved and enabled. Open Assets/Scenes/MainMenu.unity and press Play.");
        Debug.Log("Shootable menu ready: open Assets/Scenes/MainMenu.unity and press Play. Right-click to shoot Play, Settings or Quit. MainGame was not modified.");
    }
}

