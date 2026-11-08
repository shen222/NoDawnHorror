using System.Collections.Generic;
using UnityEngine;

internal sealed class FirstPersonArmDisplay
{
    private readonly List<SkinnedMeshRenderer> arms = new();
    private readonly Dictionary<Renderer,bool> body = new();
    public bool Show(Transform player,Transform gun)
    {
        if(arms.Count==0)
        {
            var data=Resources.Load<FirstPersonArmMeshes>("FirstPersonArms/OriginalArms");
            if(data==null) { Debug.LogError("Original FPS arm meshes missing. Run Tools/Combat/Rebuild Original FPS Arms."); return false; }
            var originals=player.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach(var part in data.parts)
            {
                SkinnedMeshRenderer source=null;
                foreach(var candidate in originals) if(candidate.name==part.rendererName) { source=candidate;break; }
                if(source==null || part.mesh==null) continue;
                var root=new GameObject("FPS Arms "+source.name);
                root.layer=source.gameObject.layer;
                root.transform.SetParent(source.transform,false);
                var renderer=root.AddComponent<SkinnedMeshRenderer>();
                renderer.sharedMesh=part.mesh; renderer.sharedMaterials=source.sharedMaterials;
                renderer.bones=source.bones; renderer.rootBone=source.rootBone;
                renderer.localBounds=source.localBounds; renderer.updateWhenOffscreen=true;
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows=source.receiveShadows;
                arms.Add(renderer);
            }
        }
        if(arms.Count==0) return false;
        foreach(var renderer in player.GetComponentsInChildren<Renderer>(true))
        {
            if(renderer is not SkinnedMeshRenderer && renderer is not MeshRenderer) continue;
            if(renderer is SkinnedMeshRenderer skinned && arms.Contains(skinned)) continue;
            if(gun!=null && renderer.transform.IsChildOf(gun)) continue;
            body[renderer]=renderer.forceRenderingOff;
            renderer.forceRenderingOff=true;
        }
        foreach(var renderer in arms) renderer.enabled=true;
        return true;
    }
    public void Hide()
    {
        foreach(var renderer in arms) if(renderer!=null) renderer.enabled=false;
        foreach(var pair in body) if(pair.Key!=null) pair.Key.forceRenderingOff=pair.Value;
        body.Clear();
    }
    public void Dispose()
    {
        Hide();
        foreach(var renderer in arms) if(renderer!=null) Object.Destroy(renderer.gameObject);
        arms.Clear();
    }
}