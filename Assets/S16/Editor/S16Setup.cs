#if UNITY_EDITOR
// ---------------------------------------------------------------------------
//  S16 과제 자동 생성 스크립트
//  메뉴: CG > S16 과제 자동 생성
//  - S15_Landscape -> S16/S16_Landscape 로 Save As
//  - 실습 1: 최소한의 셰이더(S16_MyShader) + Mat_S16_MyShader (빨간 원판)
//  - 실습 2: 숯 셰이더(S16_MyCoal) + Mat_S16_MyCoal (목표 숯과 같은 색)
//  - 과제 3: 우리 코드로 만든 메시 2개에 MyCoal 복제본을 입히고 다른 색으로
//  - 과제 4: 목표 숯 + 우리 셰이더의 공 + 우리 메시가 함께 보이는 카메라 구도
// ---------------------------------------------------------------------------
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class S16Setup
{
    const string SRC_SCENE = "Assets/S15/S15_Landscape.unity";
    const string DST_SCENE = "Assets/S16/S16_Landscape.unity";
    const string S16_DIR   = "Assets/S16";

    const string SH_MYSHADER = "Assets/S16/S16_MyShader.shader";
    const string SH_MYCOAL   = "Assets/S16/S16_MyCoal.shader";
    const string MAT_COAL    = "Assets/S14/Mat_S14_Coal.mat";   // 목표 숯 (S13/S14 숯 재질)

    const string M_MYSHADER = "Assets/S16/Mat_S16_MyShader.mat";
    const string M_MYCOAL   = "Assets/S16/Mat_S16_MyCoal.mat";
    const string M_CYAN     = "Assets/S16/Mat_S16_MyCoal_Cyan.mat";
    const string M_MAGENTA  = "Assets/S16/Mat_S16_MyCoal_Magenta.mat";

    const string ROOT_NAME  = "S16_Showcase";
    const string CAM_NAME   = "S16_ShotCamera";

    // 진열대에서 물체 사이 간격과 카메라 거리
    const float GAP         = 1.0f;   // 좌우 간격
    const float TIER        = 0.45f;  // 윗줄(공) / 아랫줄(메시) 높이차
    const float CAM_MARGIN  = 1.12f;  // 여백 (1.0이면 딱 맞음)
    const float SAFE_ASPECT = 1.33f;  // Game view 가로:세로를 보수적으로 가정

    const string MARKER  = "Assets/S16/.runsetup";
    const string LOGFILE = "Assets/S16/_setup_log.txt";
    static bool silent = false;

    static void Say(string title, string body)
    {
        File.WriteAllText(LOGFILE, title + "\n\n" + body);
        if (silent) Debug.Log("[S16] " + title + "\n" + body);
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
    [MenuItem("CG/S16 과제 자동 생성", false, 1)]
    public static void Run()
    {
        try
        {
            EditorUtility.DisplayProgressBar("S16", "씬 준비 (Save As)...", 0.05f);
            Scene scene = PrepareScene();

            EditorUtility.DisplayProgressBar("S16", "셰이더 확인...", 0.2f);
            Shader shMy   = AssetDatabase.LoadAssetAtPath<Shader>(SH_MYSHADER);
            Shader shCoal = AssetDatabase.LoadAssetAtPath<Shader>(SH_MYCOAL);
            if (shMy == null || shCoal == null)
            {
                EditorUtility.ClearProgressBar();
                Say("S16 오류", "셰이더를 찾지 못했습니다.\n" + SH_MYSHADER + "\n" + SH_MYCOAL);
                return;
            }

            EditorUtility.DisplayProgressBar("S16", "재질 생성...", 0.35f);

            // 목표 숯(Mat_S14_Coal)의 Emission 색을 그대로 가져온다.
            Color coalEmission = new Color(23.968628f, 14.682353f, 0.3764706f, 1f);
            Material coalSrc = AssetDatabase.LoadAssetAtPath<Material>(MAT_COAL);
            if (coalSrc != null && coalSrc.HasProperty("_EmissionColor"))
                coalEmission = coalSrc.GetColor("_EmissionColor");

            // 실습 1 재질: 셰이더에 칸이 없으므로 값 없이 그대로
            Material matMyShader = MakeMaterial(M_MYSHADER, shMy);

            // 실습 2 재질: 목표 숯과 같은 색 (Base 검정 + 숯의 Emission)
            Material matMyCoal = MakeMaterial(M_MYCOAL, shCoal);
            matMyCoal.SetColor("_BaseColor", new Color(0f, 0f, 0f, 1f));
            matMyCoal.SetColor("_EmissionColor", coalEmission);

            // 과제 3 재질: MyCoal의 복제본, 숯과 다른 색
            Material matCyan = MakeMaterial(M_CYAN, shCoal);
            matCyan.SetColor("_BaseColor", new Color(0f, 0f, 0.20f, 1f));
            matCyan.SetColor("_EmissionColor", new Color(0.20f, 1.90f, 2.60f, 1f));

            Material matMagenta = MakeMaterial(M_MAGENTA, shCoal);
            matMagenta.SetColor("_BaseColor", new Color(0.16f, 0f, 0.12f, 1f));
            matMagenta.SetColor("_EmissionColor", new Color(2.60f, 0.22f, 1.70f, 1f));

            foreach (Material m in new[] { matMyShader, matMyCoal, matCyan, matMagenta })
                EditorUtility.SetDirty(m);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayProgressBar("S16", "장면에 배치...", 0.6f);
            BuildShowcase(scene, coalSrc, matMyShader, matMyCoal, matCyan, matMagenta);

            EditorUtility.DisplayProgressBar("S16", "씬 저장...", 0.8f);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, DST_SCENE);

            EditorUtility.ClearProgressBar();
            Say("S16 완료",
                "S16 과제 자동 생성이 끝났습니다.\n\n" +
                "씬: " + DST_SCENE + "\n\n" +
                "Game view를 보면 왼쪽부터\n" +
                "목표 숯 / 실습1 빨간 공 / 실습2 우리 숯 공 /\n" +
                "우리 메시(다이아몬드, 사각뿔) 순서로 보입니다.");
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogException(e);
            Say("S16 오류", e.ToString());
        }
    }

    [MenuItem("CG/S16 조명 베이크 (Generate Lighting)", false, 2)]
    public static void Bake()
    {
        if (!EditorSceneManager.GetActiveScene().path.EndsWith("S16_Landscape.unity"))
            EditorSceneManager.OpenScene(DST_SCENE, OpenSceneMode.Single);
        UnityEditor.Lightmapping.Bake();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("S16", "Generate Lighting을 끝내고 씬을 저장했습니다.", "확인");
    }

    // =====================================================================
    static Scene PrepareScene()
    {
        EnsureFolder(S16_DIR);
        EnsureFolder(S16_DIR + "/Editor");

        Scene scene = EditorSceneManager.OpenScene(SRC_SCENE, OpenSceneMode.Single);
        EditorSceneManager.SaveScene(scene, DST_SCENE);
        return EditorSceneManager.GetActiveScene();
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf   = Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    static Material MakeMaterial(string path, Shader shader)
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        else
        {
            m.shader = shader;
        }
        return m;
    }

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

    // =====================================================================
    static Camera BuildShowcase(Scene scene, Material coalMat,
                                Material matMyShader, Material matMyCoal,
                                Material matCyan, Material matMagenta)
    {
        // 이미 만들어 둔 것이 있으면 지우고 새로 만든다 (여러 번 실행해도 안전)
        GameObject old = FindInScene(scene, ROOT_NAME);
        if (old != null) Object.DestroyImmediate(old);

        GameObject firepit = FindInScene(scene, "Firepit");
        GameObject shot    = FindInScene(scene, "ShotCamera");

        Vector3 anchor = firepit != null ? firepit.transform.position : new Vector3(6f, 2.6f, 2f);

        // 화로에서 기존 구도 카메라 쪽(= 트인 방향)으로 나오는 방향
        Vector3 fwd = Vector3.forward;
        if (shot != null)
        {
            Vector3 d = shot.transform.position - anchor;
            d.y = 0f;
            if (d.sqrMagnitude > 0.01f) fwd = d.normalized;
        }
        Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

        // 실습 1대로 화로 옆(정자 안)에 둔다. 화로보다 조금 트인 쪽으로 당겨
        // 카메라가 정자 안에 머물 수 있게 한다.
        Vector3 center = anchor + fwd * 1.3f;
        center.y = anchor.y - 0.25f;

        GameObject root = new GameObject(ROOT_NAME);
        root.transform.position = center;
        SceneManager.MoveGameObjectToScene(root, scene);

        // --- 공 3개 -------------------------------------------------------
        // 실습 1: 화로 옆에 Sphere 두 개 (왼쪽 = 목표 숯, 오른쪽 = 우리 셰이더)
        // 실습 2: 우리 셰이더 공을 MyCoal 재질로 키운 것까지 같이 보이게 3개 배치
        MakeSphere(root.transform, "Sphere_TargetCoal",
                   center + right * (-GAP) + Vector3.up * TIER, 0.55f,
                   coalMat != null ? coalMat : matMyCoal);
        MakeSphere(root.transform, "Sphere_S16_MyShader",
                   center + Vector3.up * TIER, 0.55f, matMyShader);
        MakeSphere(root.transform, "Sphere_S16_MyCoal",
                   center + right * GAP + Vector3.up * TIER, 0.55f, matMyCoal);

        // --- 우리 코드로 만든 메시 2개 ------------------------------------
        // 두 메시 모두 0~1 좌표계라서 가운데가 (0.5, *, 0.5) 이다.
        // 가운데가 자리에 오도록 피벗을 옮겨 준다.
        MakeCodeMesh<S04_CustomDiamondMesh>(root.transform, "Mesh_Diamond_S16",
            center + right * (-0.5f * GAP) + Vector3.up * -TIER, 0.45f, matCyan, 0f);
        MakeCodeMesh<PyramidMesh>(root.transform, "Mesh_Pyramid_S16",
            center + right * (0.5f * GAP) + Vector3.up * -TIER, 0.70f, matMagenta, -0.5f);

        // --- 구도용 카메라 -------------------------------------------------
        GameObject camGo = FindInScene(scene, CAM_NAME);
        if (camGo == null)
        {
            camGo = new GameObject(CAM_NAME);
            SceneManager.MoveGameObjectToScene(camGo, scene);
        }
        camGo.transform.SetParent(root.transform, true);

        Camera cam = camGo.GetComponent<Camera>();
        if (cam == null) cam = camGo.AddComponent<Camera>();

        Camera src = shot != null ? shot.GetComponent<Camera>() : null;
        if (src != null)
        {
            cam.clearFlags      = src.clearFlags;
            cam.backgroundColor = src.backgroundColor;
            cam.nearClipPlane   = src.nearClipPlane;
            cam.farClipPlane    = src.farClipPlane;
        }
        cam.fieldOfView = 50f;
        cam.depth       = 100f;          // Game view가 이 카메라를 쓰도록
        cam.tag         = "Untagged";

        // 후처리(Bloom)를 켜 둬야 Emission이 번진다.
        UniversalAdditionalCameraData data = camGo.GetComponent<UniversalAdditionalCameraData>();
        if (data == null) data = camGo.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = true;
        data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

        // 진열한 것들을 모두 담는 상자를 구해, 전부 화면에 들어오는 거리를 계산한다.
        Renderer[] rends = root.GetComponentsInChildren<Renderer>();
        Bounds bounds = new Bounds(center, Vector3.one * 0.5f);
        for (int i = 0; i < rends.Length; i++)
        {
            if (i == 0) bounds = rends[i].bounds;
            else        bounds.Encapsulate(rends[i].bounds);
        }

        // 가로/세로로 필요한 만큼만 물러난다. (구 반지름을 쓰면 깊이까지 더해져 너무 멀어짐)
        Vector3 e = bounds.extents;
        Vector3 aR = new Vector3(Mathf.Abs(right.x), Mathf.Abs(right.y), Mathf.Abs(right.z));
        Vector3 aF = new Vector3(Mathf.Abs(fwd.x),   Mathf.Abs(fwd.y),   Mathf.Abs(fwd.z));
        float needH  = Vector3.Dot(e, aR);
        float needV  = Mathf.Abs(e.y);
        float depthE = Vector3.Dot(e, aF);

        float halfFov = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float t       = Mathf.Tan(halfFov);
        // Game view가 4:3으로 좁아도 들어가도록 보수적으로 잡는다.
        float dist = Mathf.Max(needV / t, needH / (SAFE_ASPECT * t)) * CAM_MARGIN + depthE;

        Vector3 target = bounds.center;

        // 카메라가 정자 밖으로 물러나면 격자벽이 화면에 끼어든다.
        // S14 구도용 카메라(ShotCamera)보다 조금 뒤까지만 허용하고,
        // 그래도 다 안 들어오면 더 물러나는 대신 화각(FOV)을 넓힌다.
        if (shot != null)
        {
            float maxDist = Vector3.Dot(shot.transform.position + fwd * 0.8f - target, fwd);
            if (dist > maxDist)
            {
                dist = Mathf.Max(maxDist, 1.5f);
                float fovV = 2f * Mathf.Atan(needV / dist) * Mathf.Rad2Deg;
                float fovH = 2f * Mathf.Atan(needH / (SAFE_ASPECT * dist)) * Mathf.Rad2Deg;
                cam.fieldOfView = Mathf.Clamp(Mathf.Max(fovV, fovH) * CAM_MARGIN, 40f, 90f);
            }
        }

        camGo.transform.position = target + fwd * dist + Vector3.up * 0.25f;
        camGo.transform.LookAt(target);

        return cam;
    }

    static void MakeSphere(Transform parent, string name, Vector3 pos, float scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, true);
        go.transform.position   = pos;
        go.transform.localScale = Vector3.one * scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    static void MakeCodeMesh<T>(Transform parent, string name, Vector3 pos,
                                float scale, Material mat, float yOffset)
        where T : MonoBehaviour
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, true);
        go.transform.localScale = Vector3.one * scale;
        // 0~1 좌표계 메시의 가운데를 pos에 맞춘다.
        go.transform.position = pos + new Vector3(-0.5f * scale, yOffset * scale, -0.5f * scale);
        go.transform.rotation = Quaternion.Euler(0f, 30f, 0f);

        go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        go.AddComponent<T>();                       // OnEnable에서 메시를 만든다
        mr.sharedMaterial = mat;                    // 메시 생성 뒤 우리 재질로 덮어쓴다
    }

}
#endif
