using UnityEngine;

namespace glitch
{
    [RequireComponent(typeof(Camera))]
    public class GlitchEffect : MonoBehaviour
    {
        public Material glitchMaterial;
        [Range(0f, 1f)]
        public float intensity = 0.5f;
        public float timeSpeed = 1f;
        public float blockSize = 10f;

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (glitchMaterial != null)
            {
                glitchMaterial.SetFloat("_Intensity", intensity);
                glitchMaterial.SetFloat("_TimeSpeed", timeSpeed);
                glitchMaterial.SetFloat("_BlockSize", blockSize);
                Graphics.Blit(src, dest, glitchMaterial);
            }
            else
            {
                Graphics.Blit(src, dest);
            }
        }

        public void SetIntensity(float value) => intensity = value;
    }

}