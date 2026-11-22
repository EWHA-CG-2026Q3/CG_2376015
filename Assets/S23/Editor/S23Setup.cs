#if UNITY_EDITOR
// ---------------------------------------------------------------------------
//  S23 과제 자동 생성 스크립트
//  메뉴: CG > S23 과제 자동 생성
//  - S17_Landscape -> S23/S23_Landscape 로 Save As (이미 있으면 그 씬을 열어 다시 만듦)
//  - 과제 2: Spline Extrude로 덩굴 줄기 (실습 1과 같은 경로, 같은 값)
//  - 과제 3: bilinear patch 나뭇잎 + 텍스처 (실습 2의 잎 하나 + 줄기를 따라 잎 여러 장)
//  - 과제 4: Pavilion_Roof를 끄고 bilinear patch 캐노피로 교체 (Render Face: Both)
//  - 과제 5: 고래 두 마리
//            왼쪽 = S20 재질 세 개를 Mesh Renderer에 겹쳐 올린 것 (비교용)
//            오른쪽 = 합친 셰이더 CG/S23_ToonOutlineVertexColor 재질 하나
//  - 카메라: S23_ShotCamera(전체), S23_UnderRoofCamera(지붕 아래), S23_ShaderCamera(고래)
//
//  좌표는 교수자 Finish 씬의 값을 그대로 쓴다. 교수자 씬의 Pavilion은 원점에 있으므로,
//  루트(S23_Assignment)를 우리 씬의 Pavilion 위치·회전에 맞추고 자식에 그 값을 넣는다.
// ---------------------------------------------------------------------------
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;

public static class S23Setup
{
    const string SRC_SCENE = "Assets/S17/S17_Landscape.unity";
    const string DST_SCENE = "Assets/S23/S23_Landscape.unity";
    const string DIR       = "Assets/S23";

    const string PAVILION_PREFAB = "Assets/ThirdParty/IGBlocks/Pavilion/Prefabs/Pavilion.prefab";
    const string ROOF_SRC        = "Assets/ThirdParty/IGBlocks/Pavilion/Models/Materials/Roof.mat";

    const string M_ROOF   = DIR + "/Mat_S23_Roof.mat";
    const string M_LEAF   = DIR + "/Mat_S23_Leaf.mat";
    const string M_STEM   = DIR + "/Mat_S23_Spline.mat";
    const string SH_MERGE = DIR + "/S23_ToonOutlineVertexColor.shader";
    const string M_MERGE  = DIR + "/Mat_S23_ToonOutlineVertexColor.mat";
    const string M_TOON   = DIR + "/S20_Shaders/Mat_S20_Toon_Finish.mat";
    const string M_OUTL   = DIR + "/S20_Shaders/Mat_S20_Outline_Finish.mat";
    const string M_VCOL   = DIR + "/S20_Shaders/Mat_S20_VertexColor_Finish.mat";
    const string WHALE    = DIR + "/Models/smeerws_2018-03-03_09-02-26wal.fbx";

    const string ROOT_NAME = "S23_Assignment";
    const float  WHALE_YAW = 60f;   // 고래가 카메라 쪽으로 옆모습을 보이게 돌리는 각도

    const string MARKER  = "Assets/S23/.runsetup";
    const string LOGFILE = "Assets/S23/_setup_log.txt";
    static bool silent = false;

    static void Say(string title, string body)
    {
        File.WriteAllText(LOGFILE, title + "\n\n" + body);
        if (silent) Debug.Log("[S23] " + title + "\n" + body);
        else        EditorUtility.DisplayDialog(title, body, "확인");
    }

    // 마커 파일이 있으면 스크립트 리로드 직후 자동으로 한 번 실행한다.
    [DidReloadScripts]
    static void AutoRun()
    {
        if (!File.Exists(MARKER)) return;
        File.Delete(MARKER);
        EditorApplication.delayCall += () => { silent = true; Run(); silent = false; };
    }

    // =====================================================================
    [MenuItem("CG/S23 과제 자동 생성", false, 1)]
    public static void Run()
    {
        try
        {
            EditorUtility.DisplayProgressBar("S23", "씬 준비...", 0.05f);
            Scene scene = PrepareScene();

            GameObject pav = FindPavilion(scene);
            if (pav == null)
            {
                EditorUtility.ClearProgressBar();
                Say("S23 오류", "씬에서 Pavilion 프리팹을 찾지 못했습니다.");
                return;
            }

            EditorUtility.DisplayProgressBar("S23", "재질 준비...", 0.15f);
            Materials mats = PrepareMaterials();
            if (mats == null) return;

            // 다시 실행해도 안전하게: 지난번에 만든 것은 지우고 새로 만든다
            GameObject old = FindInScene(scene, ROOT_NAME);
            if (old != null) Object.DestroyImmediate(old);

            GameObject root = new GameObject(ROOT_NAME);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.SetPositionAndRotation(pav.transform.position, pav.transform.rotation);

            EditorUtility.DisplayProgressBar("S23", "덩굴 줄기 (Spline Extrude)...", 0.3f);
            SplineContainer vine = BuildVine(root.transform, mats.stem);

            EditorUtility.DisplayProgressBar("S23", "나뭇잎 (bilinear patch)...", 0.45f);
            BuildLeaves(root.transform, vine, mats.leaf);

            EditorUtility.DisplayProgressBar("S23", "캐노피 지붕 (bilinear patch)...", 0.6f);
            BuildCanopy(root.transform, pav, mats.roof);

            EditorUtility.DisplayProgressBar("S23", "셰이더 비교용 고래...", 0.75f);
            string whaleNote = BuildWhales(root.transform, mats);

            EditorUtility.DisplayProgressBar("S23", "카메라...", 0.85f);
            BuildCameras(root.transform, scene);

            EditorUtility.DisplayProgressBar("S23", "씬 저장...", 0.95f);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, DST_SCENE);
            AssetDatabase.SaveAssets();

            EditorUtility.ClearProgressBar();
            Say("S23 완료",
                "S23 과제 자동 생성이 끝났습니다.\n\n" +
                "씬: " + DST_SCENE + "\n" +
                "Hierarchy > " + ROOT_NAME + " 아래에 덩굴(S23_Vine), 나뭇잎(S23_Leaves),\n" +
                "캐노피(S23_Canopy), 고래 두 마리(S23_Whales), 카메라 세 대가 있습니다.\n" +
                "Pavilion_Roof는 꺼 두었습니다.\n\n" + whaleNote);
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogException(e);
            Say("S23 오류", e.ToString());
        }
    }

    // =====================================================================
    //  씬
    // =====================================================================
    static Scene PrepareScene()
    {
        Scene scene;
        if (File.Exists(DST_SCENE))
        {
            scene = EditorSceneManager.OpenScene(DST_SCENE, OpenSceneMode.Single);
        }
        else
        {
            scene = EditorSceneManager.OpenScene(SRC_SCENE, OpenSceneMode.Single);
            EditorSceneManager.SaveScene(scene, DST_SCENE);
            scene = EditorSceneManager.GetActiveScene();
        }
        return scene;
    }

    static GameObject FindPavilion(Scene scene)
    {
        foreach (GameObject r in scene.GetRootGameObjects())
            foreach (Transform t in r.GetComponentsInChildren<Transform>(true))
            {
                GameObject go = t.gameObject;
                if (!PrefabUtility.IsOutermostPrefabInstanceRoot(go)) continue;
                if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go) == PAVILION_PREFAB)
                    return go;
            }
        return null;
    }

    // =====================================================================
    //  재질
    // =====================================================================
    class Materials { public Material roof, leaf, stem, merged, toon, outline, vcolor; }

    static Materials PrepareMaterials()
    {
        Materials m = new Materials();

        // 지붕: 파빌리온의 Roof 재질을 복제해서 Render Face를 Both로 (위·아래 모두 보이게)
        m.roof = AssetDatabase.LoadAssetAtPath<Material>(M_ROOF);
        if (m.roof == null)
        {
            AssetDatabase.CopyAsset(ROOF_SRC, M_ROOF);
            m.roof = AssetDatabase.LoadAssetAtPath<Material>(M_ROOF);
        }
        if (m.roof != null)
        {
            m.roof.SetFloat("_Cull", 0f);          // 0 = Off = Render Face: Both
            m.roof.doubleSidedGI = true;
            EditorUtility.SetDirty(m.roof);
        }

        m.leaf    = AssetDatabase.LoadAssetAtPath<Material>(M_LEAF);
        m.stem    = AssetDatabase.LoadAssetAtPath<Material>(M_STEM);
        m.toon    = AssetDatabase.LoadAssetAtPath<Material>(M_TOON);
        m.outline = AssetDatabase.LoadAssetAtPath<Material>(M_OUTL);
        m.vcolor  = AssetDatabase.LoadAssetAtPath<Material>(M_VCOL);

        // 나뭇잎: Render Face Both (실습 2의 5번)
        if (m.leaf != null) { m.leaf.SetFloat("_Cull", 0f); m.leaf.doubleSidedGI = true; EditorUtility.SetDirty(m.leaf); }

        // 합친 셰이더의 재질
        Shader merged = AssetDatabase.LoadAssetAtPath<Shader>(SH_MERGE);
        if (merged == null)
        {
            EditorUtility.ClearProgressBar();
            Say("S23 오류", "합친 셰이더를 찾지 못했습니다: " + SH_MERGE);
            return null;
        }
        m.merged = AssetDatabase.LoadAssetAtPath<Material>(M_MERGE);
        if (m.merged == null)
        {
            m.merged = new Material(merged);
            AssetDatabase.CreateAsset(m.merged, M_MERGE);
        }
        else m.merged.shader = merged;
        EditorUtility.SetDirty(m.merged);

        string missing = "";
        if (m.roof == null)    missing += "\n" + M_ROOF;
        if (m.leaf == null)    missing += "\n" + M_LEAF;
        if (m.stem == null)    missing += "\n" + M_STEM;
        if (m.toon == null)    missing += "\n" + M_TOON;
        if (m.outline == null) missing += "\n" + M_OUTL;
        if (m.vcolor == null)  missing += "\n" + M_VCOL;
        if (missing.Length > 0)
        {
            EditorUtility.ClearProgressBar();
            Say("S23 오류", "재질을 찾지 못했습니다:" + missing);
            return null;
        }
        AssetDatabase.SaveAssets();
        return m;
    }

    // =====================================================================
    //  과제 2: 덩굴 줄기 (Spline Extrude)
    // =====================================================================
    // 교수자 S23_SplineExtrude_Stem_Finish의 Spline (Pavilion 기준 좌표)
    static readonly float3[] KNOT_POS =
    {
        new float3(-0.58120704f, 0.0000005f,  -0.003376007f),
        new float3(-0.65220714f, 1.2540004f,  -0.18737602f),
        new float3(-0.67732215f, 1.8391558f,   0.00412941f),
        new float3(-0.65220714f, 2.6030004f,   0.2666241f),
        new float3(-0.32120705f, 4.1090007f,  -0.020375967f),
        new float3( 1.4266704f,  4.301335f,    0.3308016f),
    };
    static readonly float[] KNOT_TIN  = { -0.12694144f, -0.58358556f, -0.20551968f, -0.30201054f, -0.43103692f, -0.1793152f };
    static readonly float[] KNOT_TOUT = {  0.12694144f,  0.20929274f,  0.27817222f,  0.42075306f,  0.46088296f,  0.1793152f };
    static readonly quaternion[] KNOT_ROT =
    {
        new quaternion( 0.58413345f, -0.4808976f, -0.44345167f, -0.4804958f),
        new quaternion(-0.50659734f,  0.4264101f,  0.50751364f,  0.5513288f),
        new quaternion(-0.4114531f,   0.34895855f, 0.56247497f,  0.62654305f),
        new quaternion(-0.4783172f,   0.4841356f,  0.45214063f,  0.57653636f),
        new quaternion(-0.2661582f,   0.65202034f, 0.24034983f,  0.66802794f),
        new quaternion( 0.017409956f, 0.6338573f,  0.10564883f,  0.7660028f),
    };
    // 0 = AutoSmooth, 4 = Broken
    static readonly TangentMode[] KNOT_MODE =
    {
        TangentMode.AutoSmooth, TangentMode.Broken, TangentMode.Broken,
        TangentMode.AutoSmooth, TangentMode.AutoSmooth, TangentMode.AutoSmooth,
    };

    static SplineContainer BuildVine(Transform root, Material stemMat)
    {
        GameObject go = new GameObject("S23_Vine");
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3(-2.438793f, 0f, 1.883376f);   // 기둥 하나 옆

        // 1~3. Spline Container를 만들고 Knot을 찍음
        SplineContainer container = go.AddComponent<SplineContainer>();
        Spline spline = container.Spline;
        spline.Clear();
        for (int i = 0; i < KNOT_POS.Length; i++)
        {
            BezierKnot k = new BezierKnot(KNOT_POS[i],
                                          new float3(0, 0, KNOT_TIN[i]),
                                          new float3(0, 0, KNOT_TOUT[i]),
                                          KNOT_ROT[i]);
            spline.Add(k, KNOT_MODE[i]);
        }
        spline.Closed = false;

        // 2~7. Spline Extrude: 경로를 따라 원형 단면을 밀어 관 모양 메시를 만듦
        MeshFilter mf = go.GetComponent<MeshFilter>();
        if (mf == null) mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr == null) mr = go.AddComponent<MeshRenderer>();

        SplineExtrude ex = go.AddComponent<SplineExtrude>();
        if (ex.targetMesh == null)
        {
            Mesh mesh = new Mesh { name = "S23_VineMesh" };
            ex.targetMesh = mesh;
        }
        mf.sharedMesh      = ex.targetMesh;
        ex.Container       = container;   // 3. Container에 Spline 연결
        ex.Radius          = 0.03f;       // 4. 줄기 굵기
        ex.Sides           = 8;           // 5. 단면 다각형의 변 수
        ex.SegmentsPerUnit = 4f;
        ex.Capped          = true;        // 6. 끝을 막음
        ex.Range           = new Vector2(0f, 1f);
        ex.RebuildOnSplineChange = true;
        ex.Rebuild();

        mr.sharedMaterial = stemMat;      // 7. Material 지정
        return container;
    }

    // =====================================================================
    //  과제 3: 나뭇잎 (bilinear patch + 텍스처)
    // =====================================================================
    // 교수자 Leaf 씬의 네 코너 (Corners 기준 로컬 좌표). 잎 텍스처의 꼭지가 (u,v)=(0,0) 쪽이라
    // Corner00이 줄기에 붙는 자리, Corner11이 잎 끝이다.
    static readonly Vector3 C00 = new Vector3(-0.199f,  0.123f, -0.535f);
    static readonly Vector3 C10 = new Vector3(-1.315f,  0.506f, -0.768f);
    static readonly Vector3 C01 = new Vector3(-0.240f,  0.451f,  0.268f);
    static readonly Vector3 C11 = new Vector3(-1.207f, -0.420f,  0.395f);

    static GameObject MakeLeaf(Transform parent, string name, Material leafMat, int segs)
    {
        // 1. 빈 GameObject 4개 (Corner00~11)
        // 3. 새 GameObject(BilinearPatch)에 S23_BilinearPatch_Finish 추가
        GameObject patch = new GameObject(name);
        patch.transform.SetParent(parent, false);
        MeshFilter mf = patch.AddComponent<MeshFilter>();
        MeshRenderer mr = patch.AddComponent<MeshRenderer>();

        GameObject corners = new GameObject("Corners");
        corners.transform.SetParent(patch.transform, false);
        Transform c00 = MakeCorner(corners.transform, "Corner00", C00);
        Transform c10 = MakeCorner(corners.transform, "Corner10", C10);
        Transform c01 = MakeCorner(corners.transform, "Corner01", C01);
        Transform c11 = MakeCorner(corners.transform, "Corner11", C11);

        // 4. 코너 슬롯 연결 (컴포넌트를 붙이기 전에 값부터 정해 두려고 비활성화 상태에서 붙임)
        patch.SetActive(false);
        S23_BilinearPatch_Finish bp = patch.AddComponent<S23_BilinearPatch_Finish>();
        bp.corner00 = c00; bp.corner10 = c10; bp.corner01 = c01; bp.corner11 = c11;
        bp.uSegments = segs; bp.vSegments = segs;
        patch.SetActive(true);

        // 5. Material 지정 (Render Face Both는 재질에서 설정함)
        mr.sharedMaterial = leafMat;
        return patch;
    }

    static Transform MakeCorner(Transform parent, string name, Vector3 local)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = local;
        return g.transform;
    }

    static void BuildLeaves(Transform root, SplineContainer vine, Material leafMat)
    {
        GameObject leaves = new GameObject("S23_Leaves");
        leaves.transform.SetParent(root, false);

        // (1) 실습 2와 똑같은 잎 한 장 (교수자 Finish 씬의 값 그대로)
        GameObject leaf0 = new GameObject("BilinearLeaf");
        leaf0.transform.SetParent(leaves.transform, false);
        leaf0.transform.localPosition = new Vector3(-3.27f, 0.383f, 1.877f);
        leaf0.transform.localRotation = new Quaternion(0f, 0.5254717f, 0f, -0.8508111f);
        GameObject p0 = MakeLeaf(leaf0.transform, "BilinearPatch", leafMat, 6);
        p0.transform.localPosition = new Vector3(0.035f, 0.726f, 0.004f);
        p0.transform.localScale    = Vector3.one * 0.5f;

        // (2) 줄기를 따라 잎을 여러 장 더: 줄기 위의 t 지점마다 잎 꼭지(Corner00)를 붙인다
        //     잎이 뻗는 방향(코너 로컬): Corner00에서 네 코너의 가운데로 가는 방향
        Vector3 cAvg = (C00 + C10 + C01 + C11) * 0.25f;
        Vector3 leafDir = cAvg - C00; leafDir.y = 0f; leafDir.Normalize();

        Vector3 pavCenter = root.TransformPoint(new Vector3(0f, 0f, 2f));   // 지붕 가운데 아래
        float[] ts     = { 0.10f, 0.20f, 0.30f, 0.40f, 0.50f, 0.60f, 0.70f, 0.80f, 0.90f };
        float[] scales = { 0.42f, 0.50f, 0.46f, 0.55f, 0.48f, 0.52f, 0.45f, 0.50f, 0.40f };

        for (int i = 0; i < ts.Length; i++)
        {
            Vector3 p = (Vector3)vine.EvaluatePosition(ts[i]);   // 줄기 위의 한 자리 (월드)

            // 파빌리온 가운데에서 바깥쪽으로, 좌우 번갈아 55°씩 틀어서 뻗게 한다
            Vector3 outH = p - pavCenter; outH.y = 0f;
            if (outH.sqrMagnitude < 1e-4f) outH = -root.forward;
            outH.Normalize();
            float side = (i % 2 == 0) ? 55f : -55f;
            outH = Quaternion.AngleAxis(side, Vector3.up) * outH;

            GameObject leafRoot = new GameObject("Leaf_" + (i + 1).ToString("00"));
            leafRoot.transform.SetParent(leaves.transform, true);
            leafRoot.transform.position = p;
            leafRoot.transform.rotation = Quaternion.FromToRotation(leafDir, outH)
                                        * Quaternion.Euler(0f, 0f, (i % 3 - 1) * 12f); // 살짝씩 다르게 기울임

            GameObject patch = MakeLeaf(leafRoot.transform, "BilinearPatch", leafMat, 6);
            float s = scales[i];
            patch.transform.localScale    = Vector3.one * s;
            patch.transform.localPosition = -C00 * s;      // 꼭지(Corner00)가 줄기 위에 오도록
        }
    }

    // =====================================================================
    //  과제 4: 캐노피 지붕 (bilinear patch)
    // =====================================================================
    static void BuildCanopy(Transform root, GameObject pav, Material roofMat)
    {
        // 원래 지붕 끄기
        Transform oldRoof = FindDeep(pav.transform, "Pavilion_Roof");
        if (oldRoof != null)
        {
            oldRoof.gameObject.SetActive(false);
            EditorUtility.SetDirty(oldRoof.gameObject);
            PrefabUtility.RecordPrefabInstancePropertyModifications(oldRoof.gameObject);
        }

        GameObject canopy = new GameObject("S23_Canopy");
        canopy.transform.SetParent(root, false);
        canopy.transform.localPosition = new Vector3(0f, 4.7f, 2f);   // 지붕 자리

        canopy.AddComponent<MeshFilter>();
        MeshRenderer mr = canopy.AddComponent<MeshRenderer>();

        // 네 코너를 45° 돌리고 2배로: 마주 보는 두 모서리는 올라가고 다른 두 모서리는 내려간
        // 말안장(쌍곡 포물면) 모양의 캐노피가 된다
        GameObject corners = new GameObject("Corners");
        corners.transform.SetParent(canopy.transform, false);
        corners.transform.localRotation = Quaternion.Euler(0f, -45f, 0f);
        corners.transform.localScale    = Vector3.one * 2f;
        Transform c00 = MakeCorner(corners.transform, "Corner00", new Vector3(-2f,  1f, -2f));
        Transform c10 = MakeCorner(corners.transform, "Corner10", new Vector3( 2f, -1f, -2f));
        Transform c01 = MakeCorner(corners.transform, "Corner01", new Vector3(-2f, -1f,  2f));
        Transform c11 = MakeCorner(corners.transform, "Corner11", new Vector3( 2f,  1f,  2f));

        canopy.SetActive(false);
        S23_BilinearPatch_Finish bp = canopy.AddComponent<S23_BilinearPatch_Finish>();
        bp.corner00 = c00; bp.corner10 = c10; bp.corner01 = c01; bp.corner11 = c11;
        bp.uSegments = 8; bp.vSegments = 8;
        canopy.SetActive(true);

        mr.sharedMaterial = roofMat;   // Render Face: Both → 위에서도 아래에서도 보임
    }

    // =====================================================================
    //  과제 5: 셰이더 비교용 고래
    // =====================================================================
    static string BuildWhales(Transform root, Materials m)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(WHALE);
        if (model == null) return "(고래 모델을 찾지 못해 과제 5의 비교용 오브젝트는 만들지 않았습니다)";

        GameObject group = new GameObject("S23_Whales");
        group.transform.SetParent(root, false);

        GameObject a = MakeWhale(group.transform, model, "Whale_Stacked_3Materials", new Vector3(3.6f, 1.6f, -3.8f));
        GameObject b = MakeWhale(group.transform, model, "Whale_Merged_1Material",   new Vector3(5.2f, 1.6f, -3.8f));

        Renderer ra = a.GetComponentInChildren<Renderer>();
        Renderer rb = b.GetComponentInChildren<Renderer>();
        int subs = SubMeshCount(ra);

        // 외곽선 굵기: 정점을 "모델 좌표"로 미는 값이라, 고래를 줄인 만큼 키워 줘야 월드에서 약 1.5cm가 됨
        float objScale = Mathf.Max(1e-5f, ra.transform.lossyScale.x);
        float width = 0.015f / objScale;
        m.outline.SetFloat("_OutlineWidth", width);
        m.merged.SetFloat("_OutlineWidth", width);
        EditorUtility.SetDirty(m.outline); EditorUtility.SetDirty(m.merged);

        // (1) 세 재질을 Mesh Renderer에 겹쳐 올림 (Element 0, 1, 2)
        var stacked = new List<Material>();
        for (int i = 0; i < subs - 1; i++) stacked.Add(m.toon);   // 서브메시가 여럿이면 앞쪽은 툰으로 채움
        stacked.Add(m.toon); stacked.Add(m.outline); stacked.Add(m.vcolor);
        ra.sharedMaterials = stacked.ToArray();

        // (2) 합친 셰이더 재질 하나
        var one = new Material[subs];
        for (int i = 0; i < subs; i++) one[i] = m.merged;
        rb.sharedMaterials = one;

        Mesh mesh = MeshOf(ra);
        bool hasColors = mesh != null && mesh.colors32 != null && mesh.colors32.Length > 0;
        return "과제 5: 왼쪽 고래 = 재질 3개 겹침, 오른쪽 고래 = 합친 셰이더 1개\n" +
               "(서브메시 " + subs + "개, 정점 색 " + (hasColors ? "있음" : "없음") + ")";
    }

    static GameObject MakeWhale(Transform parent, GameObject model, string name, Vector3 localPos)
    {
        GameObject w = (GameObject)PrefabUtility.InstantiatePrefab(model);
        w.name = name;
        w.transform.SetParent(parent, false);
        w.transform.localRotation = Quaternion.Euler(-90f, WHALE_YAW, 0f);   // 모델이 꼬리를 위로 세운 채 들어와서 눕힘

        // 가장 긴 변이 1.2m가 되게 크기를 맞춤
        Bounds bb = new Bounds();
        bool first = true;
        foreach (Renderer r in w.GetComponentsInChildren<Renderer>())
        {
            if (first) { bb = r.bounds; first = false; } else bb.Encapsulate(r.bounds);
        }
        float longest = Mathf.Max(bb.size.x, Mathf.Max(bb.size.y, bb.size.z));
        if (longest > 1e-5f) w.transform.localScale *= 1.2f / longest;

        // 크기를 바꾼 뒤 가운데가 localPos에 오게 옮김
        w.transform.localPosition = Vector3.zero;
        first = true;
        foreach (Renderer r in w.GetComponentsInChildren<Renderer>())
        {
            if (first) { bb = r.bounds; first = false; } else bb.Encapsulate(r.bounds);
        }
        Vector3 target = parent.TransformPoint(localPos);
        w.transform.position += target - bb.center;
        return w;
    }

    static Mesh MeshOf(Renderer r)
    {
        if (r is SkinnedMeshRenderer s) return s.sharedMesh;
        MeshFilter f = r.GetComponent<MeshFilter>();
        return f != null ? f.sharedMesh : null;
    }

    static int SubMeshCount(Renderer r)
    {
        Mesh mesh = MeshOf(r);
        return mesh != null ? Mathf.Max(1, mesh.subMeshCount) : 1;
    }

    // =====================================================================
    //  카메라
    // =====================================================================
    static void BuildCameras(Transform root, Scene scene)
    {
        GameObject shot = FindInScene(scene, "ShotCamera");
        Camera src = shot != null ? shot.GetComponent<Camera>() : null;

        // 전체: 교수자 Roof 씬의 ShotCamera 자리 (덩굴, 잎, 캐노피, 고래가 함께 보임)
        MakeCamera(root, "S23_ShotCamera", src, true,
                   new Vector3(-6.1605635f, 6.090968f, -8.438127f),
                   new Quaternion(0.1775649f, 0.31211635f, -0.05955566f, 0.9314007f), 60f);

        // 지붕 아래에서 올려다보기 (캐노피 아랫면 확인용) — 쓸 때 켜기
        Vector3 under = new Vector3(1.2f, 1.6f, -0.6f);
        Quaternion lookUp = Quaternion.LookRotation(new Vector3(0f, 4.7f, 2f) - under, Vector3.up);
        MakeCamera(root, "S23_UnderRoofCamera", src, false, under, lookUp, 75f);

        // 고래 두 마리 가까이 (과제 5 스크린샷용) — 쓸 때 켜기
        Vector3 mid = new Vector3(4.4f, 1.6f, -3.8f);
        Vector3 camPos = mid + new Vector3(-1.0f, 0.5f, -3.0f);
        MakeCamera(root, "S23_ShaderCamera", src, false, camPos,
                   Quaternion.LookRotation(mid - camPos, Vector3.up), 50f);
    }

    static void MakeCamera(Transform root, string name, Camera src, bool active,
                           Vector3 localPos, Quaternion localRot, float fov)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;

        Camera cam = go.AddComponent<Camera>();
        if (src != null)
        {
            cam.clearFlags      = src.clearFlags;
            cam.backgroundColor = src.backgroundColor;
            cam.nearClipPlane   = src.nearClipPlane;
            cam.farClipPlane    = src.farClipPlane;
        }
        cam.fieldOfView = fov;
        cam.depth       = 200f;         // 기존 S16/S17 카메라(100)보다 위
        cam.tag         = "Untagged";

        UniversalAdditionalCameraData data = go.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = true;
        data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

        go.SetActive(active);
    }

    // =====================================================================
    static GameObject FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name) return root;
            Transform t = FindDeep(root.transform, name);
            if (t != null) return t.gameObject;
        }
        return null;
    }

    static Transform FindDeep(Transform parent, string name)
    {
        foreach (Transform c in parent)
        {
            if (c.name == name) return c;
            Transform r = FindDeep(c, name);
            if (r != null) return r;
        }
        return null;
    }
}
#endif
