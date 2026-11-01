#if UNITY_EDITOR
// ---------------------------------------------------------------------------
//  S17 과제 자동 생성 스크립트
//  메뉴: CG > S17 과제 자동 생성
//  - S16_Landscape -> S17/S17_Landscape 로 Save As
//  - Mat_S17_MyTexture (CG/S17_MyTexture + 격자 텍스처) 생성
//  - Quad, Sphere, 우리 메시(다이아몬드)에 그 재질을 입힘
//  - 세 물체가 함께 보이는 구도용 카메라 배치
// ---------------------------------------------------------------------------
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class S17Setup
{
    const string SRC_SCENE = "Assets/S16/S16_Landscape.unity";
    const string DST_SCENE = "Assets/S17/S17_Landscape.unity";
    const string S17_DIR   = "Assets/S17";

    const string SHADER_PATH = "Assets/S17/S17_MyTexture.shader";
    const string TEX_PATH    = "Assets/S17/Checker Texture 512x512 Texel Density Texture 1.png";
    const string MAT_PATH    = "Assets/S17/Mat_S17_MyTexture.mat";

    const string ROOT_NAME = "S17_Showcase";
    const string CAM_NAME  = "S17_ShotCamera";

    const float GAP         = 1.15f;  // 좌우 간격
    const float CAM_MARGIN  = 1.12f;  // 여백 (1.0이면 딱 맞음)
    const float SAFE_ASPECT = 1.33f;  // Game view 가로:세로를 보수적으로 가정

    const string MARKER  = "Assets/S17/.runsetup";
    const string LOGFILE = "Assets/S17/_setup_log.txt";
    static bool silent = false;

    static void Say(string title, string body)
    {
        File.WriteAllText(LOGFILE, title + "\n\n" + body);
        if (silent) Debug.Log("[S17] " + title + "\n" + body);
        else        EditorUtility.DisplayDialog(title, body, "확인");
    }

    [DidReloadScripts]
    static void AutoRun()
    {
        if (!File.Exists(MARKER)) return;
        File.Delete(MARKER);
        EditorApplication.delayCall += () => { silent = true; Run(); silent = false; };
    }

    // =====================================================================
    [MenuItem("CG/S17 과제 자동 생성", false, 11)]
    public static void Run()
    {
        try
        {
            EditorUtility.DisplayProgressBar("S17", "씬 준비 (Save As)...", 0.05f);
            Scene scene = PrepareScene();

            EditorUtility.DisplayProgressBar("S17", "셰이더와 텍스처 확인...", 0.2f);
            Shader sh = AssetDatabase.LoadAssetAtPath<Shader>(SHADER_PATH);
            Texture tex = AssetDatabase.LoadAssetAtPath<Texture>(TEX_PATH);
            if (sh == null || tex == null)
            {
                EditorUtility.ClearProgressBar();
                Say("S17 오류",
                    "셰이더 또는 격자 텍스처를 찾지 못했습니다.\n" + SHADER_PATH + "\n" + TEX_PATH);
                return;
            }

            EditorUtility.DisplayProgressBar("S17", "재질 생성...", 0.35f);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MAT_PATH);
            if (mat == null)
            {
                mat = new Material(sh);
                AssetDatabase.CreateAsset(mat, MAT_PATH);
            }
            else mat.shader = sh;
            mat.SetTexture("_BaseMap", tex);
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayProgressBar("S17", "장면에 배치...", 0.6f);
            BuildShowcase(scene, mat);

            EditorUtility.DisplayProgressBar("S17", "씬 저장...", 0.85f);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, DST_SCENE);

            EditorUtility.ClearProgressBar();
            Say("S17 완료",
                "S17 과제 자동 생성이 끝났습니다.\n\n" +
                "씬: " + DST_SCENE + "\n\n" +
                "Game view에 왼쪽부터 Quad / Sphere / 우리 메시(다이아몬드)가\n" +
                "모두 D3 칸 색으로 보입니다.\n" +
                "(S16_Showcase는 이 씬에서 꺼 두었습니다.)");
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogException(e);
            Say("S17 오류", e.ToString());
        }
    }

    // =====================================================================
    static Scene PrepareScene()
    {
        EnsureFolder(S17_DIR);
        EnsureFolder(S17_DIR + "/Editor");
        Scene scene = EditorSceneManager.OpenScene(SRC_SCENE, OpenSceneMode.Single);
        EditorSceneManager.SaveScene(scene, DST_SCENE);
        return EditorSceneManager.GetActiveScene();
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
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
    static void BuildShowcase(Scene scene, Material mat)
    {
        GameObject old = FindInScene(scene, ROOT_NAME);
        if (old != null) Object.DestroyImmediate(old);

        // S16 진열대는 이 씬에서 꺼 둔다 (S16 씬은 그대로 남는다)
        GameObject s16 = FindInScene(scene, "S16_Showcase");
        if (s16 != null) s16.SetActive(false);

        GameObject firepit = FindInScene(scene, "Firepit");
        GameObject shot    = FindInScene(scene, "ShotCamera");

        Vector3 anchor = firepit != null ? firepit.transform.position : new Vector3(6f, 2.6f, 2f);

        Vector3 fwd = Vector3.forward;
        if (shot != null)
        {
            Vector3 d = shot.transform.position - anchor;
            d.y = 0f;
            if (d.sqrMagnitude > 0.01f) fwd = d.normalized;
        }
        Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

        Vector3 center = anchor + fwd * 1.3f;
        center.y = anchor.y - 0.25f;

        GameObject root = new GameObject(ROOT_NAME);
        root.transform.position = center;
        SceneManager.MoveGameObjectToScene(root, scene);

        // Quad: Unity의 Quad는 법선이 -Z라, -fwd를 보게 돌려야 카메라 쪽을 향한다.
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Quad_S17";
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        quad.transform.SetParent(root.transform, true);
        quad.transform.position   = center + right * (-GAP);
        quad.transform.rotation   = Quaternion.LookRotation(-fwd, Vector3.up);
        quad.transform.localScale = Vector3.one * 1.0f;
        quad.GetComponent<MeshRenderer>().sharedMaterial = mat;

        // Sphere: Unity가 정해 둔 uv 배열이 들어 있다.
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Sphere_S17";
        Object.DestroyImmediate(sphere.GetComponent<Collider>());
        sphere.transform.SetParent(root.transform, true);
        sphere.transform.position   = center;
        sphere.transform.localScale = Vector3.one * 0.8f;
        sphere.GetComponent<MeshRenderer>().sharedMaterial = mat;

        // 우리 메시: uv 배열을 넣지 않아 모든 정점의 UV가 (0, 0)이다.
        GameObject dia = new GameObject("Mesh_Diamond_S17");
        dia.transform.SetParent(root.transform, true);
        dia.transform.localScale = Vector3.one * 0.5f;
        dia.transform.position   = center + right * GAP;
        dia.transform.rotation   = Quaternion.Euler(0f, 30f, 0f);
        dia.AddComponent<MeshFilter>();
        MeshRenderer dmr = dia.AddComponent<MeshRenderer>();
        dia.AddComponent<S04_CustomDiamondMesh>();   // OnEnable에서 메시를 만든다
        dmr.sharedMaterial = mat;

        // 이 메시는 0~1 좌표계라 피벗이 모서리에 있다.
        // 메시의 실제 가운데가 제자리에 오도록 옮긴다.
        Vector3 wantCenter = center + right * GAP;
        dia.transform.position += wantCenter - dmr.bounds.center;

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
        cam.depth       = 110f;
        cam.tag         = "Untagged";

        UniversalAdditionalCameraData data = camGo.GetComponent<UniversalAdditionalCameraData>();
        if (data == null) data = camGo.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = true;
        data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

        // 세 물체를 모두 담는 상자를 구해 거리를 정한다.
        Renderer[] rends = root.GetComponentsInChildren<Renderer>();
        Bounds bounds = new Bounds(center, Vector3.one * 0.5f);
        for (int i = 0; i < rends.Length; i++)
        {
            if (i == 0) bounds = rends[i].bounds;
            else        bounds.Encapsulate(rends[i].bounds);
        }

        Vector3 e  = bounds.extents;
        Vector3 aR = new Vector3(Mathf.Abs(right.x), Mathf.Abs(right.y), Mathf.Abs(right.z));
        Vector3 aF = new Vector3(Mathf.Abs(fwd.x),   Mathf.Abs(fwd.y),   Mathf.Abs(fwd.z));
        float needH  = Vector3.Dot(e, aR);
        float needV  = Mathf.Abs(e.y);
        float depthE = Vector3.Dot(e, aF);

        float halfFov = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float t       = Mathf.Tan(halfFov);
        float dist = Mathf.Max(needV / t, needH / (SAFE_ASPECT * t)) * CAM_MARGIN + depthE;

        Vector3 target = bounds.center;

        // 카메라가 정자 밖으로 물러나면 격자벽이 끼어든다.
        // ShotCamera보다 조금 뒤까지만 허용하고, 모자라면 화각을 넓힌다.
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
    }
}
#endif
