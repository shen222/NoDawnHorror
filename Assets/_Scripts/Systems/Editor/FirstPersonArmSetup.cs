using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class FirstPersonArmSetup
{
    [MenuItem("Tools/Combat/Rebuild Original FPS Arms")]
    public static void Build()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before rebuilding arm meshes.");
        var player=UnityEngine.Object.FindObjectsByType<TankControls>(FindObjectsSortMode.None).First(p=>p.GetComponent<HealthManager>()!=null);
        const string folder="Assets/Resources/FirstPersonArms";
        if(!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Resources","FirstPersonArms");
        var parts=new List<FirstPersonArmMeshes.Part>();
        foreach(var source in player.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var allowed=new HashSet<int>();
            for(int i=0;i<source.bones.Length;i++)
            {
                string name=source.bones[i].name;
                if(name.Contains("upper_arm")||name.Contains("forearm")||name.Contains("hand")||name.Contains("f_middle")||name.Contains("thumb")) allowed.Add(i);
            }
            var weights=source.sharedMesh.boneWeights;
            if(weights.Length==0 || allowed.Count==0) continue;
            var mesh=UnityEngine.Object.Instantiate(source.sharedMesh);
            int kept=0;
            for(int sub=0;sub<mesh.subMeshCount;sub++)
            {
                var triangles=source.sharedMesh.GetTriangles(sub); var selected=new List<int>();
                for(int t=0;t<triangles.Length;t+=3)
                {
                    if(Weight(weights[triangles[t]],allowed)<.35f || Weight(weights[triangles[t+1]],allowed)<.35f || Weight(weights[triangles[t+2]],allowed)<.35f) continue;
                    selected.Add(triangles[t]);selected.Add(triangles[t+1]);selected.Add(triangles[t+2]);
                }
                mesh.SetTriangles(selected,sub);kept+=selected.Count;
            }
            if(kept==0) { UnityEngine.Object.DestroyImmediate(mesh);continue; }
            mesh.name=source.name+" Arms";
            string path=folder+"/"+mesh.name+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing!=null) { EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;EditorUtility.SetDirty(mesh); }
            else AssetDatabase.CreateAsset(mesh,path);
            parts.Add(new FirstPersonArmMeshes.Part{rendererName=source.name,mesh=mesh});
        }
        if(parts.Count==0) throw new InvalidOperationException("No original arm triangles found.");
        string dataPath=folder+"/OriginalArms.asset";
        var data=AssetDatabase.LoadAssetAtPath<FirstPersonArmMeshes>(dataPath);
        if(data==null) {data=ScriptableObject.CreateInstance<FirstPersonArmMeshes>();AssetDatabase.CreateAsset(data,dataPath);}
        data.parts=parts.ToArray();EditorUtility.SetDirty(data);AssetDatabase.SaveAssets();
        Debug.Log("Original animated FPS arm selections saved: "+parts.Count);
    }
    private static float Weight(BoneWeight w,HashSet<int> allowed) => (allowed.Contains(w.boneIndex0)?w.weight0:0)+(allowed.Contains(w.boneIndex1)?w.weight1:0)+(allowed.Contains(w.boneIndex2)?w.weight2:0)+(allowed.Contains(w.boneIndex3)?w.weight3:0);
}