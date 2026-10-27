#if UNITY_EDITOR
// ---------------------------------------------------------------------------
//  S15 과제 자동 생성 스크립트
//  메뉴: CG > S15 과제 자동 생성
//  - S14_Landscape -> S15/S15_Landscape 로 Save As
//  - 실습 5: 화로에서 피어오르는 연기 (Particle System + Mat_S15_Smoke)
//  - 실습 6: 테이블 상판에 흐르는 물 (Mat_S15_SG_Flow)
//  - 과제 3: 화로 안의 용암 Quad (Mat_S15_SG_Lava)
//  - Generate Lighting
// ---------------------------------------------------------------------------
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class S15Setup
{
    const string SRC_SCENE   = "Assets/S14/S14_Landscape.unity";
    const string DST_SCENE   = "Assets/S15/S15_Landscape.unity";
    const string S15_DIR     = "Assets/S15";
    const string SHADER_PATH = "Assets/S15/S15_SG_Flow.shadergraph";

    const string TEX_WATER1 = "Assets/S15/Textures/Water1.png";
    const string TEX_WATER2 = "Assets/S15/Textures/Water2.png";
    const string TEX_SMOKE  = "Assets/S15/Textures/Smoke.png";
    const string TEX_LAVA1  = "Assets/ThirdParty/Textures/Lava_Seamless.png";
    const string TEX_LAVA2  = "Assets/ThirdParty/Textures/Lava_Seamless2.png";

    // =======================================================================
    [MenuItem("CG/S15 과제 자동 생성", false, 1)]
    public static void Run()
    {
        try
        {
            EditorUtility.DisplayProgressBar("S15", "텍스처 임포트 설정...", 0.05f);
            FixTextureImport();

            EditorUtility.DisplayProgressBar("S15", "씬 준비 (Save As)...", 0.15f);
            Scene scene = PrepareScene();

            EditorUtility.DisplayProgressBar("S15", "재질 생성...", 0.3f);
            Shader flow = AssetDatabase.LoadAssetAtPath<Shader>(SHADER_PATH);
            if (flow == null)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("S15",
                    "S15_SG_Flow 셰이더를 찾지 못했습니다.\n" + SHADER_PATH + " 가 있는지 확인하세요.", "확인");
                return;
            }

            GameObject top      = FindInScene(scene, "top");
            GameObject firepit  = FindInScene(scene, "Firepit");
            GameObject firebowl = FindInScene(scene, "FireBowl");
            if (top == null || firebowl == null)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("S15",
                    "씬에서 'top' 또는 'FireBowl' 오브젝트를 찾지 못했습니다.", "확인");
                return;
            }

            // ---- 실습 6: 테이블 상판에 흐르는 물 -------------------------
            Vector3 ls = top.transform.lossyScale;
            Vector2 tiling = new Vector2(Mathf.Max(0.1f, ls.x), Mathf.Max(0.1f, ls.z));
            Material water = MakeFlowMaterial(
                S15_DIR + "/Mat_S15_SG_Flow.mat", flow,
                Load<Texture>(TEX_WATER1), Load<Texture>(TEX_WATER2),
                tiling, new Vector4(0f, 0.05f, 0f, 0f), new Vector4(0.02f, 0.1f, 0f, 0f), 0.5f);

            var topRenderer = top.GetComponent<Renderer>();
            if (topRenderer != null)
            {
                Undo.RecordObject(topRenderer, "S15 water");
                var mats = topRenderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = water;
                topRenderer.sharedMaterials = mats;
            }

            // ---- 과제 3: 화로 안의 용암 ---------------------------------
            EditorUtility.DisplayProgressBar("S15", "용암 Quad 생성...", 0.45f);
            Material lava = MakeFlowMaterial(
                S15_DIR + "/Mat_S15_SG_Lava.mat", flow,
                Load<Texture>(TEX_LAVA1), Load<Texture>(TEX_LAVA2),
                new Vector2(1.6f, 1.6f),
                new Vector4(0f, 0.018f, 0f, 0f), new Vector4(0.012f, 0.03f, 0f, 0f), 0.5f, LAVA_EMISSION);
            BuildLavaQuad(scene, firepit != null ? firepit : firebowl, firebowl, lava);

            // ---- 실습 5: 화로에서 피어오르는 연기 ------------------------
            EditorUtility.DisplayProgressBar("S15", "연기 파티클 생성...", 0.6f);
            Material smokeMat = MakeSmokeMaterial(S15_DIR + "/Mat_S15_Smoke.mat");
            BuildSmoke(scene, firebowl, smokeMat);

            // ---- 숯 Emission 을 Realtime GI 로 ---------------------------
            Material coal = Load<Material>("Assets/S14/Mat_S14_Coal.mat");
            if (coal != null)
            {
                coal.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                EditorUtility.SetDirty(coal);
            }

            // ---- Global Volume 프로파일을 S15 로 복사 --------------------
            EditorUtility.DisplayProgressBar("S15", "볼륨 프로파일 복사...", 0.7f);
            CopyVolumeProfile(scene);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            // ---- Generate Lighting --------------------------------------
            EditorUtility.DisplayProgressBar("S15", "Generate Lighting (시간이 걸립니다)...", 0.8f);
            EditorUtility.ClearProgressBar();
            Lightmapping.Bake();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("S15 과제 자동 생성 완료",
                "완료되었습니다.\n\n" +
                "· 씬: " + DST_SCENE + "\n" +
                "· 연기: FireBowl > Smoke\n" +
                "· 흐르는 물: Table > model > top (Mat_S15_SG_Flow, Tiling " +
                    tiling.x.ToString("0.##") + " x " + tiling.y.ToString("0.##") + ")\n" +
                "· 흐르는 용암: Firepit > LavaFlow (Mat_S15_SG_Lava)\n\n" +
                "Game view에서 Play 후 30~60초 녹화하세요.", "확인");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // =======================================================================
    static T Load<T>(string path) where T : Object
    {
        T t = AssetDatabase.LoadAssetAtPath<T>(path);
        if (t == null) Debug.LogWarning("[S15] 에셋을 찾지 못했습니다: " + path);
        return t;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    static void FixTextureImport()
    {
        SetTex(TEX_WATER1, false);
        SetTex(TEX_WATER2, false);
        SetTex(TEX_LAVA1, false);
        SetTex(TEX_LAVA2, false);
        SetTex(TEX_SMOKE, true);
    }

    static void SetTex(string path, bool alphaTransparency)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;
        ti.textureType = TextureImporterType.Default;
        ti.wrapMode = TextureWrapMode.Repeat;
        ti.maxTextureSize = 1024;
        ti.sRGBTexture = true;
        ti.alphaIsTransparency = alphaTransparency;
        ti.alphaSource = alphaTransparency
            ? TextureImporterAlphaSource.FromInput
            : TextureImporterAlphaSource.None;
        ti.mipmapEnabled = true;
        ti.SaveAndReimport();
    }

    static Scene PrepareScene()
    {
        EnsureFolder(S15_DIR);
        EnsureFolder(S15_DIR + "/Textures");

        if (File.Exists(DST_SCENE))
            return EditorSceneManager.OpenScene(DST_SCENE, OpenSceneMode.Single);

        Scene s = EditorSceneManager.OpenScene(SRC_SCENE, OpenSceneMode.Single);
        EditorSceneManager.SaveScene(s, DST_SCENE);
        return EditorSceneManager.OpenScene(DST_SCENE, OpenSceneMode.Single);
    }

    static GameObject FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            GameObject hit = FindRecursive(root.transform, name);
            if (hit != null) return hit;
        }
        return null;
    }

    static GameObject FindRecursive(Transform t, string name)
    {
        if (t.name == name) return t.gameObject;
        for (int i = 0; i < t.childCount; i++)
        {
            GameObject hit = FindRecursive(t.GetChild(i), name);
            if (hit != null) return hit;
        }
        return null;
    }

    // =======================================================================
    // 라이팅 재생성 없이 재질 값만 다시 적용 (용암 Emission 등)
    [MenuItem("CG/S15 재질 값만 갱신", false, 2)]
    public static void RefreshMaterials()
    {
        Shader flow = AssetDatabase.LoadAssetAtPath<Shader>(SHADER_PATH);
        if (flow == null) { Debug.LogError("[S15] 셰이더를 찾지 못했습니다."); return; }
        Scene scene = EditorSceneManager.GetActiveScene();
        GameObject top = FindInScene(scene, "top");
        Vector3 ls = top != null ? top.transform.lossyScale : new Vector3(2f, 1f, 2f);
        MakeFlowMaterial(S15_DIR + "/Mat_S15_SG_Flow.mat", flow,
            Load<Texture>(TEX_WATER1), Load<Texture>(TEX_WATER2),
            new Vector2(Mathf.Max(0.1f, ls.x), Mathf.Max(0.1f, ls.z)),
            new Vector4(0f, 0.05f, 0f, 0f), new Vector4(0.02f, 0.1f, 0f, 0f), 0.5f, 0f);
        MakeFlowMaterial(S15_DIR + "/Mat_S15_SG_Lava.mat", flow,
            Load<Texture>(TEX_LAVA1), Load<Texture>(TEX_LAVA2),
            new Vector2(1.6f, 1.6f),
            new Vector4(0f, 0.018f, 0f, 0f), new Vector4(0.012f, 0.03f, 0f, 0f), 0.5f, LAVA_EMISSION);
        AssetDatabase.SaveAssets();
        Debug.Log("[S15] 재질 값 갱신 완료 (용암 EmissionStrength = " + LAVA_EMISSION + ")");
    }

    const float LAVA_EMISSION = 3f;

    static Material MakeFlowMaterial(string path, Shader sh, Texture a, Texture b,
                                     Vector2 tiling, Vector4 flow1, Vector4 flow2, float blend,
                                     float emission = 0f)
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, path);
        }
        m.shader = sh;
        if (a != null) m.SetTexture("_MainTex1", a);
        if (b != null) m.SetTexture("_MainTex2", b);
        m.SetVector("_Tiling", new Vector4(tiling.x, tiling.y, 0f, 0f));
        m.SetVector("_Flow1", flow1);
        m.SetVector("_Flow2", flow2);
        m.SetFloat("_Blend", blend);
        m.SetFloat("_EmissionStrength", emission);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material MakeSmokeMaterial(string path)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, path);
        }
        m.shader = sh;
        Texture smoke = Load<Texture>(TEX_SMOKE);
        if (smoke != null) m.SetTexture("_BaseMap", smoke);
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Surface", 1f);                 // Transparent
        m.SetFloat("_Blend", 0f);                   // Alpha
        m.SetFloat("_AlphaClip", 0f);
        m.SetFloat("_ZWrite", 0f);
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ColorMode", 0f);               // Multiply
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.DisableKeyword("_ALPHATEST_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        EditorUtility.SetDirty(m);
        return m;
    }

    // =======================================================================
    static Bounds WorldBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.5f);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    static void BuildLavaQuad(Scene scene, GameObject parent, GameObject firebowl, Material lavaMat)
    {
        GameObject old = FindInScene(scene, "LavaFlow");
        if (old != null) Object.DestroyImmediate(old);

        Bounds b = WorldBounds(firebowl);
        GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = "LavaFlow";
        var col = q.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);

        q.transform.SetParent(parent.transform, true);
        q.transform.position = new Vector3(b.center.x, b.max.y + 0.012f, b.center.z);
        q.transform.rotation = Quaternion.Euler(90f, 0f, 0f);   // 바닥에 눕히기

        float size = Mathf.Max(0.2f, Mathf.Min(b.size.x, b.size.z) * 0.70f);
        Vector3 pls = parent.transform.lossyScale;
        q.transform.localScale = new Vector3(
            size / Mathf.Max(0.0001f, Mathf.Abs(pls.x)),
            size / Mathf.Max(0.0001f, Mathf.Abs(pls.z)),
            1f);

        q.GetComponent<Renderer>().sharedMaterial = lavaMat;
        q.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        q.isStatic = false;
        Undo.RegisterCreatedObjectUndo(q, "S15 lava");
    }

    // =======================================================================
    static void BuildSmoke(Scene scene, GameObject firebowl, Material smokeMat)
    {
        GameObject old = FindInScene(scene, "Smoke");
        if (old != null) Object.DestroyImmediate(old);

        GameObject go = new GameObject("Smoke");
        go.transform.SetParent(firebowl.transform, true);

        Bounds b = WorldBounds(firebowl);
        go.transform.position = new Vector3(b.center.x, b.max.y + 0.10f, b.center.z);
        go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);  // 로컬 Z가 월드 위쪽
        go.transform.localScale = Vector3.one;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        // ---- Main (실습 5 - 3단계) ----
        var main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(3f, 5f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startSize       = new ParticleSystem.MinMaxCurve(1f, 2f);
        main.startRotation   = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor      = new ParticleSystem.MinMaxGradient(
                                   Color.white, new Color(0.62f, 0.62f, 0.62f, 1f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode     = ParticleSystemScalingMode.Local;
        main.maxParticles    = 1000;
        main.playOnAwake     = true;

        // ---- Emission / Shape (4단계) ----
        var em = ps.emission;
        em.enabled = true;
        em.rateOverTime = 10f;

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Cone;   // Emit from: Base
        shape.angle     = 10f;
        shape.radius    = 0.01f;

        // ---- Velocity over Lifetime / Noise (5단계) ----
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        vel.x = new ParticleSystem.MinMaxCurve(0f);
        vel.y = new ParticleSystem.MinMaxCurve(0f);
        vel.z = new ParticleSystem.MinMaxCurve(0.3f);

        var noise = ps.noise;
        noise.enabled     = true;
        noise.strength    = new ParticleSystem.MinMaxCurve(0.01f, 0.2f);
        noise.frequency   = 0.5f;
        noise.scrollSpeed = new ParticleSystem.MinMaxCurve(0.2f);
        noise.damping     = true;
        noise.quality     = ParticleSystemNoiseQuality.Medium;

        // ---- Color / Size over Lifetime (6단계) ----
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1.00f, 0.56f, 0.22f), 0.00f),  // 주황빛
                new GradientColorKey(new Color(0.56f, 0.42f, 0.32f), 0.35f),  // 갈색
                new GradientColorKey(new Color(0.63f, 0.63f, 0.65f), 1.00f)   // 회색
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.00f, 0.00f),   // 투명하게 나타나
                new GradientAlphaKey(0.85f, 0.18f),
                new GradientAlphaKey(0.45f, 0.70f),
                new GradientAlphaKey(0.00f, 1.00f)    // 투명하게 사라짐
            });
        col.color = new ParticleSystem.MinMaxGradient(g);

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(new Keyframe(0f, 0.35f));
        curve.AddKey(new Keyframe(0.5f, 0.75f));
        curve.AddKey(new Keyframe(1f, 1f));
        sol.size = new ParticleSystem.MinMaxCurve(1f, curve);

        // ---- Renderer (7단계) ----
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode       = ParticleSystemRenderMode.Billboard;
        r.alignment        = ParticleSystemRenderSpace.View;
        r.sharedMaterial   = smokeMat;
        r.maxParticleSize  = 1f;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows   = false;
        r.sortMode         = ParticleSystemSortMode.Distance;

        Undo.RegisterCreatedObjectUndo(go, "S15 smoke");
    }

    // =======================================================================
    static void CopyVolumeProfile(Scene scene)
    {
        GameObject vol = FindInScene(scene, "Global Volume");
        if (vol == null) return;

        Component volume = null;
        foreach (Component c in vol.GetComponents<Component>())
            if (c != null && c.GetType().Name == "Volume") { volume = c; break; }
        if (volume == null) return;

        var so = new SerializedObject(volume);
        SerializedProperty prop = so.FindProperty("sharedProfile");
        if (prop == null || prop.objectReferenceValue == null) return;

        string src = AssetDatabase.GetAssetPath(prop.objectReferenceValue);
        if (string.IsNullOrEmpty(src) || src.StartsWith(S15_DIR)) return;

        string dst = S15_DIR + "/S15_VolumeProfile.asset";
        if (!File.Exists(dst) && !AssetDatabase.CopyAsset(src, dst)) return;
        AssetDatabase.ImportAsset(dst);

        Object copied = AssetDatabase.LoadAssetAtPath<Object>(dst);
        if (copied == null) return;
        prop.objectReferenceValue = copied;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(vol);
    }
}
#endif
