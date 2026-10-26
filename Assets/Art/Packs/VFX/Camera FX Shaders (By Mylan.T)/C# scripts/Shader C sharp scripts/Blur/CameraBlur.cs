using UnityEngine;
namespace blur
{
    [RequireComponent(typeof(Camera))]
    public class CameraBlur : MonoBehaviour
    {
        public Material blurMaterial;
        [Range(0.0f, 10.0f)]
        public float blurStrength = 1.0f;

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (blurMaterial != null)
            {
                blurMaterial.SetFloat("_BlurSize", blurStrength);
                Graphics.Blit(source, destination, blurMaterial);
            }
            else
            {
                Graphics.Blit(source, destination);
            }
        }

        public void SetBlur(float value)
        {
            blurStrength = value;
        }
    }
}
