using UnityEngine;

// Presentation only: restore the camera before GunMenu reads the mouse each frame.
[DefaultExecutionOrder(-11000)]
public sealed class MenuShotFeedback : MonoBehaviour
{
    [Range(0, 1)] public float intensity = 1f;
    [Min(.01f)] public float recoilDuration = .18f;
    private Camera view;
    private Transform reticle;
    private Vector3 cameraPosition, reticleScale;
    private Quaternion cameraRotation;
    private UnityEngine.UI.Image flash;
    private RectTransform hitMarker;
    private CanvasGroup hitAlpha;
    private float shotAge = 10, hitAge = 10, roll;
    private bool cameraOffset;
    private GameObject overlay;

    public void Initialize(Camera camera, RectTransform aimReticle)
    {
        view = camera;
        reticle = aimReticle;
        cameraPosition = view.transform.localPosition;
        cameraRotation = view.transform.localRotation;
        reticleScale = reticle.localScale;
        overlay = new GameObject("Menu Shot Feedback", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
        overlay.transform.SetParent(transform, false);
        var canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 180;
        var scaler = overlay.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = .5f;
        flash = Image("Shot Flash", overlay.transform, Vector2.zero, Vector2.zero);
        flash.rectTransform.anchorMin = Vector2.zero;
        flash.rectTransform.anchorMax = Vector2.one;
        flash.rectTransform.offsetMin = flash.rectTransform.offsetMax = Vector2.zero;
        flash.color = Color.clear;
        var marker = new GameObject("Target Hit Marker", typeof(RectTransform), typeof(CanvasGroup));
        hitMarker = marker.GetComponent<RectTransform>();
        hitMarker.SetParent(overlay.transform, false);
        hitMarker.anchorMin = hitMarker.anchorMax = new Vector2(.5f, .5f);
        hitMarker.sizeDelta = new Vector2(36, 36);
        hitAlpha = marker.GetComponent<CanvasGroup>();
        hitAlpha.alpha = 0;
        hitAlpha.blocksRaycasts = false;
        hitAlpha.interactable = false;
        for (int i = 0; i < 4; i++)
        {
            float angle = 45 + i * 90;
            Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            var tick = Image("Hit Tick", hitMarker, direction * 12, new Vector2(9, 2));
            tick.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
            tick.color = new Color(1, .9f, .72f);
        }
    }

    private static UnityEngine.UI.Image Image(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        var image = root.GetComponent<UnityEngine.UI.Image>();
        image.rectTransform.SetParent(parent, false);
        image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(.5f, .5f);
        image.rectTransform.anchoredPosition = position;
        image.rectTransform.sizeDelta = size;
        image.raycastTarget = false;
        return image;
    }

    public void Shot()
    {
        shotAge = 0;
        roll = Random.Range(-.22f, .22f);
    }

    public void Hit(Vector3 worldPoint)
    {
        if (view == null || hitMarker == null) return;
        RestoreCamera();
        var screen = view.WorldToScreenPoint(worldPoint);
        if (screen.z <= 0) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(hitMarker.parent as RectTransform, screen, null, out var point);
        hitMarker.anchoredPosition = point;
        hitAge = 0;
    }

    private void Update()
    {
        RestoreCamera();
        shotAge += Time.unscaledDeltaTime;
        hitAge += Time.unscaledDeltaTime;
    }

    private void LateUpdate()
    {
        if (view == null || flash == null) return;
        float strength = Mathf.Clamp01(intensity);
        float recoil = Mathf.Clamp01(1 - shotAge / Mathf.Max(.01f, recoilDuration));
        recoil *= recoil;
        if (recoil > 0 && strength > 0)
        {
            view.transform.localPosition = cameraPosition + cameraRotation * new Vector3(roll * .025f, .012f, -.025f) * (recoil * strength);
            view.transform.localRotation = cameraRotation * Quaternion.Euler(-.5f * recoil * strength, 0, roll * recoil * strength);
            cameraOffset = true;
        }
        flash.color = new Color(1, .78f, .48f, .065f * Mathf.Clamp01(1 - shotAge / .08f) * strength);
        if (reticle != null) reticle.localScale = reticleScale * (1 + .4f * recoil * strength);
        hitAlpha.alpha = Mathf.Clamp01(1 - hitAge / .22f) * strength;
        hitMarker.localScale = Vector3.one * (1 + .2f * Mathf.Clamp01(1 - hitAge / .22f));
    }

    private void RestoreCamera()
    {
        if (!cameraOffset || view == null) return;
        view.transform.SetLocalPositionAndRotation(cameraPosition, cameraRotation);
        cameraOffset = false;
    }

    private void OnDisable()
    {
        RestoreCamera();
        if (reticle != null) reticle.localScale = reticleScale;
        if (overlay != null) overlay.SetActive(false);
    }
}