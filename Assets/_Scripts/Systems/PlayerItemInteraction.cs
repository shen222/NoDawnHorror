using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-600)]
public sealed class PlayerItemInteraction : MonoBehaviour
{
    [Min(1)] public float lettersPerSecond = 35;
    public static bool BlocksPlayer => active != null && active.IsOpen;
    public static bool ConsumedInput => inputFrame == Time.frameCount;
    public bool IsOpen => current != null;
    private static PlayerItemInteraction active;
    private static int inputFrame = -1;
    private TankControls player;
    private HealthManager health;
    private PickupItem current;
    private GameObject canvasObject, panel;
    private TextMeshProUGUI prompt, question, yes, no;
    private UnityEngine.UI.RawImage image;
    private PickupPreview preview;
    private bool chooseYes = true, cursorVisible;
    private CursorLockMode cursorLock;
    private float previousTimeScale;
    private int openedFrame;
    private float visibleLetters;
    private int totalLetters;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        active = null;
        inputFrame = -1;
        PickupItem.Active.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= Loaded;
        SceneManager.sceneLoaded += Loaded;
    }

    private static void Loaded(Scene scene, LoadSceneMode mode)
    {
        foreach (var tank in FindObjectsByType<TankControls>(FindObjectsSortMode.None))
            if (tank.gameObject.scene == scene && tank.GetComponent<HealthManager>() != null &&
                tank.GetComponent<PlayerItemInteraction>() == null)
                tank.gameObject.AddComponent<PlayerItemInteraction>();
    }

    private void Start()
    {
        player = GetComponent<TankControls>();
        health = GetComponent<HealthManager>();
        BuildUI();
    }

    private bool CanInteract => player != null && player.isActiveAndEnabled && health != null && !health.IsDead &&
        !player.IsAiming && !player.IsQuickTurning && !PlayerInspection.BlocksPlayer &&
        !DialogueSystem.BlocksPlayer && !StoryCameraDirector.IsPlaying;

    private void Update()
    {
        if (panel == null) return;
        if (active == this)
        {
            if (current == null || !current.IsAvailable || health == null || health.IsDead)
            { Close(); return; }
            visibleLetters += Time.unscaledDeltaTime * lettersPerSecond;
            question.maxVisibleCharacters = Mathf.Min(totalLetters, Mathf.FloorToInt(visibleLetters));
            if (Time.frameCount <= openedFrame) return;
            if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) Select(true);
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) Select(false);
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                Confirm(chooseYes);
            return;
        }
        var target = CanInteract && Time.timeScale > 0 && !BlocksPlayer ? FindTarget() : null;
        prompt.text = target != null ? "E  調べる" : "";
        if (target != null && Input.GetKeyDown(KeyCode.E)) TryOpen(target);
    }

    private PickupItem FindTarget()
    {
        PickupItem nearest = null;
        float best = float.PositiveInfinity;
        foreach (var item in PickupItem.Active)
        {
            if (item == null || !item.IsAvailable) continue;
            float distance = Vector3.Distance(transform.position, item.transform.position);
            if (distance > item.interactionRange || distance >= best) continue;
            Vector3 origin = transform.position + Vector3.up;
            Vector3 end = item.transform.position + Vector3.up * .15f;
            bool blocked = false;
            foreach (var hit in Physics.RaycastAll(origin, end - origin, Vector3.Distance(origin, end), ~0, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(transform) && !hit.transform.IsChildOf(item.transform))
                { blocked = true; break; }
            if (blocked) continue;
            nearest = item;
            best = distance;
        }
        return nearest;
    }

    public bool TryOpen(PickupItem item)
    {
        if (panel == null || item == null || !item.IsAvailable || BlocksPlayer || !CanInteract || Time.timeScale <= 0 ||
            Vector3.Distance(transform.position, item.transform.position) > item.interactionRange) return false;
        current = item;
        active = this;
        openedFrame = inputFrame = Time.frameCount;
        var aim = player.GetComponent<ShoulderAim>();
        if (aim != null) aim.ReturnToExplorationImmediately();
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0;
        cursorLock = Cursor.lockState;
        cursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        prompt.text = "";
        question.text = $"<color=#78B477>{Escape(item.DisplayName)}</color> がある。\n拾いますか？";
        if (!string.IsNullOrWhiteSpace(item.description)) question.text += "\n<size=22>" + Escape(item.description) + "</size>";
        panel.SetActive(true);
        StartTyping();
        preview.Show(item);
        image.texture = preview.Texture;
        Select(true);
        return true;
    }

    private static string Escape(string text) => text.Replace("<", "＜").Replace(">", "＞");

    private void StartTyping()
    {
        question.maxVisibleCharacters = int.MaxValue;
        question.ForceMeshUpdate();
        totalLetters = question.textInfo.characterCount;
        visibleLetters = 0;
        question.maxVisibleCharacters = 0;
    }

    public void Confirm(bool take)
    {
        if (!IsOpen) return;
        inputFrame = Time.frameCount;
        if (question.maxVisibleCharacters < totalLetters)
        {
            visibleLetters = totalLetters;
            question.maxVisibleCharacters = totalLetters;
            return;
        }
        if (!take) { Close(); return; }
        if (current.TryCollect(player, out string message)) { Close(); return; }
        question.text = string.IsNullOrWhiteSpace(message) ? "今は拾えない。" : message;
        StartTyping();
        Select(false);
    }

    private void Select(bool value)
    {
        chooseYes = value;
        yes.color = value ? new Color(.85f, .28f, .28f) : Color.white;
        no.color = value ? Color.white : new Color(.85f, .28f, .28f);
        yes.text = value ? "› はい" : "  はい";
        no.text = value ? "  いいえ" : "› いいえ";
    }

    public void Close()
    {
        if (active != this) return;
        inputFrame = Time.frameCount;
        Time.timeScale = previousTimeScale;
        Cursor.lockState = cursorLock;
        Cursor.visible = cursorVisible;
        current = null;
        active = null;
        if (panel != null) panel.SetActive(false);
        if (image != null) image.texture = null;
        if (preview != null) preview.Clear();
    }

    private void OnEnable()
    {
        if (canvasObject != null) canvasObject.SetActive(true);
    }

    private void OnDisable()
    {
        Close();
        if (canvasObject != null) canvasObject.SetActive(false);
    }
    private void OnDestroy()
    {
        Close();
        if (canvasObject != null) Destroy(canvasObject);
    }

    private void BuildUI()
    {
        canvasObject = new GameObject("Item Pickup UI", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(canvasObject, gameObject.scene);
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.GetComponent<Canvas>().sortingOrder = 95;
        var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = .5f;
        prompt = Label("Nearby Item", canvasObject.transform, new Vector2(.5f, .13f), new Vector2(400, 40), 24);
        prompt.alignment = TextAlignmentOptions.Center;
        panel = new GameObject("Pickup Confirmation", typeof(RectTransform));
        panel.transform.SetParent(canvasObject.transform, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        var shade = new GameObject("Scene Shade", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        shade.transform.SetParent(panel.transform, false);
        var shadeRect = shade.GetComponent<RectTransform>();
        shadeRect.anchorMin = Vector2.zero;
        shadeRect.anchorMax = Vector2.one;
        shadeRect.sizeDelta = Vector2.zero;
        shade.GetComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, .38f);
        var previewObject = new GameObject("Item Model", typeof(RectTransform), typeof(UnityEngine.UI.RawImage));
        previewObject.transform.SetParent(panel.transform, false);
        var previewRect = previewObject.GetComponent<RectTransform>();
        previewRect.anchorMin = previewRect.anchorMax = new Vector2(.5f, .57f);
        previewRect.sizeDelta = new Vector2(330, 330);
        image = previewObject.GetComponent<UnityEngine.UI.RawImage>();
        image.raycastTarget = false;
        preview = canvasObject.AddComponent<PickupPreview>();
        question = Label("Question", panel.transform, new Vector2(.5f, .19f), new Vector2(800, 125), 32);
        question.alignment = TextAlignmentOptions.Center;
        yes = Choice("Yes", new Vector2(.15f, .54f), true);
        no = Choice("No", new Vector2(.15f, .47f), false);
        var hint = Label("Controls", panel.transform, new Vector2(.5f, .045f), new Vector2(850, 30), 18);
        hint.text = "↑ ↓ 選択     E / Enter 決定     Esc 戻る";
        hint.alignment = TextAlignmentOptions.Center;
        panel.SetActive(false);
    }

    private TextMeshProUGUI Choice(string name, Vector2 anchor, bool take)
    {
        var label = Label(name, panel.transform, anchor, new Vector2(180, 48), 32);
        label.raycastTarget = true;
        var button = label.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = label;
        button.transition = UnityEngine.UI.Selectable.Transition.None;
        button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
        button.onClick.AddListener(() => { if (!ConsumedInput) Confirm(take); });
        return label;
    }

    private TextMeshProUGUI Label(string name, Transform parent, Vector2 anchor, Vector2 size, float fontSize)
    {
        var labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);
        var rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.sizeDelta = size;
        var label = labelObject.GetComponent<TextMeshProUGUI>();
        label.font = GameFonts.UI;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }
}
