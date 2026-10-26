using UnityEngine;

namespace bulged
{
    [RequireComponent(typeof(Camera))]
    public class BulgedEffect : MonoBehaviour
    {
        public Material bulgedMaterial;
        [Range(0f, 0.5f)] public float distortionAmount = 0.1f;

        void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (bulgedMaterial != null)
            {
                bulgedMaterial.SetFloat("_DistortionAmount", distortionAmount);
                Graphics.Blit(src, dest, bulgedMaterial);
            }
            else
            {
                Graphics.Blit(src, dest);
            }
        }
    }

}