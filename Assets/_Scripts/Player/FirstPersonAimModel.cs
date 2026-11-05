using System.Collections.Generic;
using UnityEngine;

// Reuses the visual-only inspection meshes without opening the inspection screen.
internal sealed class FirstPersonAimModel
{
    private const int ModelMask = 1 << 30;
    private readonly Dictionary<Renderer, bool> body = new();
    private Camera foreground;
    private Camera world;
    private Transform muzzle;
    private Transform pose, supportHand, magazine;
    private Vector3 supportRest, magazineRest;
    private int oldMask;

    public bool Visible => foreground != null && foreground.isActiveAndEnabled;
    public Vector3 MuzzlePosition => muzzle.position;

    public void AnimateReload(Gun gun)
    {
        if (!Visible) return;
        float t = gun != null ? gun.ReloadProgress : 0f;
        float dip = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, .22f, t)) *
            (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.72f, 1f, t)));
        float withdrawal = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.18f, .42f, t));
        float insertion = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.52f, .75f, t));
        float travel = .105f * withdrawal * insertion;
        Quaternion tilt = Quaternion.Euler(18f * dip, -9f * dip, -21f * dip);
        var pivot = new Vector3(0f, -.1f, .42f);
        pose.localPosition = pivot - tilt * pivot + new Vector3(.075f, -.025f, 0f) * dip;
        pose.localRotation = tilt;
        supportHand.localPosition = supportRest + Vector3.down * (travel + .035f * dip);
        if (magazine != null)
        {
            magazine.localPosition = magazineRest + Vector3.down * travel;
            magazine.gameObject.SetActive(t < .42f || t >= .52f);
        }
    }

    public void Show(Camera camera, Transform player)
    {
        if (Visible || (foreground == null && !Create(camera))) return;
        world = camera;
        oldMask = world.cullingMask;
        world.cullingMask &= ~ModelMask;
        foreach (var renderer in player.GetComponentsInChildren<Renderer>())
        {
            if (renderer.transform.IsChildOf(foreground.transform)) continue;
            if (renderer is not MeshRenderer && renderer is not SkinnedMeshRenderer) continue;
            body.Add(renderer, renderer.forceRenderingOff);
            renderer.forceRenderingOff = true;
        }
        foreground.CopyFrom(world);
        foreground.transform.SetParent(world.transform, false);
        foreground.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        foreground.cullingMask = ModelMask;
        foreground.clearFlags = CameraClearFlags.Depth;
        foreground.nearClipPlane = .015f;
        foreground.farClipPlane = 3f;
        foreground.depth = world.depth + 4;
        foreground.gameObject.SetActive(true);
        foreground.enabled = true;
    }

    public void Hide()
    {
        if (foreground != null)
        {
            foreground.enabled = false;
            // Other cameras can render these meshes even when their own camera is disabled.
            foreground.gameObject.SetActive(false);
        }
        if (world != null) world.cullingMask = oldMask;
        world = null;
        foreach (var pair in body)
            if (pair.Key != null) pair.Key.forceRenderingOff = pair.Value;
        body.Clear();
    }

    public void Destroy()
    {
        Hide();
        if (foreground != null) Object.Destroy(foreground.gameObject);
    }

    private bool Create(Camera camera)
    {
        var prefab = Resources.Load<InspectionViewModel>("Inspection/Hands");
        if (prefab == null || prefab.rightHand == null || prefab.leftHand == null) return false;
        var barrel = prefab.rightHand.Find("glock_barrel")?.GetComponent<MeshFilter>();
        var slide = prefab.rightHand.Find("glock_slide")?.GetComponent<MeshFilter>();
        if (barrel == null || barrel.sharedMesh == null || slide == null || slide.sharedMesh == null) return false;

        var cameraObject = new GameObject("First Person Aim Camera");
        cameraObject.transform.SetParent(camera.transform, false);
        foreground = cameraObject.AddComponent<Camera>();
        foreground.enabled = false;
        cameraObject.AddComponent<RetroScreenEffect>().enabled = false;

        var hands = Object.Instantiate(prefab, cameraObject.transform, false);
        var right = hands.rightHand;
        var left = hands.leftHand;
        // Undo the inspection pose's diagonal barrel angle to face down the sights.
        right.localRotation = Quaternion.Inverse(Quaternion.LookRotation(new Vector3(-.35f, .45f, .8f), Vector3.up)) * right.localRotation;
        right.localPosition = Vector3.zero;
        Bounds barrelBounds = CameraBounds(right.Find("glock_barrel").GetComponent<MeshFilter>(), cameraObject.transform);
        Bounds slideBounds = CameraBounds(right.Find("glock_slide").GetComponent<MeshFilter>(), cameraObject.transform);
        right.localPosition = new Vector3(-barrelBounds.center.x, -.045f - slideBounds.max.y, .40f - slideBounds.min.z);
        left.localRotation = right.localRotation * Quaternion.Euler(0, 0, -36);
        left.localPosition = right.localPosition + new Vector3(-.035f, -.015f, .035f);
        foreach (var renderer in left.GetComponentsInChildren<Renderer>())
            if (renderer.name.StartsWith("Wrist Watch") || renderer.name.StartsWith("Health Box")) renderer.enabled = false;

        muzzle = new GameObject("First Person Muzzle").transform;
        muzzle.SetParent(cameraObject.transform, false);
        muzzle.localPosition = right.localPosition + new Vector3(barrelBounds.center.x, barrelBounds.center.y - .012f, barrelBounds.max.z);

        var closeUpMaterial = Resources.Load<Material>("FirstPersonGlock");
        var weapon = new GameObject("FPS Glock Width").transform;
        weapon.SetParent(cameraObject.transform, false);
        weapon.localPosition = muzzle.localPosition;
        foreach (string part in new[] { "glock_barrel", "glock_slide", "glock_grip", "glock_magazine" })
        {
            var renderer = right.Find(part)?.GetComponent<MeshRenderer>();
            if (renderer == null) continue;
            renderer.transform.SetParent(weapon, true);
            if (closeUpMaterial != null) renderer.sharedMaterial = closeUpMaterial;
        }
        weapon.localScale = new Vector3(1.2f, 1f, 1f);
        pose = new GameObject("FPS Reload Pose").transform;
        pose.SetParent(cameraObject.transform, false);
        hands.transform.SetParent(pose, false);
        weapon.SetParent(pose, false);
        muzzle.SetParent(pose, false);
        supportHand = left;
        supportRest = left.localPosition;
        magazine = weapon.Find("glock_magazine");
        if (magazine != null) magazineRest = magazine.localPosition;
        return true;
    }

    private static Bounds CameraBounds(MeshFilter filter, Transform camera)
    {
        // Mesh bounds metadata remains available when CPU vertex data is stripped.
        Bounds local = filter.sharedMesh.bounds;
        var matrix = camera.worldToLocalMatrix * filter.transform.localToWorldMatrix;
        var bounds = new Bounds(matrix.MultiplyPoint3x4(local.center), Vector3.zero);
        Vector3 extents = local.extents;
        for (int corner = 0; corner < 8; corner++)
        {
            var offset = new Vector3((corner & 1) == 0 ? -extents.x : extents.x,
                (corner & 2) == 0 ? -extents.y : extents.y,
                (corner & 4) == 0 ? -extents.z : extents.z);
            bounds.Encapsulate(matrix.MultiplyPoint3x4(local.center + offset));
        }
        return bounds;
    }
}
