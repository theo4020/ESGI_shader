#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor.SceneManagement;
using Unity.Cinemachine;

public static class RitualSceneBuilder
{
    // Square hole half-size (so hole = 2.6 x 2.6m)
    const float HoleHalf     = 1.3f;
    const float HoleDepth    = 1.2f;
    const float LipWidth     = 0.4f;
    const float LipHeight    = 0.13f;
    // Floor extends past the circular walls
    const float FloorHalf    = 10.0f;
    // Cylindrical chamber wall
    const float WallRadius   = 9.6f;
    const float WallHeight   = 8.5f;
    const float CeilingHeight= 8.6f;
    const int   WallSegments = 48;
    // Pillars
    const float PillarRadius = 7.3f;
    const float PillarWidth  = 0.4f;
    const float PillarHeight = 7.5f;
    const int   PillarCount  = 6;

    static Material matStone;
    static Material matPillar;
    static Material matVoid;

    [MenuItem("Tools/Ritual Chamber/Build Scene")]
    static void BuildScene()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        // Always build into a dedicated scene, never the current one
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/RitualChamber.unity");

        EnsureDirectories();
        EnsureMaterials();

        var root       = new GameObject("RitualChamber_Root");
        var env        = Child("Environment",  root.transform);
        var ritualFX   = Child("RitualFX",     root.transform);
        var vfxParent  = Child("VFX",          ritualFX.transform);
        var lighting   = Child("Lighting",     root.transform);
        Child("Cameras", root.transform);
        var pp         = Child("PostProcessing", root.transform);

        BuildFloor(env.transform);
        BuildHoleLip(env.transform);
        BuildHoleWall(env.transform);
        BuildChamberWall(env.transform);
        BuildPillars(env.transform);
        BuildCeiling(env.transform);
        PlaceImportedDecor(env.transform);

        var poolGO        = BuildPoolSurface(ritualFX.transform);
        var waterDropsVFX = BuildWaterDropsVFX(vfxParent.transform);
        var circleDriver  = BuildMagicCirclePlane(ritualFX.transform);
        var dropGO        = BuildWaterDrop(ritualFX.transform);
        var eyeRing       = BuildEyeRing(ritualFX.transform);
        var hovlSparks  = BuildHovlSparks(vfxParent.transform);
        BuildHovlCircle(vfxParent.transform);
        BuildHovlSmoke(vfxParent.transform);

        SetupLighting(lighting.transform);
        SetupMainCamera();
        var camerasParent = root.transform.Find("Cameras");
        var (vcCircle, vcPool) = SetupCinemachineCameras(camerasParent);
        SetupPostProcessing(pp.transform);

        WireRitualScripts(ritualFX.transform, circleDriver, dropGO, poolGO, waterDropsVFX, eyeRing, vcCircle, vcPool, hovlSparks);

        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Ritual Chamber] Built → Assets/Scenes/RitualChamber.unity");
    }

    // ──────────────────────────────────────────────────
    //  Geometry
    // ──────────────────────────────────────────────────

    // Square floor with square hole — 8-vertex frame, normals +Y
    static void BuildFloor(Transform parent)
    {
        int   seg    = WallSegments * 2;
        float innerR = HoleHalf;
        float outerR = FloorHalf;

        var verts = new Vector3[seg * 2];
        var uvs   = new Vector2[seg * 2];
        var tris  = new int[seg * 6];

        for (int i = 0; i < seg; i++)
        {
            float a = 2f * Mathf.PI * i / seg;
            float c = Mathf.Cos(a), s = Mathf.Sin(a);
            verts[i]       = new Vector3(innerR * c, 0f, innerR * s);
            verts[seg + i] = new Vector3(outerR * c, 0f, outerR * s);
            float ti = innerR / outerR;
            uvs[i]       = new Vector2(0.5f + 0.5f * ti * c, 0.5f + 0.5f * ti * s);
            uvs[seg + i] = new Vector2(0.5f + 0.5f * c,      0.5f + 0.5f * s);
        }

        int t = 0;
        for (int i = 0; i < seg; i++)
        {
            int n = (i + 1) % seg;
            tris[t++] = i;       tris[t++] = seg + n; tris[t++] = seg + i;
            tris[t++] = i;       tris[t++] = n;       tris[t++] = seg + n;
        }

        var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.vertices = verts; mesh.uv = uvs; mesh.triangles = tris;
        mesh.RecalculateNormals();

        var go = new GameObject("Floor");
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = matStone;
    }

    // Circular raised ring framing the hole
    static void BuildHoleLip(Transform parent)
    {
        int   seg    = WallSegments * 2;
        float innerR = HoleHalf;
        float outerR = HoleHalf + LipWidth;
        float top    = LipHeight;

        var verts = new Vector3[seg * 2];
        var uvs   = new Vector2[seg * 2];
        var tris  = new int[seg * 6];

        for (int i = 0; i < seg; i++)
        {
            float a = 2f * Mathf.PI * i / seg;
            float c = Mathf.Cos(a), s = Mathf.Sin(a);
            verts[i]       = new Vector3(innerR * c, top, innerR * s);
            verts[seg + i] = new Vector3(outerR * c, top, outerR * s);
            float ti = innerR / outerR;
            uvs[i]       = new Vector2(0.5f + 0.5f * ti * c, 0.5f + 0.5f * ti * s);
            uvs[seg + i] = new Vector2(0.5f + 0.5f * c,      0.5f + 0.5f * s);
        }

        int t = 0;
        for (int i = 0; i < seg; i++)
        {
            int n = (i + 1) % seg;
            tris[t++] = i;       tris[t++] = seg + n; tris[t++] = seg + i;
            tris[t++] = i;       tris[t++] = n;       tris[t++] = seg + n;
        }

        var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.vertices = verts; mesh.uv = uvs; mesh.triangles = tris;
        mesh.RecalculateNormals();

        var go = new GameObject("HoleLip");
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = matStone;
    }

    // Cylindrical inner wall of the hole, normals inward
    static void BuildHoleWall(Transform parent)
    {
        int   seg = WallSegments * 2;
        float r   = HoleHalf;
        float bot = -HoleDepth;

        var verts = new Vector3[seg * 2];
        var uvs   = new Vector2[seg * 2];
        var tris  = new int[seg * 6];

        for (int i = 0; i < seg; i++)
        {
            float a = 2f * Mathf.PI * i / seg;
            float c = Mathf.Cos(a), s = Mathf.Sin(a);
            verts[i]       = new Vector3(r * c, 0f,  r * s);
            verts[seg + i] = new Vector3(r * c, bot, r * s);
            uvs[i]       = new Vector2((float)i / seg, 1f);
            uvs[seg + i] = new Vector2((float)i / seg, 0f);
        }

        int t = 0;
        for (int i = 0; i < seg; i++)
        {
            int n = (i + 1) % seg;
            tris[t++] = i;       tris[t++] = seg + i; tris[t++] = n;
            tris[t++] = n;       tris[t++] = seg + i; tris[t++] = seg + n;
        }

        var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.vertices = verts; mesh.uv = uvs; mesh.triangles = tris;
        mesh.RecalculateNormals();

        var go = new GameObject("HoleWall");
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = matStone;
    }

    // Cylindrical chamber wall, inner face visible
    static void BuildChamberWall(Transform parent)
    {
        var pos   = new List<Vector3>();
        var faces = new List<Face>();

        for (int i = 0; i < WallSegments; i++)
        {
            float a0 = 2f * Mathf.PI * i / WallSegments;
            float a1 = 2f * Mathf.PI * (i + 1) / WallSegments;

            // a1 → a0 order gives inward normal (toward center)
            int b = pos.Count;
            pos.Add(Xz(WallRadius, a1, 0f));
            pos.Add(Xz(WallRadius, a1, WallHeight));
            pos.Add(Xz(WallRadius, a0, WallHeight));
            pos.Add(Xz(WallRadius, a0, 0f));
            faces.Add(new Face(new[] { b, b+1, b+2, b, b+2, b+3 }));
        }

        Attach(ProBuilderMesh.Create(pos, faces), "ChamberWall", matStone, parent);
    }

    static void BuildPillars(Transform parent)
    {
        var container = Child("Pillars", parent);
        var prefab    = FindPrefab("column", "pillar", "pedestal", "Column", "Pillar", "Pedestal", "Pillar_");

        float baseW = PillarWidth * 3.2f;

        for (int i = 0; i < PillarCount; i++)
        {
            float angle = 2f * Mathf.PI * i / PillarCount;
            float x     = PillarRadius * Mathf.Cos(angle);
            float z     = PillarRadius * Mathf.Sin(angle);

            // Stone plinth at base of every pillar
            var plinth = BuildBox(new Vector3(baseW, 0.22f, baseW), matPillar, $"PillarBase_{i}");
            plinth.transform.SetParent(container.transform, false);
            plinth.transform.localPosition = new Vector3(x, 0.11f, z);

            // Stone capital at top
            var cap = BuildBox(new Vector3(baseW, 0.28f, baseW), matPillar, $"PillarCap_{i}");
            cap.transform.SetParent(container.transform, false);
            cap.transform.localPosition = new Vector3(x, PillarHeight - 0.14f, z);

            if (prefab != null)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, container.transform);
                go.name = $"Pillar_{i}";
                go.transform.localPosition = new Vector3(x, 0f, z);
                go.transform.localRotation = Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f);

                var b = GetRendererBounds(go);
                if (b.size.y > 0.1f)
                    go.transform.localScale = Vector3.one * (PillarHeight / b.size.y);

                EnsureURPMaterials(go, matPillar);
            }
            else
            {
                var pb = ShapeGenerator.GenerateCylinder(PivotLocation.FirstVertex, 8, PillarWidth, PillarHeight, 0);
                pb.gameObject.name = $"Pillar_{i}";
                pb.GetComponent<MeshRenderer>().sharedMaterial = matPillar;
                pb.transform.SetParent(container.transform, false);
                pb.transform.localPosition = new Vector3(x, 0f, z);
                pb.ToMesh();
                pb.Refresh();
            }
        }
    }

    // Solid dark ceiling — normal -Y (visible from inside)
    static void BuildCeiling(Transform parent)
    {
        var pos   = new List<Vector3>();
        var faces = new List<Face>();

        for (int i = 0; i < 24; i++)
        {
            float a0 = 2f * Mathf.PI * i / 24;
            float a1 = 2f * Mathf.PI * (i + 1) / 24;

            int b = pos.Count;
            pos.Add(new Vector3(0, CeilingHeight, 0));
            pos.Add(Xz(WallRadius, a0, CeilingHeight));
            pos.Add(Xz(WallRadius, a1, CeilingHeight));
            // center → p0 → p1 gives -Y normal
            faces.Add(new Face(new[] { b, b+1, b+2 }));
        }

        Attach(ProBuilderMesh.Create(pos, faces), "Ceiling", matVoid, parent);
    }

    // Search imported packs for decorative wall/arch/torch prefabs and place them
    static void PlaceImportedDecor(Transform parent)
    {
        var container = Child("Decorations", parent);

        var wallPrefab  = FindPrefab("wall_segment","WallSegment","dungeon_wall","DungeonWall","StoneWall","stone_wall");
        var torchPrefab = FindPrefab("torch","sconce","Torch","Candle","candle","lantern","Lantern","fire");
        var archPrefab  = FindPrefab("arch","Arch","gate","Gate","doorway","Doorway");

        // Wall segments between pillars
        if (wallPrefab != null)
        {
            for (int i = 0; i < PillarCount; i++)
            {
                float a0   = 2f * Mathf.PI * i / PillarCount;
                float a1   = 2f * Mathf.PI * (i + 1) / PillarCount;
                float aMid = (a0 + a1) * 0.5f;
                float r    = WallRadius - 0.3f;

                var go = (GameObject)PrefabUtility.InstantiatePrefab(wallPrefab, container.transform);
                go.name = $"WallDecor_{i}";
                go.transform.localPosition = Xz(r, aMid);
                go.transform.localRotation = Quaternion.Euler(0f, aMid * Mathf.Rad2Deg + 90f, 0f);
                EnsureURPMaterials(go, matStone);
            }
        }

        // Torches / candles on the floor beside each pillar
        if (torchPrefab != null)
        {
            for (int i = 0; i < PillarCount; i++)
            {
                float angle = 2f * Mathf.PI * i / PillarCount + 0.18f;
                float r     = PillarRadius - 0.65f;

                var go = (GameObject)PrefabUtility.InstantiatePrefab(torchPrefab, container.transform);
                go.name = $"Torch_{i}";
                go.transform.localPosition = new Vector3(r * Mathf.Cos(angle), 0f, r * Mathf.Sin(angle));
                go.transform.localRotation = Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f);

                // Lift so bottom of mesh sits exactly on y=0
                var b = GetRendererBounds(go);
                if (b.size.y > 0f)
                    go.transform.localPosition += Vector3.up * -b.min.y;

                EnsureURPMaterials(go, matStone);
            }
        }

        // Arch at entrance
        if (archPrefab != null)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(archPrefab, container.transform);
            go.name = "Arch_Entrance";
            go.transform.localPosition = Xz(WallRadius - 0.5f, 0f);
            go.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            EnsureURPMaterials(go, matStone);
        }
    }

    // ──────────────────────────────────────────────────
    //  Ritual FX objects (shaders assigned by user)
    // ──────────────────────────────────────────────────

    static GameObject BuildPoolSurface(Transform parent)
    {
        // Quad has 4 vertices — wave vertex displacement rocks the whole plane.
        // HighResMesh generates a 100×100 grid so individual vertices ripple correctly.
        var go = new GameObject("PoolSurface");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, -0.5f, 0f);
        // Sunk half-way into the hole — walls at ±HoleHalf clip corners from all camera angles

        var hm = go.AddComponent<HighResMesh>();
        hm.resolution = 100;
        hm.size       = HoleHalf * 2f;  // disk fits the circular hole exactly

        const string matPath = "Assets/VFX/RitualChamber/Materials/Mat_WaterWave.mat";
        var waterMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        go.GetComponent<MeshRenderer>().sharedMaterial = waterMat != null ? waterMat : matVoid;
        return go;
    }

    static UnityEngine.VFX.VisualEffect BuildWaterDropsVFX(Transform parent)
    {
        const string vfxPath = "Assets/VFX/RitualChamber/VFX/WaterDrops.vfx";
        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.VFX.VisualEffectAsset>(vfxPath);
        if (asset == null) { Debug.LogWarning("[Ritual Chamber] WaterDrops.vfx not found — skipped."); return null; }

        var go  = new GameObject("WaterDropsVFX");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        var vfx = go.AddComponent<UnityEngine.VFX.VisualEffect>();
        vfx.visualEffectAsset = asset;
        return vfx;
    }

    static MagicCircleDriver BuildMagicCirclePlane(Transform parent)
    {
        var go = Prim(PrimitiveType.Quad, "MagicCircle", parent);
        go.transform.localPosition = new Vector3(0f, 4.0f, 0f);
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        go.transform.localScale    = new Vector3(5.6f, 5.6f, 1f);
        go.GetComponent<Renderer>().sharedMaterial = GetOrCreateMagicCircleMat();
        return go.AddComponent<MagicCircleDriver>();
    }

    static GameObject BuildWaterDrop(Transform parent)
    {
        var go = Prim(PrimitiveType.Sphere, "WaterDrop", parent);
        go.transform.localPosition = new Vector3(0f, 4f, 0f);
        go.transform.localScale    = Vector3.one * 0.28f;
        go.GetComponent<Renderer>().sharedMaterial = GetOrCreateWaterDropMat();
        go.SetActive(false);
        return go;
    }

    static EyeRing BuildEyeRing(Transform parent)
    {
        var go = new GameObject("EyeRing");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        return go.AddComponent<EyeRing>();
    }

    static GameObject BuildHovlCircle(Transform parent)
    {
        const string path = "Assets/Hovl Studio/Magic effects pack/Prefabs/Magic circle.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) { Debug.LogWarning("[Ritual Chamber] Hovl Magic circle.prefab not found."); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = "HovlMagicCircle";
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 4.0f, 0f);
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        go.transform.localScale    = Vector3.one * 2.8f;
        return go;
    }

    static GameObject BuildHovlSparks(Transform parent)
    {
        const string path = "Assets/Hovl Studio/Magic effects pack/Prefabs/Sparks blue.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) { Debug.LogWarning("[Ritual Chamber] Hovl Sparks blue.prefab not found."); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = "HovlSparks";
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, -0.5f, 0f);
        go.SetActive(false);
        return go;
    }

    static GameObject BuildHovlSmoke(Transform parent)
    {
        const string path = "Assets/Hovl Studio/Magic effects pack/Prefabs/Smoke vortex.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) { Debug.LogWarning("[Ritual Chamber] Hovl Smoke vortex.prefab not found."); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = "HovlSmoke";
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        go.transform.localScale    = Vector3.one * 1.6f;
        return go;
    }

    // ──────────────────────────────────────────────────
    //  Script wiring
    // ──────────────────────────────────────────────────

    static void WireRitualScripts(Transform ritualFX,
                                   MagicCircleDriver circleDriver,
                                   GameObject dropGO,
                                   GameObject poolGO,
                                   UnityEngine.VFX.VisualEffect waterDropsVFX,
                                   EyeRing eyeRing,
                                   CinemachineCamera vcCircle,
                                   CinemachineCamera vcPool,
                                   GameObject hovlSparks)
    {
        var seq = new GameObject("Sequencer");
        seq.transform.SetParent(ritualFX, false);

        // RitualDropController
        var drop = seq.AddComponent<RitualDropController>();
        if (circleDriver != null)
        {
            var so = new UnityEditor.SerializedObject(drop);
            so.FindProperty("circleDriver").objectReferenceValue  = circleDriver;
            so.FindProperty("dropTransform").objectReferenceValue = dropGO?.transform;
            so.FindProperty("dropRenderer").objectReferenceValue  = dropGO?.GetComponent<Renderer>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Find named lights for wiring
        Light circleGlowLight = null, holeRimLight = null;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.name == "CircleGlow")   circleGlowLight = l;
            if (l.name == "HoleRimLight") holeRimLight    = l;
        }

        // MagicCircleDriver — renderer + CircleGlow light
        if (circleDriver != null)
        {
            var so = new UnityEditor.SerializedObject(circleDriver);
            so.FindProperty("circleRenderer").objectReferenceValue = circleDriver.GetComponent<Renderer>();
            if (circleGlowLight != null) so.FindProperty("circleLight").objectReferenceValue = circleGlowLight;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ImpactEventBridge
        var bridge = seq.AddComponent<ImpactEventBridge>();
        {
            var so = new UnityEditor.SerializedObject(bridge);
            so.FindProperty("dropController").objectReferenceValue = drop;
            so.FindProperty("poolRenderer").objectReferenceValue   = poolGO?.GetComponent<Renderer>();
            if (waterDropsVFX != null) so.FindProperty("waterDropsVFX").objectReferenceValue = waterDropsVFX;
            if (holeRimLight  != null) so.FindProperty("holeRimLight").objectReferenceValue  = holeRimLight;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // RitualCameraController — needs the drop controller
        var cam = Object.FindFirstObjectByType<RitualCameraController>();
        if (cam != null)
        {
            var so = new UnityEditor.SerializedObject(cam);
            so.FindProperty("dropController").objectReferenceValue = drop;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // EyeRing
        if (eyeRing != null)
        {
            var eyePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EYE ADVANCED/Prefabs/EyeAdvanced_LOD.prefab");
            var so = new UnityEditor.SerializedObject(eyeRing);
            so.FindProperty("circleDriver").objectReferenceValue   = circleDriver;
            so.FindProperty("dropController").objectReferenceValue = drop;
            so.FindProperty("dropTransform").objectReferenceValue  = dropGO?.transform;
            if (eyePrefab != null)
                so.FindProperty("eyePrefab").objectReferenceValue  = eyePrefab;
            else
                Debug.LogWarning("[Ritual Chamber] EyeAdvanced_LOD.prefab not found — assign EyeRing.eyePrefab manually.");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // RitualCameraDirector
        var director = seq.AddComponent<RitualCameraDirector>();
        {
            var so = new UnityEditor.SerializedObject(director);
            so.FindProperty("vcamCircle").objectReferenceValue   = vcCircle;
            so.FindProperty("vcamPool").objectReferenceValue     = vcPool;
            so.FindProperty("circleDriver").objectReferenceValue = circleDriver;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ImpactEventBridge — sparks
        if (hovlSparks != null)
        {
            var so = new UnityEditor.SerializedObject(bridge);
            so.FindProperty("sparksInstance").objectReferenceValue = hovlSparks;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    // ──────────────────────────────────────────────────
    //  Scene setup
    // ──────────────────────────────────────────────────

    static void SetupLighting(Transform parent)
    {
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type != LightType.Directional) continue;
            l.intensity = 0.03f;
            l.color     = new Color(0.20f, 0.22f, 0.36f);
            l.transform.SetParent(parent, false);
            l.gameObject.name = "AmbientDir";
            break;
        }

        AddLight("CircleGlow",   parent, new Vector3(0f,  0.5f, 0f), new Color(0.12f, 0.82f, 0.70f), 3.5f, 9f);
        AddLight("HoleRimLight", parent, new Vector3(0f, -0.5f, 0f), new Color(0.06f, 0.20f, 0.95f), 2.2f, 4.0f);

        for (int i = 0; i < PillarCount; i++)
        {
            float a = 2f * Mathf.PI * i / PillarCount;
            AddLight($"EyeGlow_{i}", parent,
                new Vector3(PillarRadius * Mathf.Cos(a), 2.1f, PillarRadius * Mathf.Sin(a)),
                new Color(0.9f, 0.07f, 0.04f), 0.16f, 1.5f);
        }
    }

    static void AddLight(string name, Transform parent, Vector3 pos, Color color, float intensity, float range)
    {
        var go    = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var l     = go.AddComponent<Light>();
        l.type    = LightType.Point;
        l.color   = color;
        l.intensity = intensity;
        l.range   = range;
        l.shadows = LightShadows.None;
    }

    static void SetupMainCamera()
    {
        var cam = Object.FindFirstObjectByType<Camera>();
        if (cam == null) return;
        cam.transform.position  = new Vector3(4.8f, 2.6f, 0f);
        cam.transform.LookAt(new Vector3(0f, 0.3f, 0f));
        cam.fieldOfView         = 62f;
        cam.nearClipPlane       = 0.1f;
        cam.farClipPlane        = 80f;
        cam.backgroundColor     = Color.black;

        if (!cam.GetComponent<RitualCameraController>())
            cam.gameObject.AddComponent<RitualCameraController>();
    }

    static (CinemachineCamera circle, CinemachineCamera pool) SetupCinemachineCameras(Transform cameraParent)
    {
        var mainCam = Object.FindFirstObjectByType<Camera>();
        if (mainCam != null && mainCam.GetComponent<CinemachineBrain>() == null)
        {
            var brain = mainCam.gameObject.AddComponent<CinemachineBrain>();
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 0.8f);
        }

        var goCircle = new GameObject("VCam_Circle");
        goCircle.transform.SetParent(cameraParent, false);
        goCircle.transform.position = new Vector3(3.5f, 5.2f, 2.8f);
        goCircle.transform.LookAt(new Vector3(0f, 4.0f, 0f));
        var vcCircle = goCircle.AddComponent<CinemachineCamera>();
        vcCircle.Lens.FieldOfView = 45f;
        vcCircle.Priority = 15;

        var goPool = new GameObject("VCam_Pool");
        goPool.transform.SetParent(cameraParent, false);
        goPool.transform.position = new Vector3(4.8f, 1.4f, 3.2f);
        goPool.transform.LookAt(new Vector3(0f, -0.3f, 0f));
        var vcPool = goPool.AddComponent<CinemachineCamera>();
        vcPool.Lens.FieldOfView = 38f;
        vcPool.Priority = 10;

        return (vcCircle, vcPool);
    }

    static void SetupPostProcessing(Transform parent)
    {
        var go  = new GameObject("GlobalVolume");
        go.transform.SetParent(parent, false);
        var vol = go.AddComponent<Volume>();
        vol.isGlobal = true;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();

        var bloom = profile.Add<Bloom>();
        bloom.intensity.Override(2.2f);
        bloom.threshold.Override(0.65f);
        bloom.scatter.Override(0.85f);

        var vignette = profile.Add<Vignette>();
        vignette.intensity.Override(0.60f);
        vignette.smoothness.Override(0.36f);

        var ca = profile.Add<ColorAdjustments>();
        ca.postExposure.Override(-0.15f);
        ca.saturation.Override(-28f);

        var tone = profile.Add<Tonemapping>();
        tone.mode.Override(TonemappingMode.ACES);

        var chroma = profile.Add<ChromaticAberration>();
        chroma.intensity.Override(0.18f);

        var dof = profile.Add<DepthOfField>();
        dof.mode.Override(DepthOfFieldMode.Bokeh);
        dof.focusDistance.Override(4.0f);
        dof.focalLength.Override(65f);

        const string path = "Assets/VFX/RitualChamber/RitualChamber_VolumeProfile.asset";
        if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(path) != null) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(profile, path);
        vol.sharedProfile = profile;
    }

    // ──────────────────────────────────────────────────
    //  Materials
    // ──────────────────────────────────────────────────

    static void EnsureMaterials()
    {
        var albedo = FindTexture("stone","Stone","granite","Granite","dungeon","Dungeon","rock","Rock","floor","Floor","tile","Tile");
        var normal = FindTexture("stone_n","Stone_n","stone_normal","StoneNormal","rock_n","Rock_n","granite_n","dungeon_n");

        matStone  = GetOrCreateLitMat("Mat_Stone",  new Color(0.047f, 0.040f, 0.055f), 0.88f, albedo, normal);
        matPillar = GetOrCreateLitMat("Mat_Pillar", new Color(0.068f, 0.058f, 0.080f), 0.82f, albedo, normal);
        matVoid   = GetOrCreateUnlitMat("Mat_Void", Color.black);
        GetOrCreateMagicCircleMat();
    }

    static Material GetOrCreateLitMat(string name, Color tint, float roughness,
                                       Texture2D albedo = null, Texture2D normal = null)
    {
        string path = $"Assets/VFX/RitualChamber/Materials/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", tint);
        mat.SetFloat("_Smoothness", 1f - roughness);
        mat.SetFloat("_Metallic", 0f);
        if (albedo != null) { mat.SetTexture("_BaseMap", albedo); mat.SetTextureScale("_BaseMap", new Vector2(3f, 3f)); }
        if (normal != null) { mat.SetTexture("_BumpMap", normal); mat.SetTextureScale("_BumpMap", new Vector2(3f, 3f)); mat.EnableKeyword("_NORMALMAP"); }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material GetOrCreateUnlitMat(string name, Color color)
    {
        string path = $"Assets/VFX/RitualChamber/Materials/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.color = color;
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static Material GetOrCreateMagicCircleMat()
    {
        const string path   = "Assets/VFX/RitualChamber/Materials/Mat_MagicCircle.mat";
        const string shader = "Ritual/MagicCircle";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            var s = Shader.Find(shader);
            if (s == null) { Debug.LogWarning($"[Ritual Chamber] Shader '{shader}' not found — using Unlit fallback."); s = Shader.Find("Universal Render Pipeline/Unlit"); }
            mat = new Material(s);
            AssetDatabase.CreateAsset(mat, path);
            EditorUtility.SetDirty(mat);
        }
        return mat;
    }

    static Material GetOrCreateWaterDropMat()
    {
        const string matPath    = "Assets/VFX/RitualChamber/Materials/Mat_WaterDrop.mat";
        const string shaderPath = "Assets/VFX/RitualChamber/VFX/waterDrop.shadergraph";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
            if (shader == null) { Debug.LogWarning("[Ritual Chamber] waterDrop.shadergraph not found — using Lit fallback."); shader = Shader.Find("Universal Render Pipeline/Lit"); }
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
            EditorUtility.SetDirty(mat);
        }
        return mat;
    }

    // ──────────────────────────────────────────────────
    //  ProBuilder helpers
    // ──────────────────────────────────────────────────

    // Full ProBuilder box — all 6 faces with verified winding
    static ProBuilderMesh BuildBox(Vector3 size, Material mat, string name)
    {
        float hx = size.x * .5f, hy = size.y * .5f, hz = size.z * .5f;

        var pos = new List<Vector3>
        {
            // Bottom  (-Y)
            new(-hx,-hy,-hz), new(hx,-hy,-hz), new(hx,-hy,hz),  new(-hx,-hy,hz),
            // Top     (+Y) — reversed Z to give +Y normal
            new(-hx,hy,hz),   new(hx,hy,hz),   new(hx,hy,-hz),  new(-hx,hy,-hz),
            // Front   (+Z)
            new(-hx,-hy,hz),  new(hx,-hy,hz),  new(hx,hy,hz),   new(-hx,hy,hz),
            // Back    (-Z)
            new(hx,-hy,-hz),  new(-hx,-hy,-hz), new(-hx,hy,-hz), new(hx,hy,-hz),
            // Right   (+X)
            new(hx,-hy,hz),   new(hx,-hy,-hz),  new(hx,hy,-hz),  new(hx,hy,hz),
            // Left    (-X)
            new(-hx,-hy,-hz), new(-hx,-hy,hz),  new(-hx,hy,hz),  new(-hx,hy,-hz),
        };

        var faces = new List<Face>();
        for (int f = 0; f < 6; f++)
        {
            int b = f * 4;
            faces.Add(new Face(new[] { b, b+1, b+2, b, b+2, b+3 }));
        }

        var pb = ProBuilderMesh.Create(pos, faces);
        pb.gameObject.name = name;
        pb.GetComponent<MeshRenderer>().sharedMaterial = mat;
        pb.ToMesh();
        pb.Refresh();
        return pb;
    }

    static void Attach(ProBuilderMesh pb, string name, Material mat, Transform parent)
    {
        pb.gameObject.name = name;
        pb.GetComponent<MeshRenderer>().sharedMaterial = mat;
        pb.transform.SetParent(parent, false);
        pb.ToMesh();
        pb.Refresh();
    }

    // ──────────────────────────────────────────────────
    //  Utility helpers
    // ──────────────────────────────────────────────────

    static void EnsureDirectories()
    {
        System.IO.Directory.CreateDirectory(Application.dataPath + "/Scenes");
        System.IO.Directory.CreateDirectory(Application.dataPath + "/VFX/RitualChamber/Materials");
        System.IO.Directory.CreateDirectory(Application.dataPath + "/VFX/RitualChamber/Shaders");
        System.IO.Directory.CreateDirectory(Application.dataPath + "/VFX/RitualChamber/VFX");
        AssetDatabase.Refresh();
    }

    static GameObject FindPrefab(params string[] keywords)
    {
        foreach (var kw in keywords)
        {
            foreach (var guid in AssetDatabase.FindAssets($"t:Prefab {kw}"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith("Packages/com.unity")) continue;
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go != null) return go;
            }
        }
        return null;
    }

    static Texture2D FindTexture(params string[] keywords)
    {
        foreach (var kw in keywords)
        {
            foreach (var guid in AssetDatabase.FindAssets($"t:Texture2D {kw}"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith("Packages/com.unity")) continue;
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex != null) return tex;
            }
        }
        return null;
    }

    // Replaces any non-URP material on a prefab instance with a URP fallback
    static void EnsureURPMaterials(GameObject go, Material fallback)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            var mats    = r.sharedMaterials;
            bool dirty  = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null || !mats[i].shader.name.StartsWith("Universal Render Pipeline"))
                {
                    mats[i] = fallback;
                    dirty   = true;
                }
            }
            if (dirty) r.sharedMaterials = mats;
        }
    }

    static Bounds GetRendererBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one);
        var b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        return b;
    }

    static GameObject Prim(PrimitiveType type, string name, Transform parent)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static Vector3 Xz(float radius, float angle, float y = 0f) =>
        new Vector3(radius * Mathf.Cos(angle), y, radius * Mathf.Sin(angle));

    static GameObject Child(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        return go;
    }
}
#endif
