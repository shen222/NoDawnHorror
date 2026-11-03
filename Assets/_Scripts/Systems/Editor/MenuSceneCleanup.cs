using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MenuSceneCleanup
{
    [MenuItem("Tools/Menu/Clean and Prepare MainMenu")]
    public static void Apply()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != GunMenuSetup.MenuPath)
            throw new InvalidOperationException("Open MainMenu outside Play mode first.");
        if (scene.GetRootGameObjects().Any(g=>g.GetComponentInChildren<GunMenu>(true)!=null))
            throw new InvalidOperationException("MainMenu already has editable layout. Cleanup is a one-time migration; edit the existing scene objects.");
        if (scene.isDirty) throw new InvalidOperationException("Save your current MainMenu changes before cleanup.");
        Directory.CreateDirectory("Logs/Menu");
        string backup = "Logs/Menu/MainMenu-before-cleanup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity";
        File.Copy(scene.path, backup, false);
        long oldSize = new FileInfo(scene.path).Length;
        int before = scene.GetRootGameObjects().Sum(g=>g.GetComponentsInChildren<Transform>(true).Length);
        var all = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var player = all.Select(t=>t.GetComponent<TankControls>()).First(a=>a!=null && a.gameObject.activeInHierarchy && a.GetComponent<Animator>().runtimeAnimatorController!=null);
        var character = player.gameObject;
        var gun = player.EquippedGun != null ? player.EquippedGun : player.GetComponentInChildren<Gun>();
        var camera = all.Select(t=>t.GetComponent<Camera>()).First(c=>c!=null && c.CompareTag("MainCamera"));
        var alley = all.First(t=>t.name=="TriggerZone alleyway").GetComponent<CamSwitcher>().activeCamera;
        var snow = character.GetComponentsInChildren<ParticleSystem>(true).FirstOrDefault(p=>p.name=="snowEffect");
        // Unpack scene instances so pruning never modifies a shared prefab asset.
        foreach(var root in scene.GetRootGameObjects())
            if(PrefabUtility.IsPartOfPrefabInstance(root)) PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        camera.transform.SetPositionAndRotation(alley.transform.position,alley.transform.rotation);
        camera.fieldOfView=50; camera.nearClipPlane=.08f;
        camera.transform.SetParent(null,true); camera.name="Menu Camera";
        player.transform.SetParent(null,true); player.name="Menu Character";
        player.transform.position=camera.ViewportToWorldPoint(new Vector3(.27f,.12f,4.4f));
        player.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(camera.ViewportToWorldPoint(new Vector3(.77f,.67f,3.5f))-player.transform.position,Vector3.up));
        if(snow!=null) { snow.transform.SetParent(null,true); snow.name="Menu Snow"; if(snow.GetComponent<SnowShelter>()==null) snow.gameObject.AddComponent<SnowShelter>(); }
        var menuRoot=new GameObject("Menu Controller"); SceneManager.MoveGameObjectToScene(menuRoot,scene);
        var menu=menuRoot.AddComponent<GunMenu>();
        menu.menuCharacter=player.transform; menu.characterAnimator=player.GetComponent<Animator>(); menu.menuCamera=camera;
        menu.firePoint=gun.firePoint; menu.weaponAnimator=gun.gunAnimator; menu.shotAudio=gun.audioSource;
        menu.shotClips=gun.gunshotClips; menu.muzzleFlash=gun.shootVFXPrefabA!=null?gun.shootVFXPrefabA:gun.shootVFXPrefabB;
        var guide=gun.firePoint.Find("Cylinder"); if(guide!=null) UnityEngine.Object.DestroyImmediate(guide.gameObject);
        // Keep the model, animation and gunshot data, remove movement/health/weapon gameplay scripts.
        foreach(var behaviour in character.GetComponentsInChildren<MonoBehaviour>(true))
            if(behaviour!=null) UnityEngine.Object.DestroyImmediate(behaviour);
        foreach(var component in character.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(component);
        foreach(var body in character.GetComponentsInChildren<Rigidbody>(true)) UnityEngine.Object.DestroyImmediate(body);
        foreach(var source in character.GetComponentsInChildren<AudioSource>(true))
            if(source!=menu.shotAudio) UnityEngine.Object.DestroyImmediate(source);
        foreach(var behaviour in camera.GetComponents<MonoBehaviour>())
            if(behaviour!=null && !(behaviour is RetroScreenEffect) && behaviour.GetType().Name!="UniversalAdditionalCameraData") UnityEngine.Object.DestroyImmediate(behaviour);
        string[] remove={"Character Controler","======Enemies======","======Camera System======","======Maze Camera======","DialogueSystem","Village NPC (Placeholder)","Car","Door","deadman","Deadman Discovery","DogEnemy","WallColliders","HouseColliders"};
        foreach(var root in scene.GetRootGameObjects()) if(remove.Contains(root.name)) UnityEngine.Object.DestroyImmediate(root);
        var scenery=new GameObject("Menu Scenery"); SceneManager.MoveGameObjectToScene(scenery,scene);
        var planes=GeometryUtility.CalculateFrustumPlanes(camera);
        int removedRenderers=0;
        foreach(var root in scene.GetRootGameObjects())
        {
            if(root==character || root==camera.gameObject || root==menuRoot || root==scenery || (snow!=null && root==snow.gameObject)) continue;
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if(renderer==null) continue;
                var bounds=renderer.bounds; bounds.Expand(4f);
                if(!renderer.gameObject.activeInHierarchy || !GeometryUtility.TestPlanesAABB(planes,bounds))
                { UnityEngine.Object.DestroyImmediate(renderer.gameObject); removedRenderers++; }
            }
            if(root==null) continue;
            foreach(var light in root.GetComponentsInChildren<Light>(true))
                if(light!=null && light.type!=LightType.Directional && !GeometryUtility.TestPlanesAABB(planes,new Bounds(light.transform.position,Vector3.one*(light.range*2+4)))) UnityEngine.Object.DestroyImmediate(light.gameObject);
            if(root==null) continue;
            // Retain roof/scenery colliders for the existing snow shelter; no navigation or interactive scripts.
            foreach(var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                if(behaviour!=null && behaviour.GetType().Name!="UniversalAdditionalLightData") UnityEngine.Object.DestroyImmediate(behaviour);
            foreach(var animator in root.GetComponentsInChildren<Animator>(true)) UnityEngine.Object.DestroyImmediate(animator);
            if(root.GetComponentsInChildren<Renderer>(true).Length==0 && root.GetComponentsInChildren<Terrain>(true).Length==0 && root.GetComponentsInChildren<Light>(true).Length==0 && root.GetComponentsInChildren<AudioSource>(true).Length==0)
                UnityEngine.Object.DestroyImmediate(root);
            else root.transform.SetParent(scenery.transform,true);
        }
        // Prune empty leaves from the scenery after removing off-screen objects.
        foreach(var t in scenery.GetComponentsInChildren<Transform>(true).Reverse())
            if(t!=null && t!=scenery.transform && t.childCount==0 && t.GetComponents<Component>().Length==1) UnityEngine.Object.DestroyImmediate(t.gameObject);
        EditorUtility.SetDirty(menu);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        int after=scene.GetRootGameObjects().Sum(g=>g.GetComponentsInChildren<Transform>(true).Length);
        File.WriteAllText("Logs/Menu/cleanup-result.txt", "MainMenu only; shared assets and MainGame preserved. Backup: "+backup+"\nObjects: "+before+" -> "+after+"\nScene bytes: "+oldSize+" -> "+new FileInfo(scene.path).Length+"\nOff-screen/inactive renderer objects removed: "+removedRenderers+"\nEdit Menu Character position with W outside Play mode; Menu Camera controls framing.\n");
        Selection.activeGameObject=character;
    }
}