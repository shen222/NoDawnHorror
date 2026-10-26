using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public sealed class DialogueSystem : MonoBehaviour
{
    public TankControls player;
    public DialogueSequence arrival;
    public float arrivalDelay = 1.5f;
    public static bool BlocksPlayer { get; private set; }
    public bool IsPlaying => sequence != null;
    private static DialogueSystem instance;
    private TextMeshProUGUI subtitle, hint;
    private DialogueSequence sequence;
    private HealthManager health;
    private int index, startedFrame;
    private float elapsed, arrivalElapsed;
    private bool arrivalPlayed;
    private StoryCameraDirector storyOwner;
    public bool IsPlayingFor(StoryCameraDirector owner) => IsPlaying && storyOwner == owner;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        BlocksPlayer = false;
        DialogueInteractable.Active.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        SceneManager.sceneLoaded += SceneLoaded;
    }

    private static void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "MainGame" || FindFirstObjectByType<DialogueSystem>() != null) return;
        foreach (var tank in FindObjectsByType<TankControls>(FindObjectsSortMode.None))
        {
            if (tank.gameObject.scene != scene) continue;
            var root = new GameObject("DialogueSystem");
            SceneManager.MoveGameObjectToScene(root, scene);
            var system = root.AddComponent<DialogueSystem>();
            system.player = tank;
            system.arrival = Resources.Load<DialogueSequence>("Dialogue/Arrival");
            break;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        var scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        subtitle = Label("Subtitle", 145, 105, 28);
        hint = Label("DialogueHint", 126, 24, 16);
        ClearText();
    }

    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<TankControls>();
        if (player != null) health = player.GetComponent<HealthManager>();
    }

    private void Update()
    {
        if (player == null || (health != null && health.IsDead)) { Stop(); return; }
        // Pause also freezes automatic subtitles and prevents advancing hidden dialogue.
        if (Time.timeScale == 0f)
        {
            subtitle.enabled = hint.enabled = false;
            return;
        }
        subtitle.enabled = hint.enabled = true;
        if (StoryCameraDirector.IsPlaying && storyOwner == null) { hint.text = ""; return; }
        if (!arrivalPlayed && !StoryCameraDirector.IsPlaying)
        {
            arrivalElapsed += Time.deltaTime;
            if (arrivalElapsed >= arrivalDelay && !IsPlaying && !player.IsQuickTurning)
            {
                arrivalPlayed = true;
                if (arrival != null) Play(arrival);
            }
        }
        if (IsPlaying)
        {
            elapsed += Time.deltaTime;
            float duration = sequence.lines[index].duration;
            if (duration <= 0) duration = Mathf.Max(3f, sequence.lines[index].text.Length * 0.065f);
            bool advance = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
            if (Time.frameCount > startedFrame && elapsed >= 0.2f && (advance || (sequence.autoAdvance && elapsed >= duration)))
                Advance();
            return;
        }
        if (StoryCameraDirector.IsPlaying) { hint.text = ""; return; }
        var target = FindTarget();
        hint.text = target != null ? "E  " + JapaneseText.Prompt(target.prompt) : "";
        if (target != null && Input.GetKeyDown(KeyCode.E) && !player.IsQuickTurning && !player.IsAiming)
            target.Interact(this);
    }

    private DialogueInteractable FindTarget()
    {
        DialogueInteractable nearest = null;
        float best = float.PositiveInfinity;
        foreach (var item in DialogueInteractable.Active)
        {
            if (item == null || !item.IsAvailable || item.gameObject.scene != player.gameObject.scene) continue;
            Vector3 offset = item.transform.position - player.transform.position;
            float distance = offset.sqrMagnitude;
            if (distance > item.range * item.range || distance >= best) continue;
            offset.y = 0;
            if (offset.sqrMagnitude > 0.1f && Vector3.Dot(player.transform.forward, offset.normalized) < 0.25f) continue;
            Vector3 eye = player.transform.position + Vector3.up * 1.2f;
            Vector3 destination = item.transform.position + Vector3.up;
            bool blocked = false;
            foreach (var hit in Physics.RaycastAll(eye, destination - eye, Vector3.Distance(eye, destination), ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(player.transform) || hit.transform.IsChildOf(item.transform)) continue;
                blocked = true;
                break;
            }
            if (blocked) continue;
            nearest = item;
            best = distance;
        }
        return nearest;
    }

    public bool Play(DialogueSequence value) => PlayInternal(value, null);

    internal bool PlayForStory(DialogueSequence value, StoryCameraDirector owner)
    {
        if (owner == null || !owner.OwnsCamera) return false;
        return PlayInternal(value, owner);
    }

    public static bool HasValidLines(DialogueSequence value)
    {
        if (value == null || value.lines == null || value.lines.Length == 0) return false;
        foreach (var line in value.lines)
            if (string.IsNullOrWhiteSpace(line.text)) return false;
        return true;
    }

    private bool PlayInternal(DialogueSequence value, StoryCameraDirector owner)
    {
        if (!isActiveAndEnabled || IsPlaying || !HasValidLines(value) ||
            Time.timeScale == 0f || (StoryCameraDirector.IsPlaying && owner == null) || (health != null && health.IsDead)) return false;
        storyOwner = owner;
        sequence = value;
        index = 0;
        BlocksPlayer = value.lockPlayer;
        ShowLine();
        return true;
    }

    private void ShowLine()
    {
        elapsed = 0;
        startedFrame = Time.frameCount;
        var line = sequence.lines[index];
        subtitle.text = string.IsNullOrEmpty(line.speaker) ? line.text : line.speaker + ": " + line.text;
        hint.text = sequence.autoAdvance ? "" : "Enter  次へ";
    }

    public void Advance()
    {
        if (!IsPlaying) return;
        if (++index >= sequence.lines.Length) Stop();
        else ShowLine();
    }

    public void Stop()
    {
        sequence = null;
        storyOwner = null;
        if (instance == this) BlocksPlayer = false;
        ClearText();
    }

    private void ClearText()
    {
        if (subtitle != null) subtitle.text = "";
        if (hint != null) hint.text = "";
    }

    private void OnDisable() => Stop();
    private void OnDestroy() { if (instance == this) { BlocksPlayer = false; instance = null; } }

    private TextMeshProUGUI Label(string label, float bottom, float height, float size)
    {
        var obj = new GameObject(label, typeof(RectTransform));
        var rect = obj.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0.15f, 0);
        rect.anchorMax = new Vector2(0.85f, 0);
        rect.pivot = new Vector2(0.5f, 0);
        rect.anchoredPosition = new Vector2(0, bottom);
        rect.sizeDelta = new Vector2(0, height);
        var text = obj.AddComponent<TextMeshProUGUI>();
        text.font = JapaneseText.Font;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.richText = false;
        text.raycastTarget = false;
        if (text.font != null)
        {
            text.fontMaterial.EnableKeyword("UNDERLAY_ON");
            text.fontMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0, 0, 0, 0.85f));
            text.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.7f);
            text.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.2f);
        }
        return text;
    }
}
