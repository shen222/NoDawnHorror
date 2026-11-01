using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class OpeningSequenceValidation
{
    [InitializeOnLoadMethod]
    private static void Schedule() => EditorApplication.delayCall += ApplyRequested;
    private static void ApplyRequested()
    {
        const string request = "Logs/Opening/validate.request";
        if (!File.Exists(request)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        { EditorApplication.delayCall += ApplyRequested; return; }
        File.Delete(request);
        try
        {
            Validate();
            File.WriteAllText("Logs/Opening/validation.txt", "PASS: arrival, dispatch interaction, girl gating, cancellation, conversation completion, and independence of the original body reveal.");
        }
        catch (Exception error) { File.WriteAllText("Logs/Opening/validation.txt", error.ToString()); Debug.LogException(error); }
    }

    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    public static void RunBatch()
    {
        try
        {
            Validate();
            Directory.CreateDirectory("Logs/Opening");
            File.WriteAllText("Logs/Opening/validation.txt", "PASS: arrival, dispatch interaction, girl gating, cancellation, conversation completion, and independence of the original body reveal.");
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Directory.CreateDirectory("Logs/Opening");
            File.WriteAllText("Logs/Opening/validation.txt", error.ToString());
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }

    [MenuItem("Tools/Story/Validate Opening In Empty Test Scene")]
    public static void Validate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode before validation.");
        var original = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        float timeScale = Time.timeScale;
        var testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(testScene);
        try { ValidateFlow(); }
        finally
        {
            Time.timeScale = timeScale;
            EditorSceneManager.CloseScene(testScene, true);
            if (original.IsValid()) UnityEngine.SceneManagement.SceneManager.SetActiveScene(original);
            typeof(DialogueSystem).GetMethod("ResetStatics", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        }
    }

    private static void ValidateFlow()
    {


        typeof(DialogueSystem).GetMethod("ResetStatics", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        var player = new GameObject("Opening Test Player").AddComponent<TankControls>();
        var car = new GameObject("Car");
        var girl = new GameObject("Village NPC (Placeholder)").AddComponent<DialogueInteractable>();
        girl.dialogue = ScriptableObject.CreateInstance<DialogueSequence>();
        girl.dialogue.lines = new[] { new DialogueSequence.Line { text = "Question" }, new DialogueSequence.Line { text = "Answer" } };
        var body = new GameObject("Deadman Discovery").AddComponent<StoryFocusShot>();
        body.player = player;
        body.triggerByDistance = true;
        body.subject = new GameObject("Test Body").transform;
        body.cameraAnchor = new GameObject("Test Reveal Camera").transform;
        var alleyRoot = new GameObject("TriggerZone alleyway");
        var box = alleyRoot.AddComponent<BoxCollider>();
        box.size = new Vector3(5, 4, 5);
        box.isTrigger = true;
        var alley = alleyRoot.AddComponent<CamSwitcher>();
        alley.mode = CamSwitcher.CameraMode.Follow;
        var dialogue = new GameObject("Test Dialogue").AddComponent<DialogueSystem>();
        dialogue.player = player;
        if (Get(dialogue, "subtitle") == null) Call(dialogue, "Awake");
        var opening = new GameObject("Test Opening").AddComponent<OpeningSequence>();
        Call(opening, "Start");
        Check(!girl.enabled && body.playOnEnter, "The original body reveal must remain independent of the opening");
        Set(opening, "elapsed", 2f);
        Call(opening, "Update");
        Check(dialogue.IsPlaying, "Arrival should play");
        dialogue.Stop();
        Check(opening.CurrentBeat == OpeningSequence.Beat.Arrival, "Cancelled text must not advance the story");
        Set(opening, "elapsed", 2f);
        Call(opening, "Update");
        dialogue.Advance();
        Check(opening.CurrentBeat == OpeningSequence.Beat.Arrival, "Finish all arrival lines first");
        dialogue.Advance();
        var radio = car.GetComponent<DialogueInteractable>();
        Check(opening.CurrentBeat == OpeningSequence.Beat.Dispatch && radio.enabled, "Dispatch must unlock after arrival");
        Check(!girl.Interact(dialogue), "Girl interaction must stay gated");
        Check(radio.Interact(dialogue), "Dispatch radio should accept interaction");
        dialogue.Advance();
        dialogue.Advance();
        Check(opening.CurrentBeat == OpeningSequence.Beat.Girl && girl.enabled && !radio.enabled, "Dispatch should unlock the girl exactly once");
        Check(girl.Interact(dialogue), "Girl conversation should start");
        dialogue.Advance();
        Check(opening.CurrentBeat == OpeningSequence.Beat.Girl, "The full exchange must finish");
        dialogue.Advance();
        Check(opening.CurrentBeat == OpeningSequence.Beat.Complete && body.playOnEnter, "The intro must finish after the girl without gating the original body reveal");
        Debug.Log("OPENING_VALIDATION_PASS");
    }

    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static object Get(object target, string field) => target.GetType().GetField(field, Hidden).GetValue(target);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Hidden).SetValue(target, value);
    private static void Call(object target, string method) => target.GetType().GetMethod(method, Hidden).Invoke(target, null);
}





