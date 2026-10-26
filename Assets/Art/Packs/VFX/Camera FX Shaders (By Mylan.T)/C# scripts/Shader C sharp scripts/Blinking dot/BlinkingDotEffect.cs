using UnityEngine;

namespace blinkingDot
{
    [RequireComponent(typeof(Camera))]
    public class BlinkingDotEffect : MonoBehaviour
    {
        public Material dotMaterial;
        [Range(100, 1000)] public float dotSize = 300;
        [Range(0f, 1f)] public float dotIntensity = 0.3f;
        [Range(0f, 20f)] public float blinkSpeed = 5f;
        [Range(0f, 1f)] public float blinkStrength = 0.5f;

        void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (dotMaterial != null)
            {
                dotMaterial.SetFloat("_DotSize", dotSize);
                dotMaterial.SetFloat("_DotIntensity", dotIntensity);
                dotMaterial.SetFloat("_BlinkSpeed", blinkSpeed);
                dotMaterial.SetFloat("_BlinkStrength", blinkStrength);
                Graphics.Blit(src, dest, dotMaterial);
            }
            else
            {
                Graphics.Blit(src, dest);
            }
        }
    }
}

