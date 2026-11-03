using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class InspectionViewSetup
{
    [MenuItem("Tools/Player/Build Inspection Hands")]
    public static void Build()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        var player=UnityEngine.Object.FindObjectsByType<TankControls>(FindObjectsSortMode.None).First(p=>p.gameObject.activeInHierarchy);
        var sourceClone=UnityEngine.Object.Instantiate(player.gameObject);
        sourceClone.SetActive(false);
        sourceClone.hideFlags=HideFlags.HideAndDontSave;
        player=sourceClone.GetComponent<TankControls>();
        var gun=player.EquippedGun;
        const string folder="Assets/Resources/Inspection";
        if(!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Resources","Inspection");
        var root=new GameObject("Inspection Hands");
        try
        {
            var model=root.AddComponent<InspectionViewModel>();
            var bones=player.GetComponentsInChildren<Transform>(true);
            var left=bones.First(t=>t.name=="DEF-handL"); var right=bones.First(t=>t.name=="DEF-handR");
            Quaternion leftFrame=Frame(left,bones,"L"), rightFrame=Frame(right,bones,"R");
            model.leftHand=Arm(root.transform,player,left,leftFrame,"L",folder);
            model.rightHand=Arm(root.transform,player,right,rightFrame,"R",folder);
            model.leftHand.localPosition=new Vector3(-.22f,-.10f,.55f);
            model.leftHand.localRotation=Quaternion.Euler(-65,15,-10);
            model.rightHand.localPosition=new Vector3(.21f,-.17f,.45f);
            var barrel=gun.transform.Find("core/glock_barrel").GetComponent<MeshRenderer>();
            var mag=gun.transform.Find("core/magazine/glock_magazine").GetComponent<MeshRenderer>();
            Vector3 barrelDirection=(gun.firePoint.position-barrel.bounds.center).normalized;
            Vector3 weaponUp=Vector3.ProjectOnPlane(barrel.bounds.center-mag.bounds.center,barrelDirection).normalized;
            Quaternion muzzleFrame=Quaternion.Inverse(rightFrame)*Quaternion.LookRotation(barrelDirection,weaponUp);
            model.rightHand.localRotation=Quaternion.LookRotation(new Vector3(-.35f,.45f,.8f),Vector3.up)*Quaternion.Inverse(muzzleFrame);
            foreach(var filter in gun.GetComponentsInChildren<MeshFilter>(true))
            {
                if(filter.name=="Cylinder") continue;
                var source=filter.GetComponent<MeshRenderer>(); if(source==null) continue;
                var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);
                var vertices=mesh.vertices;
                for(int i=0;i<vertices.Length;i++) vertices[i]=Quaternion.Inverse(rightFrame)*(filter.transform.TransformPoint(vertices[i])-right.position);
                mesh.vertices=vertices; mesh.RecalculateNormals(); mesh.RecalculateBounds();
                string path=folder+"/"+filter.name+".asset"; mesh=SaveMesh(mesh,path);
                MeshObject(filter.name,model.rightHand,mesh,source.sharedMaterials);
            }
            var strap=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            strap.name="Wrist Watch Strap"; UnityEngine.Object.DestroyImmediate(strap.GetComponent<Collider>());
            strap.transform.SetParent(model.leftHand,false);
            strap.transform.localPosition=new Vector3(.035f,.01f,-.035f);
            strap.transform.localRotation=Quaternion.Euler(90,0,0);
            strap.transform.localScale=new Vector3(.105f,.013f,.10f);
            strap.GetComponent<Renderer>().sharedMaterial=Material(folder+"/WatchStrap.asset",new Color(.035f,.031f,.027f));
            Cube("Wrist Watch",model.leftHand,new Vector3(.035f,.067f,-.035f),new Vector3(.084f,.012f,.038f),Material(folder+"/WatchCase.asset",new Color(.12f,.115f,.105f)));
            Cube("Watch Screen",model.leftHand,new Vector3(.035f,.075f,-.035f),new Vector3(.076f,.002f,.030f),Material(folder+"/WatchScreen.asset",new Color(.012f,.008f,.008f)));
            model.liveBoxMaterial=Material(folder+"/WatchRed.asset",new Color(1f,.025f,.015f));
            model.emptyBoxMaterial=Material(folder+"/WatchEmpty.asset",new Color(.12f,.015f,.012f));
            model.healthBoxes=new Renderer[5];
            for(int i=0;i<5;i++)
                model.healthBoxes[i]=Cube("Health Box "+(i+1),model.leftHand,new Vector3(.035f+(i-2)*.014f,.078f,-.035f),new Vector3(.010f,.002f,.020f),model.liveBoxMaterial).GetComponent<Renderer>();
            foreach(var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=30;
            foreach(var r in root.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows=false; }
            PrefabUtility.SaveAsPrefabAsset(root,folder+"/Hands.prefab");
            AssetDatabase.SaveAssets();
            Debug.Log("First-person inspection hands built from the current player and pistol.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(sourceClone); }
    }
    private static Quaternion Frame(Transform hand,Transform[] bones,string side)
    {
        Vector3 forward=bones.First(t=>t.name=="DEF-f_middle01"+side).position-hand.position;
        Vector3 thumb=bones.First(t=>t.name=="DEF-thumb01"+side).position-hand.position;
        Vector3 palm=side=="L"?Vector3.Cross(forward,thumb):Vector3.Cross(thumb,forward);
        return Quaternion.LookRotation(forward,palm);
    }
    private static Transform Arm(Transform parent,TankControls player,Transform hand,Quaternion frame,string side,string folder)
    {
        var root=new GameObject(side=="L"?"Left Hand":"Right Hand"); root.transform.SetParent(parent,false);
        foreach(var source in player.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var weights=source.sharedMesh.boneWeights;
            if(weights.Length==0) continue;
            var allowed=new HashSet<int>();
            for(int b=0;b<source.bones.Length;b++) if(source.bones[b].name=="DEF-forearm"+side || source.bones[b]==hand || source.bones[b].IsChildOf(hand)) allowed.Add(b);
            var baked=new Mesh(); source.BakeMesh(baked, false);
            var old=baked.vertices; var uv=baked.uv;
            var positions=new List<Vector3>(); var uvs=new List<Vector2>(); var map=new Dictionary<int,int>(); var submeshes=new List<int[]>();
            for(int sub=0;sub<baked.subMeshCount;sub++)
            {
                var kept=new List<int>(); var tris=baked.GetTriangles(sub);
                for(int t=0;t<tris.Length;t+=3)
                {
                    if(Weight(weights[tris[t]],allowed)<.35f || Weight(weights[tris[t+1]],allowed)<.35f || Weight(weights[tris[t+2]],allowed)<.35f) continue;
                    for(int k=0;k<3;k++)
                    {
                        int idx=tris[t+k];
                        if(!map.TryGetValue(idx,out int next))
                        { next=positions.Count; map[idx]=next; positions.Add(Quaternion.Inverse(frame)*((source.transform.position + source.transform.rotation * old[idx])-hand.position)); uvs.Add(uv.Length>idx?uv[idx]:Vector2.zero); }
                        kept.Add(next);
                    }
                }
                submeshes.Add(kept.ToArray());
            }
            UnityEngine.Object.DestroyImmediate(baked);
            if(positions.Count==0) continue;
            var mesh=new Mesh {name=side+" "+source.name}; mesh.SetVertices(positions); mesh.SetUVs(0,uvs); mesh.subMeshCount=submeshes.Count;
            for(int s=0;s<submeshes.Count;s++) mesh.SetTriangles(submeshes[s],s);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            mesh=SaveMesh(mesh,folder+"/"+side+"-"+source.name+".asset"); MeshObject(source.name,root.transform,mesh,source.sharedMaterials);
        }
        if(root.GetComponentsInChildren<MeshRenderer>().Length==0) throw new InvalidOperationException("No arm geometry extracted for "+side);
        return root.transform;
    }
    private static GameObject Cube(string name,Transform parent,Vector3 position,Vector3 size,Material material)
    {
        var obj=GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name=name;
        UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>()); obj.transform.SetParent(parent,false);
        obj.transform.localPosition=position; obj.transform.localScale=size; obj.GetComponent<Renderer>().sharedMaterial=material;
        return obj;
    }
    private static float Weight(BoneWeight w,HashSet<int> set) => (set.Contains(w.boneIndex0)?w.weight0:0)+(set.Contains(w.boneIndex1)?w.weight1:0)+(set.Contains(w.boneIndex2)?w.weight2:0)+(set.Contains(w.boneIndex3)?w.weight3:0);
    private static GameObject MeshObject(string name,Transform parent,Mesh mesh,Material[] materials)
    {
        var part=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)); part.transform.SetParent(parent,false);
        part.GetComponent<MeshFilter>().sharedMesh=mesh; part.GetComponent<MeshRenderer>().sharedMaterials=materials; return part;
    }
    private static Mesh SaveMesh(Mesh mesh,string path)
    {
        mesh.name=System.IO.Path.GetFileNameWithoutExtension(path);
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing!=null) { EditorUtility.CopySerialized(mesh,existing); UnityEngine.Object.DestroyImmediate(mesh); }
        else AssetDatabase.CreateAsset(mesh,path);
        return existing!=null?existing:mesh;
    }
    private static Material Material(string path,Color color)
    {
        var value=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(value==null) { value=new Material(Shader.Find("Unlit/Color")); AssetDatabase.CreateAsset(value,path); }
        value.color=color; EditorUtility.SetDirty(value); return value;
    }
}