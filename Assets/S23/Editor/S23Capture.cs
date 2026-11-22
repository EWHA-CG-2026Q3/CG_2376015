#if UNITY_EDITOR
// ---------------------------------------------------------------------------
//  S23 과제 스크린샷 저장
//  메뉴: CG > S23 스크린샷 저장
//  카메라마다 1920x1080 PNG로 렌더링해 프로젝트 폴더의 S23_Screenshots/ 에 저장한다.
//  (Assets 밖이라 Unity가 텍스처로 임포트하지 않음. 씬은 바꾸지 않음)
// ---------------------------------------------------------------------------
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class S23Capture
{
    const string SCENE   = "Assets/S23/S23_Landscape.unity";
    const string OUT_DIR = "S23_Screenshots";
    const string MARKER  = "Assets/S23/.runcapture";
    const int W = 1920, H = 1080;

    [DidReloadScripts]
    static void AutoRun()
    {
        if (!File.Exists(MARKER)) return;
        File.Delete(MARKER);
        EditorApplication.delayCall += () => Run();
    }

    [MenuItem("CG/S23 스크린샷 저장", false, 2)]
    public static void Run()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != SCENE) scene = EditorSceneManager.OpenScene(SCENE, OpenSceneMode.Single);

        GameObject root = GameObject.Find("S23_Assignment");
        if (root == null) { Debug.LogError("[S23] S23_Assignment를 찾지 못했습니다."); return; }
        Transform r = root.transform;
        Directory.CreateDirectory(OUT_DIR);

        // bilinear patch 메시는 Update에서 계산되므로, 스크립트를 다시 불러온 직후에는 비어 있을 수 있다.
        // 찍기 전에 모든 패치의 BuildMesh()를 한 번씩 직접 불러 메시를 채운다.
        var build = typeof(S23_BilinearPatch_Finish).GetMethod("BuildMesh",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        foreach (var bp in root.GetComponentsInChildren<S23_BilinearPatch_Finish>(true))
            if (bp.isActiveAndEnabled) build.Invoke(bp, null);

        string log = "";
        log += Shot(r, "S23_ShotCamera",      "01_전체_덩굴_잎_캐노피_고래.png");
        // 캐노피 아래: 파빌리온 앞쪽 밖에서, 앞으로 높이 솟은 캐노피 모서리의 아랫면을 올려다봄
        log += TempShot(r, "03_캐노피_아래에서.png",
                        new Vector3(0.8f, 1.7f, -6.2f), new Vector3(0f, 7.2f, -1.2f), 65f);
        log += Shot(r, "S23_ShaderCamera",    "04_셰이더_비교_고래.png");

        // 덩굴·잎 가까이 (임시 카메라, 저장 후 지움)
        log += TempShot(r, "02_덩굴_나뭇잎.png",
                        new Vector3(-3.6f, 3.3f, -2.2f), new Vector3(-2.7f, 2.3f, 1.9f), 48f);
        // 캐노피를 위에서 (임시 카메라)
        log += TempShot(r, "05_캐노피_위에서.png",
                        new Vector3(-5.5f, 10.5f, -6.5f), new Vector3(0f, 4.2f, 2f), 50f);

        File.WriteAllText(Path.Combine(OUT_DIR, "_capture_log.txt"), log);
        Debug.Log("[S23] 스크린샷 저장 완료\n" + log);
    }

    static string Shot(Transform root, string camName, string file)
    {
        Transform t = root.Find(camName);
        if (t == null) return "없음: " + camName + "\n";
        bool wasActive = t.gameObject.activeSelf;
        t.gameObject.SetActive(true);
        string res = Render(t.GetComponent<Camera>(), file);
        t.gameObject.SetActive(wasActive);
        return res;
    }

    static string TempShot(Transform root, string file, Vector3 localPos, Vector3 localTarget, float fov)
    {
        GameObject go = new GameObject("S23_TempCaptureCamera");
        go.hideFlags = HideFlags.DontSave;
        Camera src = root.Find("S23_ShotCamera") != null ? root.Find("S23_ShotCamera").GetComponent<Camera>() : null;
        Camera cam = go.AddComponent<Camera>();
        if (src != null) { cam.clearFlags = src.clearFlags; cam.backgroundColor = src.backgroundColor; }
        cam.fieldOfView = fov;
        var data = go.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = true;
        data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        go.transform.position = root.TransformPoint(localPos);
        go.transform.rotation = Quaternion.LookRotation(root.TransformPoint(localTarget) - go.transform.position, Vector3.up);
        string res = Render(cam, file);
        Object.DestroyImmediate(go);
        return res;
    }

    static string Render(Camera cam, string file)
    {
        if (cam == null) return "카메라 없음: " + file + "\n";
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
        return "저장: " + path + "\n";
    }
}
#endif
