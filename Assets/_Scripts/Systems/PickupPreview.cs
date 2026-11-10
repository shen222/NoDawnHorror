using System.Collections.Generic;
using UnityEngine;

// Copies visual meshes only, so inspecting an item cannot run its gameplay scripts.
public sealed class PickupPreview : MonoBehaviour
{
    private Camera previewCamera;
    private GameObject model;
    private RenderTexture texture;
    private readonly List<Material> materials = new List<Material>();
    public Texture Texture => texture;

    public void Show(PickupItem item)
    {
        Clear();
        texture = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
        texture.Create();
        var cameraObject = new GameObject("Item Preview Camera");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, gameObject.scene);
        cameraObject.transform.position = new Vector3(0, 10000, -3);
        previewCamera = cameraObject.AddComponent<Camera>();
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = Color.clear;
        previewCamera.cullingMask = 1 << 31;
        previewCamera.orthographic = true;
        previewCamera.orthographicSize = .85f;
        previewCamera.nearClipPlane = .1f;
        previewCamera.farClipPlane = 10;
        previewCamera.targetTexture = texture;
        previewCamera.enabled = false;
        model = new GameObject("Item Preview Model");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(model, gameObject.scene);
        var source = item.previewModel != null ? item.previewModel : item.transform;
        var bounds = new Bounds();
        bool hasBounds = false;
        var shader = Resources.Load<Shader>("UI/PickupPreview");
        foreach (var filter in source.GetComponentsInChildren<MeshFilter>())
        {
            var renderer = filter.GetComponent<MeshRenderer>();
            if (filter.sharedMesh == null || renderer == null || !renderer.enabled) continue;
            var part = new GameObject(filter.name, typeof(MeshFilter), typeof(MeshRenderer));
            part.layer = 31;
            part.transform.SetParent(model.transform, false);
            part.transform.position = filter.transform.position;
            part.transform.rotation = filter.transform.rotation;
            part.transform.localScale = filter.transform.lossyScale;
            part.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            var copies = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < copies.Length; i++)
            {
                var original = renderer.sharedMaterials[i];
                var copy = new Material(shader);
                if (original != null)
                {
                    copy.mainTexture = original.mainTexture;
                    copy.mainTextureScale = original.mainTextureScale;
                    copy.mainTextureOffset = original.mainTextureOffset;
                    if (original.HasProperty("_Color")) copy.color = original.color;
                }
                copies[i] = copy;
                materials.Add(copy);
            }
            var visual = part.GetComponent<MeshRenderer>();
            visual.sharedMaterials = copies;
            visual.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (hasBounds) bounds.Encapsulate(visual.bounds);
            else { bounds = visual.bounds; hasBounds = true; }
        }
        foreach (Transform part in model.transform) part.position -= bounds.center;
        float diameter = bounds.size.magnitude;
        model.transform.localScale = Vector3.one * (diameter > .0001f ? 1.3f / diameter : 1);
        model.transform.localPosition = new Vector3(0, 10000, 0);
        model.transform.rotation = Quaternion.Euler(12, -25, -12);
        Draw();
    }

    private void LateUpdate()
    {
        if (model == null) return;
        model.transform.Rotate(Vector3.up, 18 * Time.unscaledDeltaTime, Space.World);
        Draw();
    }

    private void Draw()
    {
        if (previewCamera != null) previewCamera.Render();
    }

    public void Clear()
    {
        if (previewCamera != null) Destroy(previewCamera.gameObject);
        if (model != null) { model.SetActive(false); Destroy(model); }
        foreach (var material in materials) Destroy(material);
        materials.Clear();
        if (texture != null) { texture.Release(); Destroy(texture); }
        previewCamera = null;
        model = null;
        texture = null;
    }

    private void OnDestroy() => Clear();
}
