using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-500)]
public sealed class PlayerInspection : MonoBehaviour
{
    public static bool BlocksPlayer => active != null && active.IsOpen;
    public static bool ConsumedEscape => escapeFrame == Time.frameCount;
    public bool IsOpen { get; private set; }
    private static PlayerInspection active;
    private static int escapeFrame = -1;
    private TankControls player;
    private HealthManager health;
    private Gun gun;
    private Camera view, handsCamera;
    private CinemachineBrain brain;
    private CameraObstacleAvoidance obstruction;
    private ShoulderAim shoulder;
    private InspectionViewModel hands;
    private Transform head;
    private GameObject overlay;
    private CanvasGroup overlayAlpha;
    private TextMeshProUGUI ammo, instructions;
    private readonly Dictionary<Renderer,bool> hidden = new Dictionary<Renderer,bool>();
    private Vector3 previousPosition, blendPosition;
    private Quaternion previousRotation, blendRotation;
    private float previousFov, blendFov, blendTime, look;
    private bool closing, brainEnabled, obstructionEnabled, shoulderEnabled, cursorVisible;
    private CursorLockMode cursorLock;
    private int oldMask;
    private const float BlendDuration=.4f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { active=null; escapeFrame=-1; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register() { SceneManager.sceneLoaded-=Loaded; SceneManager.sceneLoaded+=Loaded; }
    private static void Loaded(Scene scene,LoadSceneMode mode)
    {
        if(scene.name!="MainGame") return;
        foreach(var p in FindObjectsByType<TankControls>(FindObjectsSortMode.None))
            if(p.gameObject.scene==scene && p.gameObject.activeInHierarchy && p.GetComponent<HealthManager>()!=null)
            { if(p.GetComponent<PlayerInspection>()==null) p.gameObject.AddComponent<PlayerInspection>(); break; }
    }
    private void Start()
    {
        player=GetComponent<TankControls>(); health=GetComponent<HealthManager>();
        gun=player.EquippedGun!=null?player.EquippedGun:GetComponentInChildren<Gun>();
        foreach(var t in GetComponentsInChildren<Transform>()) if(t.name=="DEF-head") {head=t;break;}
        health.OnDeath+=ForceClose;
        var handsPrefab=Resources.Load<GameObject>("Inspection/Hands");
        if(handsPrefab==null) { Debug.LogError("Inspection hands missing. Run Tools/Player/Build Inspection Hands."); enabled=false; return; }
        // Instantiate only visual meshes: no duplicated player, weapon or damage scripts.
        var root=Instantiate(handsPrefab); root.name="First Person Inspection Hands";
        SceneManager.MoveGameObjectToScene(root,gameObject.scene);
        hands=root.GetComponent<InspectionViewModel>(); root.SetActive(false);
        BuildOverlay();
    }
    private void Update()
    {
        if(hands==null || health==null) return;
        if(Input.GetKeyDown(KeyCode.I)) { if(IsOpen) Close(); else TryOpen(); }
        if(!IsOpen) return;
        if(health.IsDead || StoryCameraDirector.IsPlaying || DialogueSystem.BlocksPlayer || Time.timeScale==0) { ForceClose(); return; }
        if(Input.GetKeyDown(KeyCode.Escape)) {escapeFrame=Time.frameCount;Close();}
        if(closing) return;
        if(Application.isFocused)
        {
            look=Mathf.Clamp((Input.mousePosition.x/Mathf.Max(1,Screen.width)-.5f)*2,-1,1);
        }
        UpdateStats();
    }
    public bool TryOpen()
    {
        if(hands==null || IsOpen || !player.isActiveAndEnabled || health.IsDead || gun!=null && gun.IsReloading ||
           player.IsQuickTurning || Time.timeScale==0 || StoryCameraDirector.IsPlaying || DialogueSystem.BlocksPlayer) return false;
        view=Camera.main; if(view==null) return false;
        shoulder=GetComponent<ShoulderAim>();
        if(shoulder!=null) { shoulder.ReturnToExplorationImmediately(); shoulderEnabled=shoulder.enabled; shoulder.enabled=false; }
        previousPosition=blendPosition=view.transform.position; previousRotation=blendRotation=view.transform.rotation;
        previousFov=blendFov=view.fieldOfView;
        brain=view.GetComponent<CinemachineBrain>(); brainEnabled=brain!=null && brain.enabled;
        obstruction=view.GetComponent<CameraObstacleAvoidance>(); obstructionEnabled=obstruction!=null && obstruction.enabled;
        if(brain!=null) brain.enabled=false; if(obstruction!=null) obstruction.enabled=false;
        oldMask=view.cullingMask; view.cullingMask&=~(1<<30);
        cursorLock=Cursor.lockState; cursorVisible=Cursor.visible; Cursor.lockState=CursorLockMode.None; Cursor.visible=false;
        hidden.Clear();
        foreach(var renderer in GetComponentsInChildren<Renderer>(true))
        {
            if(!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
            hidden[renderer]=renderer.forceRenderingOff; renderer.forceRenderingOff=true;
        }
        if(handsCamera==null)
        {
            var cameraObject=new GameObject("Inspection Foreground Camera");
            SceneManager.MoveGameObjectToScene(cameraObject,gameObject.scene);
            handsCamera=cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<RetroScreenEffect>().enabled=false;
        }
        handsCamera.CopyFrom(view); handsCamera.cullingMask=1<<30; handsCamera.clearFlags=CameraClearFlags.Depth;
        handsCamera.nearClipPlane=.01f; handsCamera.farClipPlane=3; handsCamera.fieldOfView=55; handsCamera.depth=view.depth+5;
        handsCamera.enabled=true;
        hands.transform.SetParent(handsCamera.transform,false); hands.gameObject.SetActive(true);
        overlay.SetActive(true); overlayAlpha.alpha=0;
        IsOpen=true; active=this; closing=false; blendTime=look=0;
        UpdateStats(); return true;
    }
    public void Close()
    {
        if(!IsOpen || closing) return;
        closing=true; blendTime=0;
        blendPosition=view.transform.position; blendRotation=view.transform.rotation; blendFov=view.fieldOfView;
    }
    private void LateUpdate()
    {
        if(!IsOpen || view==null || hands==null) return;
        blendTime+=Time.deltaTime;
        float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(blendTime/BlendDuration));
        if(closing)
        {
            view.transform.SetPositionAndRotation(Vector3.Lerp(blendPosition,previousPosition,t),Quaternion.Slerp(blendRotation,previousRotation,t));
            view.fieldOfView=Mathf.Lerp(blendFov,previousFov,t);
            Present(1-t);
            if(t>=1) ForceClose();
        }
        else
        {
            Vector3 eye=(head!=null?head.position:transform.position+Vector3.up*1.5f)+transform.forward*.05f;
            Quaternion rotation=transform.rotation*Quaternion.Euler(0,look*8,0);
            view.transform.SetPositionAndRotation(Vector3.Lerp(blendPosition,eye,t),Quaternion.Slerp(blendRotation,rotation,t));
            view.fieldOfView=Mathf.Lerp(blendFov,60,t);
            Present(t);
        }
        if(handsCamera!=null) handsCamera.transform.SetPositionAndRotation(view.transform.position,view.transform.rotation);
    }
    private void Present(float visibility)
    {
        overlayAlpha.alpha=visibility;
        float ratio=health.MaxHealth>0?(float)health.CurrentHealth/health.MaxHealth:1;
        hands.Pose(visibility,look,ratio);
    }
    private void UpdateStats()
    {
        ammo.text=gun!=null?$"残弾  {gun.CurrentAmmo} / {gun.magSize}\n予備  {gun.reserveAmmo}":"武器なし";
    }
    public void ForceClose()
    {
        if(!IsOpen) return;
        if(view!=null)
        { view.transform.SetPositionAndRotation(previousPosition,previousRotation); view.fieldOfView=previousFov; view.cullingMask=oldMask; }
        if(brain!=null) brain.enabled=brainEnabled;
        if(obstruction!=null) obstruction.enabled=obstructionEnabled;
        if(shoulder!=null) shoulder.enabled=shoulderEnabled;
        foreach(var pair in hidden) if(pair.Key!=null) pair.Key.forceRenderingOff=pair.Value;
        hidden.Clear();
        if(hands!=null) hands.gameObject.SetActive(false);
        if(handsCamera!=null) handsCamera.enabled=false;
        if(overlay!=null) overlay.SetActive(false);
        Cursor.lockState=cursorLock; Cursor.visible=cursorVisible;
        IsOpen=false; closing=false; if(active==this) active=null;
    }
    private void OnDisable() => ForceClose();
    private void OnDestroy()
    {
        ForceClose();
        if(health!=null) { health.OnDeath-=ForceClose; }
        if(handsCamera!=null) Destroy(handsCamera.gameObject);
        else if(hands!=null) Destroy(hands.gameObject);
        if(overlay!=null) Destroy(overlay);
    }
    private void BuildOverlay()
    {
        overlay=new GameObject("Equipment Inspection Text",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(CanvasGroup));
        SceneManager.MoveGameObjectToScene(overlay,gameObject.scene);
        overlay.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay; overlay.GetComponent<Canvas>().sortingOrder=92;
        var scale=overlay.GetComponent<UnityEngine.UI.CanvasScaler>(); scale.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scale.referenceResolution=new Vector2(1280,720); scale.matchWidthOrHeight=.5f;
        overlayAlpha=overlay.GetComponent<CanvasGroup>(); overlayAlpha.blocksRaycasts=false;
        ammo=Label("Weapon Status",new Vector2(.70f,.68f),new Vector2(330,78),24);
        instructions=Label("Inspection Controls",new Vector2(.5f,.07f),new Vector2(750,35),17); instructions.alignment=TextAlignmentOptions.Center;
        instructions.text="I  閉じる     マウス  見る";
        overlay.SetActive(false);
    }
    private TextMeshProUGUI Label(string name,Vector2 anchor,Vector2 size,int fontSize)
    {
        var root=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));
        var text=root.GetComponent<TextMeshProUGUI>(); text.rectTransform.SetParent(overlay.transform,false);
        text.rectTransform.anchorMin=text.rectTransform.anchorMax=anchor; text.rectTransform.sizeDelta=size;
        text.font=GameFonts.UI; text.fontSize=fontSize; text.color=new Color(.93f,.91f,.84f); text.raycastTarget=false;
        return text;
    }
}