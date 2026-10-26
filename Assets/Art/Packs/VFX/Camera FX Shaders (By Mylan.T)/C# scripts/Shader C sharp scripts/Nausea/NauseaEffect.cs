using UnityEngine;

namespace Nausea
{
    [RequireComponent(typeof(Camera))]
    public class NauseaEffect : MonoBehaviour
    {
        public Material nauseaMaterial;
        [Range(0, 0.1f)] public float waveStrength = 0.03f;
        [Range(0, 20f)] public float waveSpeed = 5f;
        [Range(0f, 5f)] public float chromaticAmount = 1.5f;

        void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (nauseaMaterial != null)
            {
                nauseaMaterial.SetFloat("_WaveStrength", waveStrength);
                nauseaMaterial.SetFloat("_WaveSpeed", waveSpeed);
                nauseaMaterial.SetFloat("_ChromaticAmount", chromaticAmount);
                Graphics.Blit(src, dest, nauseaMaterial);
            }
            else
            {
                Graphics.Blit(src, dest);
            }
        }
    }

}