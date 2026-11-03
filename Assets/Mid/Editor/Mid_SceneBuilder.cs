#if UNITY_EDITOR
// Mid_SceneBuilder.cs
// 중간 프로젝트 장면 자동 생성 (에디터 전용)
// 메뉴: CG > Mid 장면 만들기 (비 오는 날)
//
//   - Assets/Mid/Mid_RainyDay.unity 장면을 새로 만듦
//   - 재질 Mat_Mid_*.mat 을 Assets/Mid 에 만듦 (이미 있으면 그대로 씀 → 색을 바꿔 두었다면 유지됨)
//   - 배경(잔디, 흙길, 층층이 언덕, 둥근 나무, 연잎 연못, 수국 덤불)을 만듦
//   - 개구리(+ 손 + 연잎 우산), 토끼, 비, 연출 오브젝트를 만들고 서로 연결함
//   - 비 오는 날의 부드러운 파스텔 분위기로 카메라, 조명, 안개를 설정함
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Mid_SceneBuilder
{
    const string MID_DIR = "Assets/Mid";
    const string SCENE_PATH = "Assets/Mid/Mid_RainyDay.unity";

    // 비 오는 날의 흐린 파스텔 하늘 색
    static readonly Color SkyColor = new Color(0.66f, 0.73f, 0.84f);

    [MenuItem("CG/Mid 장면 만들기 (비 오는 날)", false, 50)]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (File.Exists(SCENE_PATH) &&
            !EditorUtility.DisplayDialog("Mid 장면 만들기",
                SCENE_PATH + " 가 이미 있습니다.\n새로 만들어 덮어쓸까요?\n(재질 파일은 지우지 않고 그대로 씁니다)",
                "덮어쓰기", "취소"))
            return;

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // ---------- 재질 ----------
        Material matLotus    = LitMaterial("Lotus",       new Color(0.26f, 0.55f, 0.19f), 0.60f);
        Material matFrog     = LitMaterial("Frog",        new Color(0.56f, 0.84f, 0.47f), 0.55f);
        Material matFrogDark = LitMaterial("FrogFeet",    new Color(0.44f, 0.71f, 0.38f), 0.50f);
        Material matBelly    = LitMaterial("FrogBelly",   new Color(0.93f, 0.95f, 0.72f), 0.40f);
        Material matRabbit   = LitMaterial("Rabbit",      new Color(0.97f, 0.95f, 0.92f), 0.35f);
        Material matEarPink  = LitMaterial("EarInner",    new Color(1.00f, 0.72f, 0.76f), 0.30f);
        Material matEyeWhite = LitMaterial("EyeWhite",    new Color(1.00f, 1.00f, 1.00f), 0.70f);
        Material matEyeBlack = LitMaterial("EyeBlack",    new Color(0.06f, 0.05f, 0.07f), 0.95f);
        Material matMouth    = LitMaterial("Mouth",       new Color(0.30f, 0.14f, 0.12f), 0.30f);
        Material matBlush    = LitMaterial("Blush",       new Color(1.00f, 0.55f, 0.62f), 0.00f);
        Material matNose     = LitMaterial("Nose",        new Color(1.00f, 0.60f, 0.68f), 0.50f);
        Material matRain     = GLMaterial("Rain");

        // ---------- 카메라, 조명, 분위기 ----------
        SetupCameraAndLight();

        // ---------- 배경 ----------
        BuildBackground(matLotus);

        // ---------- 개구리 + 연잎 우산 ----------
        Transform frogHand;
        Transform frogBody;
        Transform leafCenter;
        Mid_LotusUmbrella umbrella;
        Transform frog = BuildFrog(matFrog, matFrogDark, matBelly, matEyeWhite, matEyeBlack, matMouth, matBlush,
                                   matLotus, out frogBody, out frogHand, out umbrella, out leafCenter);

        // ---------- 토끼 ----------
        RabbitParts rabbitParts = BuildRabbit(matRabbit, matEarPink, matEyeBlack, matEyeWhite, matMouth, matBlush, matNose);
        Transform rabbit = rabbitParts.root;

        // ---------- 비 ----------
        GameObject rainGO = new GameObject("Rain (GL)");
        rainGO.transform.position = new Vector3(0f, 0f, 1.5f);
        Mid_RainGL rain = rainGO.AddComponent<Mid_RainGL>();
        rain.dropCount = 900;
        rain.areaSize = new Vector3(14f, 6f, 8f);
        rain.rainColor = new Color(1f, 1f, 1f, 0.55f);
        rain.glMaterial = matRain;
        rain.umbrellaLeafCenter = leafCenter;
        rain.shelterRadius = umbrella.leafRadius * 0.9f;

        // ---------- 연출 ----------
        GameObject directorGO = new GameObject("Mid_Director");
        Mid_RainyDayDirector director = directorGO.AddComponent<Mid_RainyDayDirector>();
        director.frog = frog;
        director.frogBody = frogBody;
        director.frogHand = frogHand;
        director.rabbit = rabbit;
        director.rabbitBody = rabbitParts.body;
        director.earLeft = rabbitParts.earL;
        director.earRight = rabbitParts.earR;
        director.rabbitMouth = rabbitParts.mouth;
        director.blushLeft = rabbitParts.blushL;
        director.blushRight = rabbitParts.blushR;
        director.eyeLeft = rabbitParts.eyeL;
        director.eyeRight = rabbitParts.eyeR;
        director.happyEyeLeft = rabbitParts.happyEyeL;
        director.happyEyeRight = rabbitParts.happyEyeR;

        // 편집 화면에서는 "시작 장면"(귀 처짐, 시무룩, 눈 뜸) 상태로 보이게 맞춰 둠
        rabbitParts.earL.localRotation = Quaternion.Euler(0f, 0f, director.earDroopAngle);
        rabbitParts.earR.localRotation = Quaternion.Euler(0f, 0f, -director.earDroopAngle);
        rabbitParts.mouth.localScale = new Vector3(1f, -1f, 1f);
        rabbitParts.blushL.localScale = Vector3.zero;
        rabbitParts.blushR.localScale = Vector3.zero;
        rabbitParts.happyEyeL.localScale = Vector3.zero;
        rabbitParts.happyEyeR.localScale = Vector3.zero;

        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = directorGO;

        EditorUtility.DisplayDialog("Mid 장면 만들기",
            "완료!\n\n" + SCENE_PATH + "\n\nPlay를 누르면 장면이 재생됩니다.\n" +
            (matRain == null ? "\n※ Mid_GLColor 셰이더를 찾지 못해 비 재질이 비어 있습니다." : ""),
            "확인");
    }

    // =====================================================================
    // 개구리
    //   Frog (이동)
    //    └ FrogBody (몸, 폴짝 뛸 때 크기 변화)
    //        ├ 눈, 입, 볼, 배, 발
    //        └ Hand (빈 오브젝트 = 회전축)
    //            ├ HandBlob
    //            └ LotusUmbrella
    //                └ LeafCenter (비 가림 기준점)
    // =====================================================================
    static Transform BuildFrog(Material body, Material feet, Material belly, Material eyeWhite, Material eyeBlack,
                               Material mouthMat, Material blushMat, Material lotusMat,
                               out Transform frogBody, out Transform hand,
                               out Mid_LotusUmbrella umbrella, out Transform leafCenter)
    {
        Transform frog = new GameObject("Frog").transform;
        frog.position = new Vector3(-0.55f, 0f, 0f);

        Mid_BlobMesh b = Blob("FrogBody", frog, Vector3.zero, body, 0.50f, 0.42f, 0.42f, 0.55f, true);
        frogBody = b.transform;

        // 발 (몸 아래 앞쪽, 납작)
        Blob("Foot_L", frogBody, new Vector3(-0.24f, 0f, -0.18f), feet, 0.15f, 0.19f, 0.05f, 0.5f, true);
        Blob("Foot_R", frogBody, new Vector3( 0.24f, 0f, -0.18f), feet, 0.15f, 0.19f, 0.05f, 0.5f, true);

        // 배 (앞쪽 아래, 연한 색 원판)
        Mid_BlobMesh bellyBlob = Blob("Belly", frogBody, Vector3.zero, belly, 0.28f, 0.04f, 0.15f, 1f, false);
        OnSurface(bellyBlob.transform, b, -28f, -90f, 0.0f);

        // 툭 튀어나온 눈 두 개 (머리 위)
        for (int side = -1; side <= 1; side += 2)
        {
            string s = side < 0 ? "_L" : "_R";
            Mid_BlobMesh bump = Blob("EyeBump" + s, frogBody, b.SurfacePoint(58f, -90f + side * 38f), body,
                                     0.16f, 0.14f, 0.15f, 1f, false);
            Mid_BlobMesh white = Blob("EyeWhite" + s, bump.transform, new Vector3(0f, 0.02f, -0.10f), eyeWhite,
                                      0.11f, 0.06f, 0.11f, 1f, false);
            Mid_BlobMesh pupil = Blob("Pupil" + s, white.transform, new Vector3(0f, 0.005f, -0.05f), eyeBlack,
                                      0.06f, 0.03f, 0.07f, 1f, false);
            Blob("Shine" + s, pupil.transform, new Vector3(0.022f, 0.03f, -0.025f), eyeWhite,
                 0.018f, 0.01f, 0.018f, 1f, false);
        }

        // 웃는 입
        Mid_ArcMesh mouth = Arc("Mouth", frogBody, mouthMat, 0.15f, 0.022f, 150f);
        OnSurface(mouth.transform, b, 22f, -90f, 0.004f);

        // 볼터치
        for (int side = -1; side <= 1; side += 2)
        {
            Mid_BlobMesh blush = Blob(side < 0 ? "Blush_L" : "Blush_R", frogBody, Vector3.zero, blushMat,
                                      0.07f, 0.015f, 0.035f, 1f, false);
            OnSurface(blush.transform, b, 18f, -90f + side * 50f, 0.0f);
        }

        // 손 = 회전축. 몸 오른쪽 앞
        hand = new GameObject("Hand (회전축)").transform;
        hand.SetParent(frogBody, false);
        hand.localPosition = b.SurfacePoint(5f, -40f);   // 몸의 오른쪽 앞
        Blob("HandBlob", hand, new Vector3(0.02f, 0f, 0f), body, 0.10f, 0.09f, 0.09f, 1f, false);

        // 연잎 우산: 손의 자식, 줄기 맨 아래가 손 위치
        GameObject umbrellaGO = new GameObject("LotusUmbrella");
        umbrellaGO.transform.SetParent(hand, false);
        umbrellaGO.transform.localPosition = new Vector3(0f, -0.04f, 0f);
        umbrella = umbrellaGO.AddComponent<Mid_LotusUmbrella>();
        umbrella.stemHeight = 1.85f;   // 토끼 귀가 올라와도 잎에 닿지 않는 높이
        umbrella.material = lotusMat;
        umbrella.Rebuild();

        leafCenter = new GameObject("LeafCenter").transform;
        leafCenter.SetParent(umbrellaGO.transform, false);
        leafCenter.localPosition = new Vector3(0f, umbrella.stemHeight, 0f);

        return frog;
    }

    // =====================================================================
    // 토끼
    //   Rabbit (몸 돌리기, 개구리 쪽으로 다가가기)
    //    └ RabbitBody (오들오들, 기대기, 뭉클)
    //        ├ EarPivot_L / EarPivot_R (귀 뿌리 = 회전축)
    //        │    └ Ear └ EarInner
    //        ├ Eye (동그란 눈) / HappyEye (^^ 감은 눈)
    //        ├ 코, 입, 볼, 발
    // =====================================================================
    class RabbitParts
    {
        public Transform root, body, earL, earR, mouth, blushL, blushR, eyeL, eyeR, happyEyeL, happyEyeR;
        public Renderer[] skin;   // 붉어질 피부 (몸, 귀, 발)
    }

    static RabbitParts BuildRabbit(Material body, Material earPink, Material eyeBlack, Material eyeWhite,
                                   Material mouthMat, Material blushMat, Material noseMat)
    {
        RabbitParts p = new RabbitParts();
        p.root = new GameObject("Rabbit").transform;
        p.root.position = new Vector3(0.75f, 0f, 0.1f);

        Mid_BlobMesh b = Blob("RabbitBody", p.root, Vector3.zero, body, 0.42f, 0.38f, 0.50f, 0.6f, true);
        p.body = b.transform;

        // 발
        Mid_BlobMesh footL = Blob("Foot_L", p.body, new Vector3(-0.18f, 0f, -0.2f), body, 0.11f, 0.15f, 0.05f, 0.5f, true);
        Mid_BlobMesh footR = Blob("Foot_R", p.body, new Vector3( 0.18f, 0f, -0.2f), body, 0.11f, 0.15f, 0.05f, 0.5f, true);

        // 귀: 뿌리에 빈 오브젝트(회전축)를 두고, 귀는 그 자식으로 위로 뻗게
        Renderer earSkinL, earSkinR;
        p.earL = BuildEar("EarPivot_L", p.body, b.SurfacePoint(70f, 200f), body, earPink, out earSkinL);
        p.earR = BuildEar("EarPivot_R", p.body, b.SurfacePoint(70f, -20f), body, earPink, out earSkinR);

        p.skin = new Renderer[]
        {
            b.GetComponent<MeshRenderer>(), footL.GetComponent<MeshRenderer>(), footR.GetComponent<MeshRenderer>(),
            earSkinL, earSkinR
        };

        // 눈 (까만 콩 + 반짝이) + 감동받았을 때의 ^^ 눈 (입 메시를 ∩ 모양으로 뒤집어 씀)
        for (int side = -1; side <= 1; side += 2)
        {
            string s = side < 0 ? "_L" : "_R";
            float lon = -90f + side * 24f;
            Mid_BlobMesh eye = Blob("Eye" + s, p.body, Vector3.zero, eyeBlack, 0.045f, 0.03f, 0.055f, 1f, false);
            OnSurface(eye.transform, b, 30f, lon, 0.0f);
            Blob("Shine" + s, eye.transform, new Vector3(0.015f, 0.022f, -0.022f), eyeWhite,
                 0.013f, 0.008f, 0.013f, 1f, false);

            Mid_ArcMesh happy = Arc("HappyEye" + s, p.body, eyeBlack, 0.038f, 0.013f, 150f);
            OnSurface(happy.transform, b, 30f, lon, 0.006f);

            if (side < 0) { p.eyeL = eye.transform; p.happyEyeL = happy.transform; }
            else          { p.eyeR = eye.transform; p.happyEyeR = happy.transform; }
        }

        // 코
        Mid_BlobMesh nose = Blob("Nose", p.body, Vector3.zero, noseMat, 0.028f, 0.02f, 0.02f, 1f, false);
        OnSurface(nose.transform, b, 19f, -90f, 0.0f);

        // 입 (시작은 y 뒤집기 = 시무룩)
        Mid_ArcMesh m = Arc("Mouth", p.body, mouthMat, 0.05f, 0.014f, 140f);
        OnSurface(m.transform, b, 9f, -90f, 0.004f);
        p.mouth = m.transform;

        // 볼터치 (시작은 크기 0, 점점 커지며 발그레)
        Mid_BlobMesh bl = Blob("Blush_L", p.body, Vector3.zero, blushMat, 0.085f, 0.014f, 0.05f, 1f, false);
        OnSurface(bl.transform, b, 15f, -90f - 42f, 0.0f);
        Mid_BlobMesh br = Blob("Blush_R", p.body, Vector3.zero, blushMat, 0.085f, 0.014f, 0.05f, 1f, false);
        OnSurface(br.transform, b, 15f, -90f + 42f, 0.0f);
        p.blushL = bl.transform;
        p.blushR = br.transform;

        return p;
    }

    // =====================================================================
    // 배경: 비 오는 날의 아늑한 숲속 마을 느낌
    //   잔디 바닥 + 캐릭터가 서 있는 흙길 + 뒤쪽 층층이 언덕 2단 + 둥근 나무
    //   + 개구리가 연잎을 따 온 연못(왼쪽 뒤) + 장마철 수국 덤불
    // =====================================================================
    static void BuildBackground(Material matLotus)
    {
        Material grass     = LitMaterial("Grass",       new Color(0.40f, 0.68f, 0.32f), 0.25f);
        Material grassFar  = LitMaterial("GrassFar",    new Color(0.47f, 0.72f, 0.38f), 0.20f);
        Material path      = LitMaterial("Path",        new Color(0.86f, 0.76f, 0.58f), 0.30f);
        Material cliff     = LitMaterial("Cliff",       new Color(0.86f, 0.58f, 0.42f), 0.20f);
        Material leaves    = LitMaterial("TreeLeaves",  new Color(0.30f, 0.58f, 0.30f), 0.30f);
        Material leaves2   = LitMaterial("TreeLeaves2", new Color(0.42f, 0.66f, 0.28f), 0.30f);
        Material trunk     = LitMaterial("TreeTrunk",   new Color(0.55f, 0.38f, 0.26f), 0.20f);
        Material water     = LitMaterial("Water",       new Color(0.40f, 0.70f, 0.86f), 0.95f);
        Material shore     = LitMaterial("Shore",       new Color(0.80f, 0.74f, 0.58f), 0.30f);
        Material bush      = LitMaterial("Bush",        new Color(0.30f, 0.55f, 0.32f), 0.30f);
        Material[] flowers =
        {
            LitMaterial("HydrangeaBlue",   new Color(0.55f, 0.66f, 0.98f), 0.40f),
            LitMaterial("HydrangeaPurple", new Color(0.72f, 0.60f, 0.95f), 0.40f),
            LitMaterial("HydrangeaPink",   new Color(0.96f, 0.66f, 0.84f), 0.40f),
        };

        Transform root = new GameObject("Background").transform;

        // 잔디 바닥 (배경용 기본 Plane)
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Grass";
        ground.transform.SetParent(root, false);
        ground.transform.localScale = new Vector3(4f, 1f, 4f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = grass;

        // 흙길: 납작한 타원 여러 개를 이어 왼쪽에서 오른쪽으로 구불구불하게
        Transform pathRoot = new GameObject("Path").transform;
        pathRoot.SetParent(root, false);
        for (int i = 0; i < 12; i++)
        {
            float x = -6f + i * 0.85f;
            float z = -0.05f + Mathf.Sin(i * 0.9f) * 0.18f;
            Mid_BlobMesh piece = Blob("PathPiece", pathRoot, new Vector3(x, 0.003f, z), path,
                                      0.75f, 0.62f + 0.08f * Mathf.Sin(i * 1.7f), 0.004f, 1f, true);
            piece.stacks = 3; piece.Rebuild();
        }

        // 층층이 언덕: 앞(낮음), 뒤(높음)
        Terrace("Terrace_Near", root, new Vector3(0f, 0f, 3.6f), 0.9f, 4f, 0.45f, 4.5f, cliff, grass);
        Terrace("Terrace_Far",  root, new Vector3(1.5f, 0f, 7.6f), 2.0f, 8f, 0.6f, 6.5f, cliff, grassFar);

        // 둥근 나무: 앞 언덕 위, 뒤 언덕 위, 양쪽 끝
        Tree(root, new Vector3(-3.6f, 0.9f, 4.4f), 1.0f, leaves, trunk);
        Tree(root, new Vector3(-1.4f, 0.9f, 5.0f), 0.8f, leaves2, trunk);
        Tree(root, new Vector3( 2.6f, 0.9f, 4.6f), 1.1f, leaves, trunk);
        Tree(root, new Vector3( 4.6f, 0.9f, 4.2f), 0.9f, leaves2, trunk);
        Tree(root, new Vector3(-5.5f, 2.0f, 8.5f), 1.3f, leaves2, trunk);
        Tree(root, new Vector3( 0.5f, 2.0f, 8.8f), 1.2f, leaves, trunk);
        Tree(root, new Vector3( 5.8f, 2.0f, 8.4f), 1.4f, leaves2, trunk);
        Tree(root, new Vector3(-4.6f, 0f, 1.9f), 0.9f, leaves, trunk);
        Tree(root, new Vector3( 4.2f, 0f, 1.6f), 0.95f, leaves2, trunk);

        // 연잎 연못 (왼쪽 뒤): 모래 테두리 + 물 + 떠 있는 연잎 (연잎 우산과 같은 메시, 줄기 없이)
        Transform pond = new GameObject("LotusPond").transform;
        pond.SetParent(root, false);
        pond.localPosition = new Vector3(-2.5f, 0f, 1.7f);
        Mid_BlobMesh shoreBlob = Blob("Shore", pond, new Vector3(0f, 0.004f, 0f), shore, 1.25f, 0.85f, 0.006f, 1f, true);
        shoreBlob.segments = 40; shoreBlob.stacks = 3; shoreBlob.Rebuild();
        Mid_BlobMesh waterBlob = Blob("Water", pond, new Vector3(0f, 0.008f, 0f), water, 1.1f, 0.72f, 0.006f, 1f, true);
        waterBlob.segments = 40; waterBlob.stacks = 3; waterBlob.Rebuild();
        Vector3[] pads = { new Vector3(-0.45f, 0f, 0.15f), new Vector3(0.35f, 0f, 0.25f), new Vector3(0.05f, 0f, -0.3f), new Vector3(0.65f, 0f, -0.15f) };
        float[] padSize = { 0.28f, 0.22f, 0.25f, 0.16f };
        for (int i = 0; i < pads.Length; i++)
        {
            GameObject padGO = new GameObject("LilyPad");
            padGO.transform.SetParent(pond, false);
            padGO.transform.localPosition = pads[i] + new Vector3(0f, 0.012f, 0f);
            padGO.transform.localRotation = Quaternion.Euler(0f, i * 73f, 0f);
            Mid_LotusUmbrella pad = padGO.AddComponent<Mid_LotusUmbrella>();
            pad.stemHeight = 0.01f;
            pad.stemRadius = 0.001f;
            pad.leafRadius = padSize[i];
            pad.domeHeight = 0.01f;
            pad.waveRadius = 0.015f;
            pad.waveHeight = 0.004f;
            pad.material = matLotus;
            pad.Rebuild();
        }

        // 수국 덤불: 오른쪽 뒤(토끼 뒤), 연못 옆
        Hydrangea(root, new Vector3(2.1f, 0f, 1.5f), 1.0f, bush, flowers, 0);
        Hydrangea(root, new Vector3(3.2f, 0f, 2.3f), 0.8f, bush, flowers, 1);
        Hydrangea(root, new Vector3(-1.1f, 0f, 2.4f), 0.75f, bush, flowers, 2);
    }

    static void Terrace(string name, Transform parent, Vector3 pos, float height, float depth,
                        float wave, float waveLength, Material cliffMat, Material topMat)
    {
        Transform t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = pos;
        for (int k = 0; k < 2; k++)
        {
            GameObject go = new GameObject(k == 0 ? "Cliff" : "GrassTop");
            go.transform.SetParent(t, false);
            Mid_TerraceMesh m = go.AddComponent<Mid_TerraceMesh>();
            m.part = k == 0 ? Mid_TerraceMesh.Part.Cliff : Mid_TerraceMesh.Part.Top;
            m.height = height;
            m.depth = depth;
            m.waveAmount = wave;
            m.waveLength = waveLength;
            m.material = k == 0 ? cliffMat : topMat;
            m.Rebuild();
        }
    }

    // 둥근 나무: 짧은 줄기 + 몽글몽글한 잎 덩어리 3개
    static void Tree(Transform parent, Vector3 pos, float size, Material leaves, Material trunk)
    {
        Transform t = new GameObject("Tree").transform;
        t.SetParent(parent, false);
        t.localPosition = pos;
        t.localScale = Vector3.one * size;
        Blob("Trunk", t, Vector3.zero, trunk, 0.1f, 0.1f, 0.55f, 0.1f, true);
        Blob("Leaves", t, new Vector3(0f, 0.42f, 0f), leaves, 0.55f, 0.5f, 0.6f, 0.75f, true);
        Blob("Leaves", t, new Vector3(-0.32f, 0.5f, 0.05f), leaves, 0.36f, 0.34f, 0.36f, 0.8f, true);
        Blob("Leaves", t, new Vector3(0.3f, 0.62f, -0.05f), leaves, 0.34f, 0.32f, 0.34f, 0.8f, true);
    }

    // 수국 덤불: 초록 덤불 위에 동그란 꽃송이를 표면을 따라 붙임
    static void Hydrangea(Transform parent, Vector3 pos, float size, Material bushMat, Material[] flowerMats, int colorOffset)
    {
        Transform t = new GameObject("Hydrangea").transform;
        t.SetParent(parent, false);
        t.localPosition = pos;
        t.localScale = Vector3.one * size;
        Mid_BlobMesh b = Blob("Bush", t, Vector3.zero, bushMat, 0.5f, 0.38f, 0.38f, 0.4f, true);
        float[] lat = { 55f, 35f, 35f, 30f, 15f, 15f, 50f, 25f };
        float[] lon = { -90f, -140f, -40f, -95f, -170f, -10f, 160f, 120f };
        for (int i = 0; i < lat.Length; i++)
        {
            Material m = flowerMats[(i + colorOffset) % flowerMats.Length];
            Blob("Flower", t, b.SurfacePoint(lat[i], lon[i]) - new Vector3(0f, 0.06f, 0f), m,
                 0.13f, 0.13f, 0.12f, 0.8f, true);
        }
    }

    static Transform BuildEar(string name, Transform parent, Vector3 localPos, Material body, Material inner,
                              out Renderer earSkin)
    {
        Transform pivot = new GameObject(name + " (회전축)").transform;
        pivot.SetParent(parent, false);
        pivot.localPosition = localPos;

        Mid_BlobMesh ear = Blob("Ear", pivot, new Vector3(0f, -0.05f, 0f), body, 0.085f, 0.05f, 0.42f, 0.25f, true);
        Blob("EarInner", ear.transform, new Vector3(0f, 0.07f, -0.04f), inner, 0.045f, 0.02f, 0.32f, 0.25f, true);
        earSkin = ear.GetComponent<MeshRenderer>();
        return pivot;
    }

    // =====================================================================
    // 공통 도우미
    // =====================================================================
    static Mid_BlobMesh Blob(string name, Transform parent, Vector3 localPos, Material mat,
                             float radiusX, float radiusZ, float topHeight, float bottomSquash, bool pivotAtBottom)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        Mid_BlobMesh blob = go.AddComponent<Mid_BlobMesh>();
        blob.radiusX = radiusX;
        blob.radiusZ = radiusZ;
        blob.topHeight = topHeight;
        blob.bottomSquash = bottomSquash;
        blob.pivotAtBottom = pivotAtBottom;
        blob.material = mat;
        blob.Rebuild();
        return blob;
    }

    static Mid_ArcMesh Arc(string name, Transform parent, Material mat, float radius, float thickness, float arcAngle)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Mid_ArcMesh arc = go.AddComponent<Mid_ArcMesh>();
        arc.radius = radius;
        arc.thickness = thickness;
        arc.arcAngle = arcAngle;
        arc.material = mat;
        arc.Rebuild();
        return arc;
    }

    // 얼굴 부품을 몸 표면 (위도, 경도) 위치에 붙이고, 표면 방향에 맞게 돌림 (부품의 앞 = -z)
    static void OnSurface(Transform part, Mid_BlobMesh body, float lat, float lon, float push)
    {
        Vector3 n = body.SurfaceNormal(lat, lon);
        part.localPosition = body.SurfacePoint(lat, lon) + n * push;
        part.localRotation = Quaternion.FromToRotation(Vector3.back, n);
    }

    static Material LitMaterial(string name, Color color, float smoothness)
    {
        string path = MID_DIR + "/Mat_Mid_" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", smoothness);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static Material GLMaterial(string name)
    {
        string path = MID_DIR + "/Mat_Mid_" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        Shader shader = Shader.Find("Custom/Mid_GLColor");
        if (shader == null) return null;
        mat = new Material(shader);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static void SetupCameraAndLight()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0.1f, 1.25f, -4.3f);
            cam.transform.LookAt(new Vector3(0.1f, 0.78f, 0f));
            cam.fieldOfView = 45f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = SkyColor;
        }

        Light sun = Object.FindFirstObjectByType<Light>();
        if (sun != null)
        {
            sun.name = "Evening Light";
            sun.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
            sun.color = new Color(1.0f, 0.97f, 0.92f);
            sun.intensity = 1.0f;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.60f, 0.64f, 0.72f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.035f;
        RenderSettings.fogColor = SkyColor;
    }
}
#endif
