using UnityEngine;

namespace dotMask
{
    [RequireComponent(typeof(Camera))]
    public class DotMaskEffect : MonoBehaviour
    {
        public Material dotMaterial;
        [Range(100, 1000)] public float dotSize = 300;
        [Range(0f, 1f)] public float dotIntensity = 0.3f;

        void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (dotMaterial != null)
            {
                dotMaterial.SetFloat("_DotSize", dotSize);
                dotMaterial.SetFloat("_DotIntensity", dotIntensity);
                Graphics.Blit(src, dest, dotMaterial);
            }
            else
            {
                Graphics.Blit(src, dest);
            }
        }
    }

}