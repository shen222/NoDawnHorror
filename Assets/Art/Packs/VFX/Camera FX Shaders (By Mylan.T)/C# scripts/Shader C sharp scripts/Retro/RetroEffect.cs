using UnityEngine;

namespace retro
{
    [RequireComponent(typeof(Camera))]
    public class RetroEffect : MonoBehaviour
    {
        public Material retroMaterial;
        [Range(0f, 1f)] public float scanlineIntensity = 0.5f;
        [Range(0f, 1f)] public float colorFade = 0.2f;
        [Range(0f, 0.2f)] public float noiseStrength = 0.05f;
        [Range(0f, 0.05f)] public float distortion = 0.02f;

        void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (retroMaterial != null)
            {
                retroMaterial.SetFloat("_ScanlineIntensity", scanlineIntensity);
                retroMaterial.SetFloat("_ColorFade", colorFade);
                retroMaterial.SetFloat("_NoiseStrength", noiseStrength);
                retroMaterial.SetFloat("_Distortion", distortion);

                Graphics.Blit(src, dest, retroMaterial);
            }
            else
            {
                Graphics.Blit(src, dest);
            }
        }
    }
}
