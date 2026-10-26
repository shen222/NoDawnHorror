using UnityEngine;

namespace colorPulse {
    [RequireComponent(typeof(Camera))]
    public class ColorPulseEffect : MonoBehaviour
    {
        public Material pulseMaterial;
        [Range(0f, 10f)] public float pulseSpeed = 1f;
        [Range(0f, 1f)] public float intensity = 0.3f;

        void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (pulseMaterial != null)
            {
                pulseMaterial.SetFloat("_PulseSpeed", pulseSpeed);
                pulseMaterial.SetFloat("_Intensity", intensity);
                Graphics.Blit(src, dest, pulseMaterial);
            }
            else
            {
                Graphics.Blit(src, dest);
            }
        }
    }
}
