#if UNITY_EDITOR
// ---------------------------------------------------------------------------
//  S25 과제 자동 생성 / 스크린샷 저장
//  메뉴: CG > S25 과제 자동 생성   (씬 만들기 + 실드 3개 + Reflection Probe Bake + 데칼 웅덩이)
//        CG > S25 스크린샷 저장     (프로젝트 폴더/S25_Screenshots 에 PNG 저장, Assets 밖)
//
//  - S23/S23_Landscape -> S25/S25_Landscape 로 복사한 뒤 그 씬에 작업한다.
//  - 과제 2: 실드 세 개 (왼쪽부터 Original / Mission A / Mission B)
//            각 실드: Mesh Collider + Trigger_OnClickPoint -> Actor_SetImpactProperty.Actor_SetImpact
//  - 과제 3: Reflection Probe(Baked, 512, Box Projection) + 주변 물체 Reflection Probe Static + Bake
//            PC_Renderer에 Decal Renderer Feature 추가
//            바닥(Plane_Ground): Trigger_OnDragSurface(켬) -> DecalSpawner.SpawnAt / EndPath
//                               Trigger_OnClickSurface(끔, 수업 순서대로 남겨 둠)
//  - 실드를 클릭할 때 화면이 돌아가지 않도록 Player의 SceneWalker는 꺼 둔다(다시 켜도 됨).
// ---------------------------------------------------------------------------
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class S25Setup
{
    const string DIR       = "Assets/S25";
    const string SRC_SCENE = "Assets/S23/S23_Landscape.unity";
    const string DST_SCENE = DIR + "/S25_Landscape.unity";
    const string ROOT_NAME = "S25_Assignment";
    const string SHIELD    = DIR + "/Shield.fbx";
    const string PUDDLE    = DIR + "/Decal_S25_Puddle.prefab";
    const string M_MIRROR  = DIR + "/Mat_S25_Mirror.mat";
    const string MARKER    = DIR + "/.runsetup";
    const string LOGFILE   = DIR + "/_setup_log.txt";
    const string OUT_DIR   = "S25_Screenshots";
    const int W = 1920, H = 1080;

    static readonly string[] SHIELD_MATS = { DIR + "/Mat_S25_Shield_Original.mat", DIR + "/Mat_S25_Shield_MissionA.mat", DIR + "/Mat_S25_Shield_MissionB.mat" };
    static readonly string[] SHIELD_NAMES = { "Shield_Original", "Shield_MissionA", "Shield_MissionB" };
    static readonly string[] SHIELD_LABELS = { "Original", "Mission A", "Mission B" };

    static string log = "";
    static void L(string s) { log += s + "\n"; Debug.Log("[S25] " + s); }

    [DidReloadScripts]
    static void AutoRun()
    {
        if (!File.Exists(MARKER)) return;
        string mode = File.ReadAllText(MARKER).Trim();
        File.Delete(MARKER);
        EditorApplication.delayCall += () =>
        {
            if (mode != "capture") Run();
            Capture();
        };
    }

    // =====================================================================
    [MenuItem("CG/S25 과제 자동 생성", false, 1)]
    public static void Run()
    {
        log = "";
        if (!File.Exists(DST_SCENE))
        {
            AssetDatabase.CopyAsset(SRC_SCENE, DST_SCENE);
            L("씬 복사: " + SRC_SCENE + " -> " + DST_SCENE);
        }
        Scene scene = EditorSceneManager.OpenScene(DST_SCENE, OpenSceneMode.Single);

        AddDecalFeature();

        GameObject old = GameObject.Find(ROOT_NAME);
        if (old != null) Object.DestroyImmediate(old);
        Transform root = new GameObject(ROOT_NAME).transform;

        // ---- 기준: 바닥, 파빌리온 ------------------------------------------
        Collider[] grounds = Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                                   .Where(c => c.gameObject.name.StartsWith("Plane_Ground")).ToArray();
        if (grounds.Length == 0) { L("Plane_Ground를 찾지 못했습니다."); return; }
        Bounds gb = grounds[0].bounds;
        foreach (var g in grounds) gb.Encapsulate(g.bounds);
        float groundY = gb.max.y;
        GameObject pav = GameObject.Find("Pavilion");
        Vector3 center = pav != null ? pav.transform.position : gb.center;
        center.y = groundY;
        // 파빌리온(계단 포함)의 앞쪽 끝 z
        float frontZ = center.z - 3f;
        if (pav != null)
        {
            var rs = pav.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0) { Bounds pb = rs[0].bounds; foreach (var r in rs) pb.Encapsulate(r.bounds); frontZ = pb.min.z; }
        }
        Vector3 pc = new Vector3(center.x, groundY, frontZ - 1.8f);   // 웅덩이를 찍을 곳(계단 앞 빈 바닥)
        L("파빌리온 앞쪽 끝 z = " + frontZ.ToString("0.00") + ", 바닥 z 범위 " + gb.min.z.ToString("0.0") + " ~ " + gb.max.z.ToString("0.0"));

        // ---- 카메라: 파빌리온을 정면으로 보고, 실드는 그 앞 공중에 -----------------
        Camera main = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                            .FirstOrDefault(c => c.CompareTag("MainCamera"));
        Transform player = main != null && main.transform.parent != null ? main.transform.parent : (main != null ? main.transform : null);
        float camZ = Mathf.Max(frontZ - 7.5f, gb.min.z + 0.6f);
        if (player != null)
        {
            Undo.RecordObject(player, "S25");
            player.position = new Vector3(center.x, groundY + 1f, camZ);
            player.rotation = Quaternion.identity;
            var walker = player.GetComponents<MonoBehaviour>().FirstOrDefault(m => m != null && m.GetType().Name == "SceneWalker");
            if (walker != null) { walker.enabled = false; L("Player의 SceneWalker를 껐습니다 (클릭할 때 화면이 돌지 않도록)."); }
        }
        if (main != null) main.transform.localEulerAngles = new Vector3(12f, 0f, 0f);
        // Game 뷰가 Main Camera로 보이도록 다른 카메라는 끈다 (클릭 Raycast는 Main Camera 기준)
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (c != main && c.enabled) { c.enabled = false; L("카메라 끔: " + c.name); }
        Vector3 camPos = main != null ? main.transform.position : new Vector3(center.x, groundY + 1.7f, camZ);

        // ---- 과제 2: 실드 세 개 -------------------------------------------
        Mesh shieldMesh = AssetDatabase.LoadAllAssetsAtPath(SHIELD).OfType<Mesh>().FirstOrDefault();
        float scale = 1f;
        if (shieldMesh != null) { float ext = shieldMesh.bounds.size.magnitude; scale = 1.5f / Mathf.Max(ext, 0.001f); }
        Transform shields = new GameObject("S25_Shields").transform; shields.SetParent(root, false);
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        for (int i = 0; i < 3; i++)
        {
            GameObject go = new GameObject(SHIELD_NAMES[i]);
            go.transform.SetParent(shields, false);
            go.transform.position = new Vector3(camPos.x - 1.2f + (i - 1) * 1.4f, groundY + 1.55f, frontZ - 3.4f);
            go.transform.rotation = Quaternion.Euler(0f, 180f, 0f);   // 수업 씬과 같은 방향(카메라 쪽을 봄)
            go.transform.localScale = Vector3.one * scale;
            go.AddComponent<MeshFilter>().sharedMesh = shieldMesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(SHIELD_MATS[i]);
            mr.shadowCastingMode = ShadowCastingMode.Off;
            go.AddComponent<MeshCollider>().sharedMesh = shieldMesh;
            var actor = go.AddComponent<Actor_SetImpactProperty>();
            var trig = go.AddComponent<Trigger_OnClickPoint>();
            if (trig.OnClick == null) trig.OnClick = new UnityEvent<Vector3>();
            UnityEventTools.AddPersistentListener(trig.OnClick, actor.Actor_SetImpact);

            // 이름표
            GameObject label = new GameObject("Label");
            label.transform.SetParent(go.transform, false);
            label.transform.position = go.transform.position + Vector3.up * 0.95f;
            label.transform.rotation = Quaternion.identity;
            label.transform.localScale = Vector3.one / scale;
            var tm = label.AddComponent<TextMesh>();
            tm.text = SHIELD_LABELS[i]; tm.font = font; tm.fontSize = 64; tm.characterSize = 0.03f;
            tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = Color.white;
            label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }
        L("실드 3개 생성 (Original / Mission A / Mission B), 스케일 " + scale.ToString("0.###"));

        // ---- 과제 3: 데칼 웅덩이 -------------------------------------------
        GameObject spawnerGO = new GameObject("DecalSpawner");
        spawnerGO.transform.SetParent(root, false);
        var spawner = spawnerGO.AddComponent<Actor_SpawnDecal>();
        spawner.DecalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PUDDLE);
        spawner.AlignToPath = true;
        foreach (var g in grounds)
        {
            GameObject gg = g.gameObject;
            foreach (var c in gg.GetComponents<Trigger_OnClickSurface>()) Object.DestroyImmediate(c);
            foreach (var c in gg.GetComponents<Trigger_OnDragSurface>()) Object.DestroyImmediate(c);
            var click = gg.AddComponent<Trigger_OnClickSurface>();
            if (click.OnClick == null) click.OnClick = new UnityEvent<Vector3, Vector3>();
            UnityEventTools.AddPersistentListener(click.OnClick, spawner.SpawnAt);
            click.enabled = false;   // 수업 순서: 드래그를 쓰면 클릭은 끔 (드래그도 누르는 순간 하나 찍힘)
            var drag = gg.AddComponent<Trigger_OnDragSurface>();
            drag.Spacing = 0.2f;
            if (drag.OnDrag == null) drag.OnDrag = new UnityEvent<Vector3, Vector3>();
            if (drag.OnDragEnd == null) drag.OnDragEnd = new UnityEvent();
            UnityEventTools.AddPersistentListener(drag.OnDrag, spawner.SpawnAt);
            UnityEventTools.AddPersistentListener(drag.OnDragEnd, spawner.EndPath);
        }
        L("바닥 " + grounds.Length + "개에 Trigger_OnDragSurface(켬) / Trigger_OnClickSurface(끔) 연결 -> DecalSpawner");

        // ---- 반사 확인용 거울 구 ------------------------------------------
        Material mirror = AssetDatabase.LoadAssetAtPath<Material>(M_MIRROR);
        if (mirror == null)
        {
            mirror = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mirror.SetColor("_BaseColor", new Color(0.95f, 0.95f, 0.95f, 1f));
            mirror.SetFloat("_Metallic", 1f);
            mirror.SetFloat("_Smoothness", 1f);
            AssetDatabase.CreateAsset(mirror, M_MIRROR);
        }
        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "S25_MirrorBall";
        ball.transform.SetParent(root, false);
        ball.transform.localScale = Vector3.one * 0.6f;
        ball.transform.position = pc + new Vector3(1.7f, 0.3f, 0.2f);
        ball.GetComponent<MeshRenderer>().sharedMaterial = mirror;

        // ---- Reflection Probe ------------------------------------------
        GameObject probeGO = new GameObject("S25_ReflectionProbe");
        probeGO.transform.SetParent(root, false);
        probeGO.transform.position = pc + new Vector3(0f, 1.2f, 0f);
        var probe = probeGO.AddComponent<ReflectionProbe>();
        probe.mode = ReflectionProbeMode.Baked;
        probe.resolution = 512;
        probe.boxProjection = true;
        // 상자가 바닥 아래까지 덮어야 바닥(Plane_Ground)이 이 프로브를 씀 (바닥의 중심이 상자 안에 들어와야 함)
        probe.size = new Vector3(18f, 11f, 18f);
        probe.center = new Vector3(0f, 2.2f, 4f);   // 파빌리온 쪽으로 더
        probe.blendDistance = 0.5f;
        probe.importance = 10;

        // 반사될 물체: 바닥과 장면의 물체 (S25 실드·거울 구·Player는 빼고)
        int n = 0;
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (r.transform.IsChildOf(root)) continue;
            if (player != null && r.transform.IsChildOf(player)) continue;
            if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
            var flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
            if ((flags & StaticEditorFlags.ReflectionProbeStatic) == 0)
            {
                GameObjectUtility.SetStaticEditorFlags(r.gameObject, flags | StaticEditorFlags.ReflectionProbeStatic);
                n++;
            }
        }
        L("Reflection Probe Static 표시: " + n + "개");

        BuildBilinearMeshes();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        // 이 씬은 S23 씬을 복사한 것이라 S23의 Lighting Data를 같이 가리킨다. 끊고 S25 전용으로 새로 굽는다.
        // (Baked GI는 꺼져 있어 라이트맵은 굽지 않고, 반사 프로브·환경광만 빠르게 구워짐)
        Lightmapping.lightingDataAsset = null;
        bool baked = Lightmapping.Bake();
        L("Reflection Probe Bake: " + (baked ? "완료" : "실패"));
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        File.WriteAllText(LOGFILE, "S25 자동 생성 완료\n\n" + log);
    }

    // PC_Renderer, Mobile_Renderer에 Decal Renderer Feature가 없으면 추가
    static void AddDecalFeature()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:UniversalRendererData", new[] { "Assets/Settings" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            if (data == null) continue;
            if (data.rendererFeatures.Any(f => f is DecalRendererFeature)) { L("Decal 기능 이미 있음: " + path); continue; }
            var feat = ScriptableObject.CreateInstance<DecalRendererFeature>();
            feat.name = "Decal";
            AssetDatabase.AddObjectToAsset(feat, data);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feat, out string _, out long localId);
            var so = new SerializedObject(data);
            var list = so.FindProperty("m_RendererFeatures");
            var map = so.FindProperty("m_RendererFeatureMap");
            list.arraySize++; list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = feat;
            map.arraySize++;  map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
            so.ApplyModifiedProperties();
            data.SetDirty();
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            L("Decal Renderer Feature 추가: " + path);
        }
    }

    // S23 bilinear patch 메시는 Update에서 계산되므로, Bake·촬영 전에 한 번씩 만든다
    static void BuildBilinearMeshes()
    {
        System.Type t = System.AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("S23_BilinearPatch_Finish")).FirstOrDefault(x => x != null);
        if (t == null) return;
        var m = t.GetMethod("BuildMesh", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (m == null) return;
        foreach (var o in Object.FindObjectsByType(t, FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (((Behaviour)o).isActiveAndEnabled) m.Invoke(o, null);
    }

    // =====================================================================
    [MenuItem("CG/S25 스크린샷 저장", false, 2)]
    public static void Capture()
    {
        log = "";
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != DST_SCENE) scene = EditorSceneManager.OpenScene(DST_SCENE, OpenSceneMode.Single);
        GameObject rootGO = GameObject.Find(ROOT_NAME);
        if (rootGO == null) { Debug.LogError("[S25] 먼저 CG > S25 과제 자동 생성을 실행하세요."); return; }
        Transform root = rootGO.transform;
        BuildBilinearMeshes();
        Directory.CreateDirectory(OUT_DIR);

        Camera main = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                            .FirstOrDefault(c => c.CompareTag("MainCamera"));
        Vector3 camPos = main.transform.position;
        float groundY = root.Find("S25_MirrorBall").position.y - 0.3f;
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (c != main && c.enabled) c.enabled = false;

        // 촬영용 임시 웅덩이: 드래그와 같은 함수(SpawnAt)로 경로를 따라 찍는다. 촬영 후 지움
        var spawner = root.Find("DecalSpawner").GetComponent<Actor_SpawnDecal>();
        var before = new System.Collections.Generic.HashSet<GameObject>(Object.FindObjectsByType<DecalProjector>(FindObjectsSortMode.None).Select(d => d.gameObject));
        Vector3 pc = root.Find("S25_ReflectionProbe").position; pc.y = groundY;   // 웅덩이 가운데(프로브 바로 아래)
        for (int i = -4; i <= 4; i++)
        {
            float x = i * 0.2f;
            spawner.SpawnAt(pc + new Vector3(x, 0f, 0.25f * Mathf.Sin(i * 0.7f)), Vector3.up);
        }
        spawner.EndPath();
        for (int i = -2; i <= 2; i++) spawner.SpawnAt(pc + new Vector3(i * 0.22f, 0f, 0.45f), Vector3.up);
        spawner.EndPath();
        var temp = Object.FindObjectsByType<DecalProjector>(FindObjectsSortMode.None).Select(d => d.gameObject).Where(g => !before.Contains(g)).ToList();

        L(Shot(main, "00_MainCamera_실드와_바닥.png"));
        // 낮은 각도: 웅덩이 앞쪽 바닥 가까이에서 파빌리온 쪽을 봄
        L(TempShot("01_낮은각도_웅덩이_반사.png", pc + new Vector3(0f, 0.35f, -2.2f), pc + new Vector3(0f, 0.05f, 2.5f), 45f, main));
        L(TempShot("02_웅덩이_더_가까이.png",     pc + new Vector3(-0.4f, 0.22f, -1.3f), pc + new Vector3(0.2f, 0.0f, 1.5f), 50f, main));
        // 프로브 한계: 웅덩이 건너편(파빌리온 쪽)에서 실드 쪽을 돌아봄. 실드는 바로 위에 떠 있지만 웅덩이에 비치지 않음
        L(TempShot("03_프로브_한계_실드가_안_비침.png", pc + new Vector3(0.3f, 0.32f, 1.7f), pc + new Vector3(0f, 0.75f, -2.4f), 60f, main));
        foreach (var g in temp) Object.DestroyImmediate(g);
        File.WriteAllText(Path.Combine(OUT_DIR, "_capture_log.txt"), log);
        Debug.Log("[S25] 스크린샷 저장 완료\n" + log);
    }

    static string Shot(Camera cam, string file) { return Render(cam, file); }

    static string TempShot(string file, Vector3 pos, Vector3 target, float fov, Camera src)
    {
        GameObject go = new GameObject("S25_TempCaptureCamera");
        go.hideFlags = HideFlags.DontSave;
        Camera cam = go.AddComponent<Camera>();
        if (src != null) { cam.clearFlags = src.clearFlags; cam.backgroundColor = src.backgroundColor; }
        cam.fieldOfView = fov;
        cam.nearClipPlane = 0.03f;
        var data = go.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = true;
        data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        go.transform.position = pos;
        go.transform.rotation = Quaternion.LookRotation(target - pos, Vector3.up);
        string res = Render(cam, file);
        Object.DestroyImmediate(go);
        return res;
    }

    static string Render(Camera cam, string file)
    {
        if (cam == null) return "카메라 없음: " + file;
        RenderTexture rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4;
        RenderTexture prevTarget = cam.targetTexture;
        RenderTexture prevActive = RenderTexture.active;
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        cam.targetTexture = prevTarget;
        RenderTexture.active = prevActive;
        string path = Path.Combine(OUT_DIR, file);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        rt.Release(); Object.DestroyImmediate(rt);
        return "저장: " + path;
    }
}
#endif
