using UnityEngine;
using UnityEngine.Rendering;

// Camera callbacks also cover newly spawned cameras and cameras enabled by cutscenes.
[DisallowMultipleComponent, RequireComponent(typeof(Camera))]
public sealed class RetroScreenEffect : MonoBehaviour
{
    public RetroScreenSettings settings;
    private Material material;
    private static RetroScreenSettings defaults;

    private void OnEnable()
    {
        // Preserve depth requests made by other camera effects.
        GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Register()
    {
        defaults = null;
        Camera.onPreCull -= Attach;
        Camera.onPreCull += Attach;
    }

    private static void Attach(Camera camera)
    {
        if (GraphicsSettings.currentRenderPipeline != null || camera.cameraType != CameraType.Game ||
            camera.targetTexture != null || camera.GetComponent<RetroScreenEffect>() != null) return;
        camera.gameObject.AddComponent<RetroScreenEffect>();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (settings == null)
        {
            if (defaults == null) defaults = Resources.Load<RetroScreenSettings>("RetroScreenSettings");
            if (defaults == null)
            {
                defaults = ScriptableObject.CreateInstance<RetroScreenSettings>();
                defaults.hideFlags = HideFlags.HideAndDontSave;
            }
            settings = defaults;
        }
        if (!settings.effectEnabled) { Graphics.Blit(source, destination); return; }
        if (material == null)
        {
            var shader = Resources.Load<Shader>("RetroScreen");
            if (shader == null || !shader.isSupported) { Graphics.Blit(source, destination); return; }
            material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }
        int height = Mathf.Clamp(settings.verticalResolution, 120, 720);
        int width = Mathf.Max(1, Mathf.RoundToInt(height * (float)source.width / source.height));
        material.SetVector("_Resolution", new Vector4(width, height, 1f / width, 1f / height));
        material.SetVector("_Color", new Vector4(Mathf.Clamp(settings.colorLevels, 8, 256), settings.dithering, 0, 0));
        material.SetColor("_AtmosphereTint", settings.colorTint);
        material.SetColor("_FogColor", settings.fogColor);
        material.SetVector("_Atmosphere", new Vector4(Mathf.Clamp01(settings.atmosphere),
            Mathf.Clamp01(settings.saturation), Mathf.Max(0, settings.fogStart),
            Mathf.Max(settings.fogStart + 0.1f, settings.fogEnd)));
        var low = RenderTexture.GetTemporary(width, height, 0, source.format);
        low.filterMode = FilterMode.Point;
        low.wrapMode = TextureWrapMode.Clamp;
        try
        {
            // Apply fog while the original camera image and its depth still share
            // orientation; a temporary texture can lose the camera's flip metadata.
            Graphics.Blit(source, low, material, 0);
            Graphics.Blit(low, destination, material, 1);
        }
        finally { RenderTexture.ReleaseTemporary(low); }
    }

    private void OnDisable()
    {
        if (material == null) return;
        if (Application.isPlaying) Destroy(material); else DestroyImmediate(material);
        material = null;
    }
}
