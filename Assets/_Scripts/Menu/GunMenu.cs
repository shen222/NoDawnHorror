using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;


[DefaultExecutionOrder(-10000)]
public sealed class GunMenu : MonoBehaviour
{
    public enum MenuAction { Play, Settings, Quit, VolumeDown, VolumeUp, Fullscreen, Back }
    public const string MenuScene = "MainMenu";
    public const string GameScene = "MainGame";
    private const float TargetDepth = 3.5f;
    public bool IsTransitioning { get; private set; }
    public bool SettingsOpen { get; private set; }
    public Camera View { get; private set; }
    [Header("Scene layout — move Menu Character and Menu Camera in Edit mode")]
    public Transform menuCharacter;
    public Animator characterAnimator;
    public Camera menuCamera;
    public Transform firePoint;
    public Animator weaponAnimator;
    public AudioSource shotAudio;
    public AudioClip[] shotClips;
    public GameObject muzzleFlash;
    [Header("Laser sight")]
    public Color laserColor = new Color(1f, .025f, .015f);
    [Min(.001f)] public float laserWidth = .012f;
    private LineRenderer laser;
    private Transform laserDot;
    private Material laserMaterial;
    private Animator animator;

    private readonly List<ShootableMenuTarget> targets = new List<ShootableMenuTarget>();
    private Material boardMaterial, fragmentMaterial, tracerMaterial;
    private Transform targetsRoot;
    private TextMeshProUGUI hint, settingsHeading;
    private RectTransform reticle;
    private Vector3 aimPoint;
    private float nextShot;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLock;
    private Coroutine transition;
    private MenuShotFeedback shotFeedback;
    public IReadOnlyList<ShootableMenuTarget> Targets => targets;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        AudioListener.volume = PlayerPrefs.GetFloat("NoDawn.Volume", AudioListener.volume);
#if !UNITY_EDITOR
        if (PlayerPrefs.HasKey("NoDawn.Fullscreen")) Screen.fullScreen = PlayerPrefs.GetInt("NoDawn.Fullscreen") == 1;
#endif
        SceneManager.sceneLoaded -= SceneLoaded;
        SceneManager.sceneLoaded += SceneLoaded;
    }

    private static void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != MenuScene) return;
        foreach (var root in scene.GetRootGameObjects())
            if (root.GetComponentInChildren<GunMenu>(true) != null) return;
        var menu = new GameObject("Shootable Main Menu");
        SceneManager.MoveGameObjectToScene(menu, scene);
        menu.AddComponent<GunMenu>();
    }

    private void Awake()
    {
        previousCursorVisible = Cursor.visible;
        previousCursorLock = Cursor.lockState;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;
        Time.timeScale = 1;
        View = menuCamera;
        if (menuCharacter == null || View == null)
        {
            Debug.LogError("MainMenu is not configured. Run Tools/Menu/Clean and Prepare MainMenu.", this);
            Cursor.visible = true;
            enabled = false;
            return;
        }
        aimPoint = View.ViewportToWorldPoint(new Vector3(.77f, .67f, TargetDepth));
        foreach (var child in menuCharacter.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
        var foreground = new GameObject("Menu Foreground").AddComponent<Camera>();
        foreground.transform.SetParent(View.transform, false);
        foreground.CopyFrom(View);
        foreground.clearFlags = CameraClearFlags.Depth;
        foreground.cullingMask = 1 << 31;
        foreground.depth = View.depth + 10;
        View.cullingMask &= ~(1 << 31);
        // The main camera already grades the background. Keep the foreground pass clean.
        foreground.gameObject.AddComponent<RetroScreenEffect>().enabled = false;
        animator = characterAnimator;
        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.SetBool("IsAiming", true);
            foreach (string parameter in new[] { "MovingForward", "MovingBackward", "TurningLeft", "TurningRight", "Sprinting", "QuickTurn" })
                animator.SetBool(parameter, false);
        }
        boardMaterial = MakeMaterial("Menu target", new Color(.065f, .065f, .058f));
        fragmentMaterial = MakeMaterial("Broken target", new Color(.18f, .17f, .14f));
        tracerMaterial = MakeMaterial("Menu bullet", new Color(1f, .78f, .48f));

        targetsRoot = new GameObject("Shootable Targets").transform;
        targetsRoot.SetParent(transform, false);
        BuildOverlay();
        BuildTargets(false);
        CreateLaser();
        shotFeedback = gameObject.AddComponent<MenuShotFeedback>();
        shotFeedback.Initialize(View, reticle);
    }

    private Material MakeMaterial(string name, Color color)
    {
        var shader = Resources.Load<Shader>("MenuSurface");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        var value = new Material(shader) { name = name };
        value.color = color;
        if (value.HasProperty("_BaseColor")) value.SetColor("_BaseColor", color);
        return value;
    }

    private void Update()
    {
        if (View == null || !Application.isFocused) return;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;
        var ray = View.ScreenPointToRay(Input.mousePosition);
        var plane = new Plane(-View.transform.forward, View.transform.position + View.transform.forward * TargetDepth);
        if (plane.Raycast(ray, out float distance)) aimPoint = ray.GetPoint(distance);
        if (reticle != null)
        {
            Vector2 point;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(reticle.parent as RectTransform, Input.mousePosition, null, out point);
            reticle.anchoredPosition = point;
        }
        if (!IsTransitioning && Input.GetMouseButtonDown(0) && Time.time >= nextShot)
            Shoot();
        if (SettingsOpen && !IsTransitioning && Input.GetKeyDown(KeyCode.Escape))
            BuildTargets(false);
    }

    private void LateUpdate()
    {
        if (menuCharacter == null || animator == null || View == null) return;
        Vector3 direction = aimPoint - menuCharacter.position;
        Vector3 flat = Vector3.ProjectOnPlane(direction, Vector3.up);
        if (flat.sqrMagnitude > .001f) menuCharacter.rotation = Quaternion.LookRotation(flat);
        Vector3 muzzle = firePoint != null ? firePoint.position : menuCharacter.position + Vector3.up * 1.2f;
        direction = aimPoint - muzzle;
        float pitch = Mathf.Atan2(direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg;
        animator.SetFloat("AimAngle", Mathf.Clamp(pitch, -45f, 45f));
        animator.SetBool("IsAiming", true);
        UpdateLaser(muzzle);
    }

    public bool Shoot()
    {
        if (IsTransitioning || View == null || Time.time < nextShot) return false;
        nextShot = Time.time + .65f;
        Vector3 muzzle = firePoint != null ? firePoint.position : menuCharacter.position + Vector3.up * 1.2f;
        Vector3 direction = (aimPoint - muzzle).normalized;
        if (direction.sqrMagnitude < .01f) return false;
        if (shotFeedback != null) shotFeedback.Shot();
        if (animator != null) animator.SetTrigger("Shoot");
        if (weaponAnimator != null) weaponAnimator.SetTrigger("Shoot");
        if (shotAudio != null && shotClips != null && shotClips.Length > 0)
            shotAudio.PlayOneShot(shotClips[Random.Range(0, shotClips.Length)]);
        if (muzzleFlash != null) Destroy(Instantiate(muzzleFlash, muzzle, Quaternion.LookRotation(direction)), 1.5f);
        var root = new GameObject("Menu Bullet");
        SceneManager.MoveGameObjectToScene(root, gameObject.scene);
        root.layer = 31;
        root.transform.SetPositionAndRotation(muzzle, Quaternion.LookRotation(direction));
        var tracer = root.AddComponent<LineRenderer>();
        tracer.sharedMaterial = tracerMaterial;
        tracer.useWorldSpace = false;
        tracer.positionCount = 2;
        tracer.SetPosition(0, Vector3.zero);
        tracer.SetPosition(1, Vector3.back * .25f);
        tracer.startWidth = .018f;
        tracer.endWidth = .005f;
        var bullet = root.AddComponent<MenuBullet>();
        bullet.owner = this;
        bullet.direction = direction;
        return true;
    }

    public void TargetHit(ShootableMenuTarget target, Vector3 point)
    {
        if (IsTransitioning || target == null || target.owner != this) return;
        IsTransitioning = true;
        if (shotFeedback != null) shotFeedback.Hit(point);
        Vector3 size = target.transform.localScale;
        for (int i = 0; i < 12; i++)
        {
            var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shard.name = "Broken menu fragment";
            shard.layer = 31;
            shard.transform.position = target.transform.position + View.transform.right * ((i % 4 - 1.5f) * size.x / 4) + View.transform.up * ((i / 4 - 1) * size.y / 3);
            shard.transform.rotation = target.transform.rotation;
            shard.transform.localScale = new Vector3(size.x / 4.5f, size.y / 3.5f, .04f);
            Destroy(shard.GetComponent<Collider>());
            shard.GetComponent<Renderer>().sharedMaterial = fragmentMaterial;
            var fragment = shard.AddComponent<MenuFragment>();
            fragment.velocity = (shard.transform.position - point).normalized * 2f - View.transform.forward * .5f;
            fragment.spin = Random.insideUnitSphere * 170;
            Destroy(shard, 1.5f);
        }
        target.gameObject.SetActive(false);
        transition = StartCoroutine(ActivateAfterBreak(target.action));
    }

    private IEnumerator ActivateAfterBreak(MenuAction action)
    {
        yield return new WaitForSeconds(.35f);
        if (action == MenuAction.Play)
        {
            if (!Application.CanStreamedLevelBeLoaded(GameScene))
            {
                hint.text = "MainGame is missing from Build Settings.";
                BuildTargets(false);
                yield break;
            }
            PlayerPrefs.Save();
            SceneManager.LoadScene(GameScene);
            yield break;
        }
        if (action == MenuAction.Quit)
        {
            PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            yield break;
        }
        if (action == MenuAction.VolumeDown || action == MenuAction.VolumeUp)
        {
            AudioListener.volume = Mathf.Clamp01(AudioListener.volume + (action == MenuAction.VolumeUp ? .1f : -.1f));
            PlayerPrefs.SetFloat("NoDawn.Volume", AudioListener.volume);
            PlayerPrefs.Save();
        }
        if (action == MenuAction.Fullscreen)
        {
            bool fullscreen = !Screen.fullScreen;
            Screen.fullScreen = fullscreen;
            PlayerPrefs.SetInt("NoDawn.Fullscreen", fullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }
        BuildTargets(action != MenuAction.Back);
    }

    private void BuildTargets(bool settings)
    {
        foreach (var target in targets) if (target != null) Destroy(target.gameObject);
        targets.Clear();
        SettingsOpen = settings;
        IsTransitioning = false;
        settingsHeading.text = settings ? "Settings" : "";
        if (!settings)
        {
            Target("Play", MenuAction.Play, .67f);
            Target("Settings", MenuAction.Settings, .5f);
            Target("Quit", MenuAction.Quit, .33f);
        }
        else
        {
            Target("Volume −", MenuAction.VolumeDown, .70f);
            Target("Volume +", MenuAction.VolumeUp, .56f);
            Target(Screen.fullScreen ? "Fullscreen: On" : "Fullscreen: Off", MenuAction.Fullscreen, .42f);
            Target("Back", MenuAction.Back, .28f);
        }
        hint.text = settings ? "Left-click to shoot   ·   Volume " + Mathf.RoundToInt(AudioListener.volume * 100) + "%" : "Left-click to shoot";
    }

    private void Target(string title, MenuAction action, float y)
    {
        float height = 2 * TargetDepth * Mathf.Tan(View.fieldOfView * .5f * Mathf.Deg2Rad);
        var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = title + " Target";
        root.layer = 31;
        root.transform.SetParent(targetsRoot, false);
        root.transform.SetPositionAndRotation(View.ViewportToWorldPoint(new Vector3(.77f, y, TargetDepth)), View.transform.rotation);
        root.transform.localScale = new Vector3(height * View.aspect * .27f, height * .095f, .08f);
        root.GetComponent<Renderer>().sharedMaterial = boardMaterial;
        var target = root.AddComponent<ShootableMenuTarget>();
        target.owner = this;
        target.action = action;
        targets.Add(target);
        var labelObject = new GameObject(title, typeof(RectTransform), typeof(TextMeshPro));
        labelObject.layer = 31;
        labelObject.transform.SetParent(root.transform, false);
        labelObject.transform.localPosition = new Vector3(0, 0, -.6f);
        // Cancel the board's nonuniform scale so the lettering stays undistorted.
        labelObject.transform.localScale = new Vector3(1 / root.transform.localScale.x, 1 / root.transform.localScale.y, 1 / root.transform.localScale.z);
        var label = labelObject.GetComponent<TextMeshPro>();
        label.font = GameFonts.UI;
        label.text = title;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(.91f, .89f, .8f);
        label.fontSize = 5;
        label.enableAutoSizing = true;
        label.fontSizeMin = 1;
        label.fontSizeMax = 5;
        label.rectTransform.sizeDelta = new Vector2(root.transform.localScale.x * .85f, root.transform.localScale.y * .85f);
        label.raycastTarget = false;
    }

    private void BuildOverlay()
    {
        var root = new GameObject("Menu Overlay", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
        root.transform.SetParent(transform, false);
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        root.GetComponent<Canvas>().sortingOrder = 150;
        var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280,720);
        scaler.matchWidthOrHeight = .5f;
        // Plane Crash stores its distressed capital lettering in the lowercase character slots.
        var title = Label(root.transform, "no dawn", new Vector2(.23f,.87f), new Vector2(350,70), 40);
        title.font = GameFonts.Title;
        settingsHeading = Label(root.transform, "", new Vector2(.77f,.86f), new Vector2(350,55), 26);
        settingsHeading.alignment = TextAlignmentOptions.Center;
        hint = Label(root.transform, "", new Vector2(.5f,.065f), new Vector2(800,35), 16);
        hint.alignment = TextAlignmentOptions.Center;
        var cross = Label(root.transform, "+", new Vector2(.5f,.5f), new Vector2(24,24), 21);
        cross.alignment = TextAlignmentOptions.Center;
        reticle = cross.rectTransform;
        reticle.pivot = new Vector2(.5f,.5f);
    }

    private TextMeshProUGUI Label(Transform parent, string text, Vector2 anchor, Vector2 size, float fontSize)
    {
        var root = new GameObject("Menu Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var label = root.GetComponent<TextMeshProUGUI>();
        label.rectTransform.SetParent(parent, false);
        label.rectTransform.anchorMin = label.rectTransform.anchorMax = anchor;
        label.rectTransform.pivot = new Vector2(.5f,.5f);
        label.rectTransform.sizeDelta = size;
        label.font = GameFonts.UI;
        label.fontSize = fontSize;
        label.color = new Color(.9f,.89f,.82f);
        label.text = text;
        label.raycastTarget = false;
        return label;
    }

    private void CreateLaser()
    {
        if (laser != null) return;
        laserMaterial = MakeMaterial("Menu laser red", laserColor);
        var beam = new GameObject("Menu Laser Sight");
        beam.transform.SetParent(transform, false);
        beam.layer = 31;
        laser = beam.AddComponent<LineRenderer>();
        laser.sharedMaterial = laserMaterial;
        laser.useWorldSpace = true;
        laser.positionCount = 2;
        laser.numCapVertices = 4;
        laser.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        laser.receiveShadows = false;
        var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dot.name = "Menu Laser Dot";
        dot.layer = 31;
        Destroy(dot.GetComponent<Collider>());
        dot.GetComponent<Renderer>().sharedMaterial = laserMaterial;
        dot.transform.SetParent(transform, false);
        laserDot = dot.transform;
    }

    private void UpdateLaser(Vector3 muzzle)
    {
        if (laser == null) return;
        Vector3 direction = (aimPoint - muzzle).normalized;
        Vector3 end = aimPoint;
        float nearest = Vector3.Distance(muzzle, aimPoint);
        foreach (var hit in Physics.SphereCastAll(muzzle, .025f, direction, nearest, 1 << 31, QueryTriggerInteraction.Collide))
        {
            var target = hit.collider.GetComponent<ShootableMenuTarget>();
            if (target == null || target.owner != this || target.IsBroken || hit.distance >= nearest) continue;
            nearest = hit.distance;
            end = muzzle + direction * nearest;
        }
        laserMaterial.color = laserColor;
        laser.startWidth = laser.endWidth = laserWidth;
        laser.SetPosition(0, muzzle);
        laser.SetPosition(1, end);
        laserDot.position = end;
        laserDot.localScale = Vector3.one * laserWidth * 3;
    }
    private void OnDisable()
    {
        Cursor.visible = previousCursorVisible;
        Cursor.lockState = previousCursorLock;
        if (transition != null) StopCoroutine(transition);
    }

    private void OnDestroy()
    {
        if (laserMaterial != null) Destroy(laserMaterial);
        if (boardMaterial != null) Destroy(boardMaterial);
        if (fragmentMaterial != null) Destroy(fragmentMaterial);
        if (tracerMaterial != null) Destroy(tracerMaterial);
    }
}




