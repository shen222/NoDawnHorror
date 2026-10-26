using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DialogueSetup
{
    private const string Folder = "Assets/Resources/Dialogue";

    [MenuItem("Tools/Dialogue/Set Up Village Dialogue")]
    public static void Setup()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before setting up dialogue.");
        EnsureFolder("Assets", "Resources");
        EnsureFolder("Assets/Resources", "Dialogue");
        var arrival = Sequence("Arrival", false, true,
            new DialogueSequence.Line { text = "どうしてこんなに霧が濃いの？ 何も見えない。", duration = 5f });
        var conversation = Sequence("Villager", true, false,
            new DialogueSequence.Line { speaker = "警官", text = "こんなところで、小さな女の子が何をしているの？" },
            new DialogueSequence.Line { speaker = "少女", text = "お父さんとお母さんを待ってるの。" });
        Sequence("Inspect", false, true,
            new DialogueSequence.Line { text = "静かすぎる……みんなどこにいるの？", duration = 4f });

        var scene = SceneManager.GetActiveScene();
        var player = UnityEngine.Object.FindFirstObjectByType<TankControls>();
        if (player == null) throw new InvalidOperationException("Open MainGame, then run the dialogue setup.");
        var system = UnityEngine.Object.FindFirstObjectByType<DialogueSystem>();
        if (system == null) system = new GameObject("DialogueSystem").AddComponent<DialogueSystem>();
        Undo.RecordObject(system, "Set up dialogue");
        system.player = player;
        system.arrival = arrival;
        EditorUtility.SetDirty(system);

        // This is deliberately a replaceable placeholder, not a new character art asset.
        if (GameObject.Find("Village NPC (Placeholder)") == null)
        {
            var npc = new GameObject("Village NPC (Placeholder)");
            Undo.RegisterCreatedObjectUndo(npc, "Create village NPC");
            npc.transform.position = player.transform.position + player.transform.forward * 4f;
            npc.transform.rotation = Quaternion.LookRotation(-player.transform.forward, Vector3.up);
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Replace with civilian model";
            body.transform.SetParent(npc.transform, false);
            body.transform.localPosition = Vector3.up * 0.9f;
            body.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
            var talk = npc.AddComponent<DialogueInteractable>();
            talk.dialogue = conversation;
            talk.prompt = "Talk";
            talk.range = 3f;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Village dialogue ready. Save the scene. Select the NPC to move it or replace its capsule model.");
    }

    public static void SetupMainGameBatch()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Prototyping/MainGame.unity");
        Setup();
        EditorSceneManager.SaveScene(scene);
        var arrival = AssetDatabase.LoadAssetAtPath<DialogueSequence>(Folder + "/Arrival.asset");
        var villager = GameObject.Find("Village NPC (Placeholder)").GetComponent<DialogueInteractable>();
        if (arrival.lines.Length != 1 || villager.dialogue.lines.Length != 2)
            throw new InvalidOperationException("Dialogue content verification failed.");
        Debug.Log("DIALOGUE_SETUP_PASS: arrival, conversation, player binding and NPC saved in MainGame.");
    }

    private static DialogueSequence Sequence(string name, bool lockPlayer, bool autoAdvance, params DialogueSequence.Line[] lines)
    {
        string path = Folder + "/" + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<DialogueSequence>(path);
        if (existing != null) return existing;
        var asset = ScriptableObject.CreateInstance<DialogueSequence>();
        asset.lockPlayer = lockPlayer;
        asset.autoAdvance = autoAdvance;
        asset.lines = lines;
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
    }
}
