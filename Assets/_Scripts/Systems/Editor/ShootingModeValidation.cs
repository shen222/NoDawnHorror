using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Unity.Cinemachine;

public static class ShootingModeValidation
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Tools/Combat/Validate Zone Shooting Modes (Play Mode)")]
    public static void Validate()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play mode before running shooting validation.");
        var results = new List<string>();
        var root = new GameObject("Temporary shooting mode verification");
        root.transform.position = new Vector3(100000, 100000, 100000);
        var originalMain = Camera.main;
        CursorLockMode oldLock = Cursor.lockState;
        bool oldCursor = Cursor.visible;
        ShoulderAim shoulder = null;
        try
        {
            var playerRoot = Child(root, "Player");
            playerRoot.AddComponent<CharacterController>();
            playerRoot.AddComponent<MovementState>().enabled = false;
            var player = playerRoot.AddComponent<TankControls>();
            player.enabled = false;
            shoulder = playerRoot.AddComponent<ShoulderAim>();
            shoulder.enabled = false;
            var gun = Child(playerRoot, "Gun").AddComponent<Gun>();
            gun.firePoint = Child(gun.gameObject, "Muzzle").transform;
            gun.firePoint.localPosition = Vector3.up;
            gun.firePoint.localRotation = Quaternion.Euler(0, 90, 0);
            gun.projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/projectile.prefab");
            Invoke(player, "Start");
            Invoke(gun, "Start");

            var cameraRoot = Child(root, "Output camera");
            var camera = cameraRoot.AddComponent<Camera>();
            camera.enabled = false;
            if (originalMain != null) originalMain.tag = "Untagged";
            cameraRoot.tag = "MainCamera";
            // Camera.main ignores disabled cameras, so enable only for this synchronous check.
            camera.enabled = true;
            camera.transform.position = player.transform.position + new Vector3(5, 3, -5);
            camera.transform.LookAt(player.transform.position + Vector3.up);
            var brain = cameraRoot.AddComponent<CinemachineBrain>();
            var collision = cameraRoot.AddComponent<CameraObstacleAvoidance>();
            collision.enabled = false;
            collision.player = player.transform;
            var rig = Child(root, "Rig").AddComponent<ExplorationCameraRig>();
            rig.enabled = false;
            rig.player = player;
            var follow = Child(root, "Follow").AddComponent<CinemachineCamera>();
            Field(rig, "brain", brain);
            Field(rig, "collision", collision);
            Field(rig, "initialized", true);
            Field(rig, "<FollowCamera>k__BackingField", follow);
            var classic = Zone(root, "Classic zone", CamSwitcher.ShootingModeOverride.Classic);
            var overrideZone = Zone(root, "Shoulder zone", CamSwitcher.ShootingModeOverride.ShoulderAim);
            overrideZone.transform.localPosition = Vector3.right * 20;
            void Tick() { Physics.SyncTransforms(); Invoke(rig, "AdvanceExploration", 1f); }
            void Check(bool condition, string message)
            {
                if (!condition) throw new InvalidOperationException("Shooting verification failed: " + message);
                results.Add(message);
            }
            bool Aim(Vector2 delta, bool focused) => (bool)typeof(ShoulderAim)
                .GetMethod("UpdateAim", Private, null, new[] {typeof(Vector2), typeof(bool)}, null)
                .Invoke(shoulder, new object[] {delta, focused});
            Projectile Fire()
            {
                var before = UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None);
                Field(gun, "nextFireTime", 0f);
                gun.TryShoot();
                var shot = UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Except(before).Single();
                shot.transform.SetParent(root.transform, true);
                return shot;
            }

            Check(player.defaultShootingMode == ShootingMode.ShoulderAim, "existing players default to shoulder aiming");
            Tick();
            Check(rig.CurrentZone == classic && player.CurrentShootingMode == ShootingMode.Classic, "entering a classic zone selects classic aiming");
            Field(player, "<IsAiming>k__BackingField", true);
            Vector3 position = player.transform.position;
            Quaternion rotation = player.transform.rotation;
            Vector3 cameraPosition = camera.transform.position;
            Invoke(player, "UpdateClassicAim", 1f, 1f, .25f);
            Check(Quaternion.Angle(rotation, player.transform.rotation) > 20, "classic aiming turns the character");
            Check(player.AimAngle > 10, "classic aiming raises the gun");
            Check(Vector3.Distance(position, player.transform.position) < .001f, "classic aiming does not walk the player");
            Check(Vector3.Distance(cameraPosition, camera.transform.position) < .001f && brain.enabled && !shoulder.IsActive,
                "classic aiming keeps the exploration camera in control");
            Tick();
            Check(collision.fixedShot, "classic aiming preserves fixed-shot obstacle behavior");
            var laser = gun.GetComponent<ClassicLaserSight>();
            Check(laser != null, "existing weapons receive the classic laser automatically");
            player.enabled = true;
            Invoke(laser, "LateUpdate");
            var beam = (LineRenderer)typeof(ClassicLaserSight).GetField("beam", Private).GetValue(laser);
            var dot = (MeshRenderer)typeof(ClassicLaserSight).GetField("dot", Private).GetValue(laser);
            Check(beam != null && beam.enabled && !dot.enabled, "classic aiming shows a beam with no floating sky dot");
            Check(Vector3.Distance(beam.GetPosition(0), gun.firePoint.position) < .02f &&
                Vector3.Distance(beam.GetPosition(1), gun.firePoint.position + gun.firePoint.forward * laser.maximumDistance) < .03f,
                "laser follows the muzzle to its configured maximum range");
            Vector3 laserStart = gun.firePoint.position;
            Vector3 laserDirection = gun.firePoint.forward;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.SetParent(root.transform, true);
            wall.transform.SetPositionAndRotation(laserStart + laserDirection * 5, Quaternion.LookRotation(laserDirection));
            wall.transform.localScale = new Vector3(2, 2, 1);
            var ignoredTrigger = Child(root, "Non-combat trigger").AddComponent<BoxCollider>();
            ignoredTrigger.isTrigger = true;
            ignoredTrigger.transform.position = laserStart + laserDirection * 2;
            var ownerCollider = Child(playerRoot, "Owner collider").AddComponent<BoxCollider>();
            ownerCollider.transform.position = laserStart + laserDirection;
            Physics.SyncTransforms();
            Invoke(laser, "LateUpdate");
            Check(Mathf.Abs(Vector3.Distance(laserStart, beam.GetPosition(1)) - 4.5f) < .04f && dot.enabled,
                "laser stops at a wall and ignores its owner and non-combat triggers");
            Check(CaptureLaserPreview(camera, beam), "laser shader renders a visible red sight");
            var enemyTrigger = Child(root, "Enemy trigger").AddComponent<BoxCollider>();
            enemyTrigger.isTrigger = true;
            enemyTrigger.gameObject.layer = LayerMask.NameToLayer("Enemy");
            enemyTrigger.transform.SetPositionAndRotation(laserStart + laserDirection * 3, Quaternion.LookRotation(laserDirection));
            Physics.SyncTransforms();
            Invoke(laser, "LateUpdate");
            Check(Mathf.Abs(Vector3.Distance(laserStart, beam.GetPosition(1)) - 2.5f) < .04f,
                "laser detects enemy trigger hitboxes before scenery");
            Field(gun, "isReloading", true);
            Invoke(laser, "LateUpdate");
            Check(!beam.enabled && !dot.enabled, "reloading hides the beam and impact dot");
            Field(gun, "isReloading", false);
            float previousScale = Time.timeScale;
            Time.timeScale = 0;
            Invoke(laser, "LateUpdate");
            Check(!beam.enabled, "pause hides the laser");
            Time.timeScale = previousScale;
            Field(player, "<IsAiming>k__BackingField", false);
            Invoke(laser, "LateUpdate");
            Check(!beam.enabled, "lowering the gun hides the laser");
            Field(player, "<IsAiming>k__BackingField", true);
            UnityEngine.Object.DestroyImmediate(wall);
            UnityEngine.Object.DestroyImmediate(ignoredTrigger.gameObject);
            UnityEngine.Object.DestroyImmediate(ownerCollider.gameObject);
            UnityEngine.Object.DestroyImmediate(enemyTrigger.gameObject);
            Invoke(player, "UpdateClassicAim", 0f, 1f, 100f);
            Check(Mathf.Approximately(player.AimAngle, player.classicAimPitchLimit), "classic upward aim is clamped");
            Invoke(player, "UpdateClassicAim", 0f, -1f, 100f);
            Check(Mathf.Approximately(player.AimAngle, -player.classicAimPitchLimit), "classic downward aim is clamped");
            Invoke(player, "UpdateClassicAim", 0f, 1f, .9f);
            var classicShot = Fire();
            Check(Vector3.Dot(classicShot.transform.forward, gun.firePoint.forward) > .999f,
                "classic projectiles fire along the muzzle");
            UnityEngine.Object.DestroyImmediate(classicShot.gameObject);

            overrideZone.transform.position = player.transform.position;
            overrideZone.zonePriority = classic.zonePriority + 1;
            Tick();
            Check(player.CurrentShootingMode == ShootingMode.ShoulderAim, "overlap winner supplies the shooting mode");
            Invoke(laser, "LateUpdate");
            Check(!beam.enabled && !dot.enabled, "switching to shoulder aim hides the classic laser");
            Check(player.IsAiming, "changing zone mode preserves a raised gun");
            Check(Aim(Vector2.zero, true) && shoulder.IsActive && brain.enabled, "FPS aiming keeps Cinemachine active");
            var aimCamera = player.transform.Find("First Person Aim Camera").GetComponent<CinemachineCamera>();
            brain.ManualUpdate();
            Check(aimCamera.enabled && brain.ActiveVirtualCamera == aimCamera, "Cinemachine selects the FPS aim camera");
            Vector3 eye = player.transform.position + Vector3.up * shoulder.eyeHeight +
                player.transform.forward * shoulder.eyeForwardOffset + player.transform.right * shoulder.eyeSideOffset;
            Check(Vector3.Distance(camera.transform.position, eye) < .03f,
                "first-person aim places the camera at eye level");
            rotation = player.transform.rotation;
            Aim(new Vector2(4, 2), true);
            Check(Quaternion.Angle(rotation, player.transform.rotation) > 1, "mouse aiming still turns the character");
            Vector3 aimPoint = shoulder.GetAimPoint();
            Vector3 muzzle = gun.firePoint.position;
            Invoke(laser, "LateUpdate");
            Check(beam.enabled && Vector3.Distance(beam.GetPosition(0), muzzle) < .02f,
                "first-person laser starts at the visible muzzle");
            var shoulderShot = Fire();
            Check(Vector3.Distance(shoulderShot.transform.position, muzzle) < .02f,
                "first-person projectile uses the laser muzzle");

            Check(Vector3.Dot(shoulderShot.transform.forward, (aimPoint - muzzle).normalized) > .999f,
                "shoulder projectiles converge on the camera aim point");
            Check(Vector3.Dot(shoulderShot.transform.forward, gun.firePoint.forward) < .5f,
                "camera targeting differs from classic muzzle targeting");
            Check(gun.CurrentAmmo == gun.magSize - 2, "both modes consume the same magazine");
            UnityEngine.Object.DestroyImmediate(shoulderShot.gameObject);

            overrideZone.shootingMode = CamSwitcher.ShootingModeOverride.Classic;
            Tick();
            Check(player.CurrentShootingMode == ShootingMode.Classic && player.IsAiming,
                "editing the winning zone switches mode without lowering the gun");
            Check(!shoulder.IsActive && brain.enabled && !aimCamera.enabled,
                "switching to classic restores camera control and releases the first-person camera");
            Invoke(laser, "LateUpdate");
            Check(beam.enabled, "switching back to classic restores the laser");
            Check(Cursor.lockState == oldLock && Cursor.visible == oldCursor,
                "classic mode restores cursor state");

            overrideZone.shootingMode = CamSwitcher.ShootingModeOverride.UsePlayerDefault;
            player.defaultShootingMode = ShootingMode.Classic;
            Tick();
            Check(player.CurrentShootingMode == ShootingMode.Classic, "Use Player Default inherits classic");
            player.defaultShootingMode = ShootingMode.ShoulderAim;
            Tick();
            Check(player.CurrentShootingMode == ShootingMode.ShoulderAim, "Use Player Default inherits shoulder aiming");
            overrideZone.enabled = false;
            Tick();
            Check(rig.CurrentZone == classic && player.CurrentShootingMode == ShootingMode.Classic,
                "disabling an overlapping zone restores the remaining zone's mode");
            player.transform.position += Vector3.forward * 100;
            Tick();
            Check(rig.CurrentZone == null && player.CurrentShootingMode == ShootingMode.ShoulderAim,
                "leaving all zones restores the player default");
            Check(Aim(Vector2.zero, true), "shoulder aiming can resume after classic mode");
            Check(!Aim(Vector2.zero, false) && !shoulder.IsActive && brain.enabled,
                "losing focus safely releases shoulder aiming");
            Check(!aimCamera.enabled,
                "leaving FPS aim disables the virtual camera");

            string report = $"PASS: {results.Count} shooting-mode checks\n" + string.Join("\n", results);
            System.IO.Directory.CreateDirectory("Logs/Combat");
            System.IO.File.WriteAllText("Logs/Combat/shooting-modes.txt", report);
            Debug.Log(report);
        }
        finally
        {
            if (shoulder != null) shoulder.ReturnToExplorationImmediately();
            UnityEngine.Object.DestroyImmediate(root);
            if (originalMain != null) originalMain.tag = "MainCamera";
            Cursor.lockState = oldLock;
            Cursor.visible = oldCursor;
        }
    }

    private static GameObject Child(GameObject parent, string name)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    private static bool CaptureLaserPreview(Camera camera, LineRenderer beam)
    {
        Vector3 oldPosition = camera.transform.position;
        Quaternion oldRotation = camera.transform.rotation;
        var oldTarget = camera.targetTexture;
        var oldActive = RenderTexture.active;
        bool oldOrthographic = camera.orthographic;
        float oldSize = camera.orthographicSize;
        var oldClear = camera.clearFlags;
        Color oldBackground = camera.backgroundColor;
        var target = new RenderTexture(640, 360, 24);
        var image = new Texture2D(640, 360, TextureFormat.RGB24, false);
        try
        {
            Vector3 center = (beam.GetPosition(0) + beam.GetPosition(1)) * .5f;
            Vector3 direction = (beam.GetPosition(1) - beam.GetPosition(0)).normalized;
            camera.transform.position = center + Vector3.up * 1.5f + Vector3.Cross(direction, Vector3.up) * 5;
            camera.transform.LookAt(center);
            camera.orthographic = true;
            camera.orthographicSize = 1.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.06f, .06f, .06f);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 640, 360), 0, 0);
            image.Apply();
            System.IO.Directory.CreateDirectory("Logs/Combat");
            System.IO.File.WriteAllBytes("Logs/Combat/classic-laser.png", image.EncodeToPNG());
            return image.GetPixels().Count(c => c.r > .6f && c.r > c.g * 2 + .1f) > 8;
        }
        finally
        {
            camera.targetTexture = oldTarget;
            RenderTexture.active = oldActive;
            camera.transform.SetPositionAndRotation(oldPosition, oldRotation);
            camera.orthographic = oldOrthographic;
            camera.orthographicSize = oldSize;
            camera.clearFlags = oldClear;
            camera.backgroundColor = oldBackground;
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(image);
        }
    }

    private static CamSwitcher Zone(GameObject root, string name, CamSwitcher.ShootingModeOverride mode)
    {
        var child = Child(root, name);
        var volume = child.AddComponent<BoxCollider>();
        volume.isTrigger = true;
        volume.size = Vector3.one * 4;
        var zone = child.AddComponent<CamSwitcher>();
        zone.shootingMode = mode;
        zone.activeCamera = Child(root, name + " Camera").AddComponent<CinemachineCamera>();
        return zone;
    }

    private static void Field(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
    private static object Invoke(object target, string method, params object[] args) => target.GetType().GetMethod(method, Private).Invoke(target, args);
}
