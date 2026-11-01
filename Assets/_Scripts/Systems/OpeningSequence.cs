using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-250)]
public sealed class OpeningSequence : MonoBehaviour
{
    public enum Beat { Arrival, Dispatch, Girl, Complete }
    public Beat CurrentBeat { get; private set; }
    private TankControls player;
    private HealthManager health;
    private DialogueSystem dialogue;
    private DialogueInteractable girl, radio;


    private DialogueSequence arrival, dispatch, girlDialogue;
    private readonly List<DialogueSequence> owned = new List<DialogueSequence>();
    private bool ready;
    private float elapsed;



    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        SceneManager.sceneLoaded += SceneLoaded;
    }

    private static void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "MainGame") return;
        foreach (var opening in FindObjectsByType<OpeningSequence>(FindObjectsSortMode.None))
            if (opening.gameObject.scene == scene) return;
        var root = new GameObject("Village Opening");
        SceneManager.MoveGameObjectToScene(root, scene);
        root.AddComponent<OpeningSequence>();
    }

    private void Start()
    {
        var car = FindInScene("Car");
        var girlRoot = FindInScene("Village NPC (Placeholder)");
        player = FindComponentInScene<TankControls>();
        dialogue = FindComponentInScene<DialogueSystem>();
        girl = girlRoot != null ? girlRoot.GetComponent<DialogueInteractable>() : null;
        if (car == null || player == null || dialogue == null || girl == null || !DialogueSystem.HasValidLines(girl.dialogue))
        {
            Debug.LogError("Opening sequence needs Car and the village girl's dialogue in MainGame.", this);
            enabled = false;
            return;
        }
        health = player.GetComponent<HealthManager>();
        girlDialogue = girl.dialogue;
        arrival = Sequence("Opening arrival", false, true,
            Line("", "どうしてこんなに霧が濃いの？ 何も見えない。", 5),
            Line("", "車の無線で、通報の内容を確認しよう。", 4));
        dispatch = Sequence("Opening dispatch", true, false,
            Line("", "この村で人が襲われたって通報があった。", 4),
            Line("", "まずは、誰かに話を聞こう。", 3));
        dialogue.SuppressArrival();
        dialogue.Completed += DialogueCompleted;
        radio = car.GetComponent<DialogueInteractable>();
        if (radio == null) radio = car.AddComponent<DialogueInteractable>();
        radio.dialogue = dispatch;
        radio.prompt = "無線を聞く";
        radio.range = 4.5f;
        radio.playOnce = true;
        radio.enabled = false;
        girl.enabled = false;
        PositionPlayerByCar(car);
        CurrentBeat = Beat.Arrival;
        ready = true;
    }

    private T FindComponentInScene<T>() where T : Component
    {
        foreach (var root in gameObject.scene.GetRootGameObjects())
        {
            var match = root.GetComponentInChildren<T>();
            if (match != null) return match;
        }
        return null;
    }
    private GameObject FindInScene(string objectName)
    {
        foreach (var root in gameObject.scene.GetRootGameObjects())
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == objectName) return child.gameObject;
        return null;
    }

    private void PositionPlayerByCar(GameObject car)
    {
        var renderers = car.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        Bounds bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        // Preserve an authored spawn already beside the car.
        if (Vector3.Distance(bounds.ClosestPoint(player.transform.position), player.transform.position) <= 3) return;
        Vector3 direction = player.transform.position - bounds.center;
        direction.y = 0;
        if (direction.sqrMagnitude < 0.1f) direction = Vector3.right;
        direction.Normalize();
        Vector3 candidate = bounds.center + direction * (Mathf.Max(bounds.extents.x, bounds.extents.z) + 1.5f);
        candidate.y = player.transform.position.y;
        if (!NavMesh.SamplePosition(candidate, out var hit, 3f, NavMesh.AllAreas))
        {
            Debug.LogWarning("Opening retained the authored player spawn: no walkable point was found beside the car.", this);
            return;
        }
        if (bounds.Contains(hit.position + Vector3.up * 0.5f)) return;
        var controller = player.GetComponent<CharacterController>();
        bool controllerEnabled = controller != null && controller.enabled;
        if (controllerEnabled) controller.enabled = false;
        player.transform.position = hit.position;
        Vector3 facing = car.transform.position - hit.position;
        facing.y = 0;
        if (facing.sqrMagnitude > 0.01f) player.transform.rotation = Quaternion.LookRotation(facing);
        if (controllerEnabled) controller.enabled = true;
    }

    private void Update()
    {
        if (!ready || player == null || dialogue == null || Time.timeScale == 0 || (health != null && health.IsDead)) return;
        if (CurrentBeat != Beat.Arrival || dialogue.IsPlaying || StoryCameraDirector.IsPlaying) return;
        elapsed += Time.deltaTime;
        if (elapsed >= 1.5f && !player.IsQuickTurning && dialogue.Play(arrival))
            elapsed = float.NegativeInfinity;
    }
    private void DialogueCompleted(DialogueSequence completed)
    {
        if (completed == arrival && CurrentBeat == Beat.Arrival)
        {
            CurrentBeat = Beat.Dispatch;
            radio.enabled = true;
        }
        else if (completed == dispatch && CurrentBeat == Beat.Dispatch)
        {
            CurrentBeat = Beat.Girl;
            radio.enabled = false;
            girl.enabled = true;
        }
        else if (completed == girlDialogue && CurrentBeat == Beat.Girl)
        {
            CurrentBeat = Beat.Complete;
        }
    }

    private DialogueSequence Sequence(string name, bool locks, bool automatic, params DialogueSequence.Line[] lines)
    {
        var value = ScriptableObject.CreateInstance<DialogueSequence>();
        value.name = name;
        value.lockPlayer = locks;
        value.autoAdvance = automatic;
        value.lines = lines;
        owned.Add(value);
        return value;
    }

    private static DialogueSequence.Line Line(string speaker, string text, float duration) =>
        new DialogueSequence.Line { speaker = speaker, text = text, duration = duration };

    private void OnDestroy()
    {
        if (dialogue != null) dialogue.Completed -= DialogueCompleted;
        foreach (var value in owned) if (value != null) { if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
    }
}





