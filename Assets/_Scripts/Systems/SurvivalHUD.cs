using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// Runtime-only overlay: the authored scenes and prefabs remain editable as before.
public sealed class SurvivalHUD : MonoBehaviour
{
    private HealthManager health;


    private TextMeshProUGUI centerText;
    private UnityEngine.UI.Image damageFlash;
    private Material bloodVignetteMaterial;
    [Header("Health screen effect")]
    [Range(0, 0.65f)] public float maximumHealthTint = 0.45f;
    [Min(0.01f)] public float healthTintFadeTime = 0.45f;
    private float targetHealthTint, healthTint, healthTintVelocity;
    private GameObject centerPanel;
    private UnityEngine.UI.Button restartButton;
    private bool restarting;
    private bool paused;
    private float previousTimeScale = 1f, flashAlpha;
    private int lastHealth = -1;


    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "MainGame" || FindFirstObjectByType<SurvivalHUD>() != null) return;
        foreach (var tank in FindObjectsByType<TankControls>(FindObjectsSortMode.None))
        {
            if (tank.gameObject.scene != scene) continue;
            var playerHealth = tank.GetComponent<HealthManager>();
            if (playerHealth == null) continue;
            var hud = new GameObject("SurvivalHUD").AddComponent<SurvivalHUD>();
            SceneManager.MoveGameObjectToScene(hud.gameObject, scene);
            hud.health = playerHealth;
            return;
        }
    }

    private void Start()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var events = new GameObject("HUDEventSystem", typeof(UnityEngine.EventSystems.EventSystem));
            SceneManager.MoveGameObjectToScene(events, gameObject.scene);
#if ENABLE_INPUT_SYSTEM
            events.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            events.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
        }
        var scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        var flash = Panel("DamageFlash", transform, Vector2.zero, Vector2.zero, Vector2.zero);
        Stretch(flash);
        damageFlash = flash.GetComponent<UnityEngine.UI.Image>();
        var bloodShader = Resources.Load<Shader>("UI/BloodHealthVignette");
        if (bloodShader != null)
        {
            bloodVignetteMaterial = new Material(bloodShader);
            damageFlash.material = bloodVignetteMaterial;
        }
        damageFlash.color = Color.clear;
        // Tint the game view while keeping HUD labels and death/pause controls readable.
        flash.SetAsFirstSibling();

        var center = Panel("StatusPanel", transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500, 200));
        centerPanel = center.gameObject;
        centerText = Label("Status", center, new Vector2(20, -20), new Vector2(460, 90), 30);
        centerText.alignment = TextAlignmentOptions.Center;
        var restartRect = Panel("RestartButton", center, new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(220, 48));
        var restartImage = restartRect.GetComponent<UnityEngine.UI.Image>();
        restartImage.color = new Color(0.25f, 0.4f, 0.3f, 1f);
        restartImage.raycastTarget = true;
        restartButton = restartRect.gameObject.AddComponent<UnityEngine.UI.Button>();
        restartButton.targetGraphic = restartImage;
        restartButton.onClick.AddListener(RestartGame);
        var restartLabel = Label("RestartLabel", restartRect, Vector2.zero, Vector2.zero, 22);
        Stretch(restartLabel.rectTransform);
        restartLabel.text = "やり直す";
        restartLabel.alignment = TextAlignmentOptions.Center;
        restartButton.gameObject.SetActive(false);
        centerPanel.SetActive(false);
        health.OnHealthChanged += OnHealthChanged;
        OnHealthChanged(health.CurrentHealth, health.MaxHealth);
    }

    private void Update()
    {
        if (health == null || centerText == null) return;
        if (Input.GetKeyDown(KeyCode.Escape) && !health.IsDead && !PlayerInspection.ConsumedEscape && !PlayerInspection.BlocksPlayer && !PlayerItemInteraction.BlocksPlayer && !PlayerItemInteraction.ConsumedInput)
        {
            if (!paused)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                paused = true;
            }
            else Resume();
        }
        centerPanel.SetActive(paused || health.IsDead);
        restartButton.gameObject.SetActive(health.IsDead);
        if (health.IsDead)
        {
            centerText.text = "死亡";
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) RestartGame();
        }
        else if (paused) centerText.text = "一時停止";

        flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, Time.unscaledDeltaTime * 0.65f);
        healthTint = Mathf.SmoothDamp(healthTint, targetHealthTint, ref healthTintVelocity,
            healthTintFadeTime, Mathf.Infinity, Time.unscaledDeltaTime);
        if (targetHealthTint == 0 && healthTint < 0.001f) healthTint = healthTintVelocity = 0;
        damageFlash.color = new Color(0.6f, 0.03f, 0.02f, Mathf.Max(healthTint, flashAlpha));
    }

    private void OnHealthChanged(int current, int maximum)
    {
        if (lastHealth >= 0 && current < lastHealth) flashAlpha = 0.28f;
        // Healing clears the brief hit flash too, so only the remaining injury tint fades out.
        if (lastHealth >= 0 && current > lastHealth) flashAlpha = 0;
        float ratio = maximum > 0 ? Mathf.Clamp01((float)current / maximum) : 1f;
        targetHealthTint = (1f - ratio) * maximumHealthTint;
        if (lastHealth < 0) healthTint = targetHealthTint;
        lastHealth = current;
    }

    private void Resume()
    {
        if (!paused) return;
        Time.timeScale = previousTimeScale;
        paused = false;
    }

    private void RestartGame()
    {
        if (restarting || health == null || !health.IsDead) return;
        restarting = true;
        restartButton.interactable = false;
        // Prevent pause cleanup from restoring an old time scale after reloading.
        paused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameObject.scene.path);
    }

    private void OnDisable() { Resume(); }
    private void OnDestroy()
    {
        if (health != null) health.OnHealthChanged -= OnHealthChanged;
        if (bloodVignetteMaterial != null) Destroy(bloodVignetteMaterial);
        Resume();
    }

    private static RectTransform Panel(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        var rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var image = obj.GetComponent<UnityEngine.UI.Image>();
        image.color = new Color(0.035f, 0.05f, 0.05f, 0.88f);
        image.raycastTarget = false;
        return rect;
    }

    private static TextMeshProUGUI Label(string name, Transform parent, Vector2 position, Vector2 size, float fontSize)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        var rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var text = obj.AddComponent<TextMeshProUGUI>();
        text.font = GameFonts.UI;
        text.fontSize = fontSize;
        text.enableAutoSizing = false;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
