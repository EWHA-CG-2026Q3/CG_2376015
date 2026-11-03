// Mid_RainyDayDirector.cs
// 중간 프로젝트 — 장면 연출 (필수 ④ 변환 + 선택 ④ 움직임 하나 더)
//
// 사용자 조작 없이, "지금 몇 초인지"에 따라 각 물체의 위치/회전/크기를 정함.
// loopLength초가 지나면 처음부터 다시 반복됨.
//
//   0초 ~          비 오는 저녁, 토끼가 귀를 늘어뜨린 채 오들오들 떪 (시무룩한 입)
//   frogEnter ~    개구리가 진행 방향을 보며 폴짝폴짝 걸어옴 (이동 + 회전 + 납작/길쭉 크기 변화)
//                  카메라 쪽으로 살짝 돌아 있어서(3/4 각도) 걸어오는 동안 얼굴이 보임
//   frogArrive ~   개구리가 토끼 쪽으로 몸을 돌림, 토끼도 개구리 쪽을 봄 (회전)
//   ~ tiltEnd      개구리가 "손"을 축으로 우산을 토끼 쪽으로 기울임 (Hierarchy: 개구리 → 몸 → 손 → 우산)
//   ~ cheerEnd     토끼가 떨림을 멈추고 귀가 스르륵 올라옴 (귀 회전), 입이 웃음 (크기 뒤집기)
//   ~ touchedEnd   감동: 눈이 ^^로 감기고, 볼이 크고 붉어지고, 개구리 쪽으로 살짝 기댐 (회전 + 이동 + 크기)
//   faceCamera ~   둘이 함께 카메라 쪽으로 돌아섬 → 한 우산 아래 나란히 선 마지막 장면
using UnityEngine;

public class Mid_RainyDayDirector : MonoBehaviour
{
    [Header("개구리")]
    public Transform frog;          // 개구리 전체 (이동, 몸 돌리기)
    public Transform frogBody;      // 개구리 몸 (폴짝 뛸 때 납작/길쭉)
    public Transform frogHand;      // 우산을 쥔 손 = 우산의 부모, 회전축
    public float frogStartX = -4.5f;
    public float frogEndX = -0.55f;
    public float hopHeight = 0.18f;
    public float frogWalkYaw = -60f;       // 걸어올 때 방향 (-90 = 완전 옆모습, 0 = 정면)
    public float frogLookRabbitYaw = -80f; // 토끼를 바라볼 때 방향
    public float tiltAngle = 30f;          // 우산을 기울이는 각도

    [Header("토끼")]
    public Transform rabbit;        // 토끼 전체 (몸 돌리기, 다가가기)
    public Transform rabbitBody;    // 토끼 몸 (오들오들, 기대기, 뭉클)
    public Transform earLeft;       // 귀 뿌리 (회전축)
    public Transform earRight;
    public Transform rabbitMouth;   // y 크기를 -1 → 1로 바꿔 시무룩 → 웃음
    public Transform blushLeft;     // 크기를 0 → 1로 키워 발그레
    public Transform blushRight;
    public Transform eyeLeft;       // 동그란 눈: y 크기를 줄여 감음
    public Transform eyeRight;
    public Transform happyEyeLeft;  // ^^ 눈: 크기를 0 → 1로
    public Transform happyEyeRight;
    public Color blushSoft = new Color(1.0f, 0.62f, 0.70f);  // 안심했을 때의 연한 분홍 볼
    public Color blushDeep = new Color(1.0f, 0.28f, 0.40f);  // 감동했을 때의 맑고 붉은 볼
    public float earDroopAngle = 110f;
    public float earUpAngle = 22f;  // 끝까지 빳빳이 서지 않고 살짝 벌어진 편안한 귀
    public float turnAngle = 25f;   // 개구리 쪽으로 몸을 돌리는 각도
    public float leanAngle = 9f;    // 감동받아 개구리 쪽으로 기대는 각도
    public float leanShift = 0.08f; // 개구리 쪽으로 다가가는 거리

    [Header("시간표 (초)")]
    public float frogEnter = 2.5f;
    public float frogArrive = 6.5f;
    public float lookRabbitEnd = 7.3f;
    public float tiltEnd = 9f;
    public float cheerEnd = 10.6f;     // 떨림 멈춤, 귀가 스르륵 올라감, 웃음
    public float touchedEnd = 12f;     // ^^ 눈, 온몸이 붉어짐, 개구리 쪽으로 기댐
    public float faceCameraStart = 12.6f;
    public float faceCameraEnd = 13.6f;
    public float loopLength = 17f;

    Vector3 rabbitBasePos;
    MaterialPropertyBlock blushBlock;
    Renderer blushRendererLeft, blushRendererRight;

    void Start()
    {
        rabbitBasePos = rabbit.localPosition;
        blushBlock = new MaterialPropertyBlock();
        if (blushLeft != null)  blushRendererLeft  = blushLeft.GetComponent<Renderer>();
        if (blushRight != null) blushRendererRight = blushRight.GetComponent<Renderer>();
    }

    void Update()
    {
        float t = Time.time % loopLength;
        AnimateFrog(t);
        AnimateRabbit(t);
    }

    // a ~ b 구간에서 0 → 1로 부드럽게 바뀌는 값 (구간 밖에서는 0 또는 1)
    static float Progress(float t, float a, float b)
    {
        float u = Mathf.Clamp01((t - a) / (b - a));
        return u * u * (3f - 2f * u);
    }

    void AnimateFrog(float t)
    {
        // 1) 걸어오기: 일정한 속도로 x 이동, 걷는 동안 폴짝폴짝
        float walk = Mathf.Clamp01((t - frogEnter) / (frogArrive - frogEnter));
        float x = Mathf.Lerp(frogStartX, frogEndX, walk);

        float y = 0f;
        float stretch = 0f;
        if (t > frogEnter && t < frogArrive)
        {
            float duration = frogArrive - frogEnter;
            float hops = Mathf.Round(duration * 1.8f);              // 도착할 때 딱 착지하도록 정수 번
            float phase = (t - frogEnter) / duration * hops;
            float air = Mathf.Abs(Mathf.Sin(Mathf.PI * phase));     // 0 = 땅, 1 = 가장 높이
            y = air * hopHeight;
            stretch = air - 0.5f;                                   // 떠 있을 땐 길쭉, 땅에선 납작
        }
        frog.localPosition = new Vector3(x, y, frog.localPosition.z);
        frogBody.localScale = new Vector3(1f - 0.08f * stretch, 1f + 0.14f * stretch, 1f - 0.08f * stretch);

        // 2) 몸 방향: 걸어오는 방향(3/4) → 토끼 쪽 → 마지막에 카메라 정면
        float yaw = Mathf.Lerp(frogWalkYaw, frogLookRabbitYaw, Progress(t, frogArrive, lookRabbitEnd));
        yaw = Mathf.Lerp(yaw, 0f, Progress(t, faceCameraStart, faceCameraEnd));
        frog.localRotation = Quaternion.Euler(0f, yaw, 0f);

        // 3) 우산 기울이기: 손을 회전 → 자식인 우산이 손을 축으로 기울어짐.
        //    개구리가 어느 쪽을 보든 늘 토끼 쪽으로 기울도록, 손에서 토끼로 향하는 방향을
        //    몸의 좌표계(local)로 바꿔서 그 방향으로 "위쪽"을 tilt도만큼 눕힘.
        float tilt = Progress(t, lookRabbitEnd, tiltEnd) * tiltAngle * Mathf.Deg2Rad;
        Vector3 toRabbit = frogBody.InverseTransformDirection(rabbit.position - frogHand.position);
        toRabbit.y = 0f;
        toRabbit = toRabbit.sqrMagnitude > 0.0001f ? toRabbit.normalized : Vector3.right;
        Vector3 tiltedUp = Vector3.up * Mathf.Cos(tilt) + toRabbit * Mathf.Sin(tilt);
        frogHand.localRotation = Quaternion.FromToRotation(Vector3.up, tiltedUp);
    }

    void AnimateRabbit(float t)
    {
        float cheer = Progress(t, tiltEnd - 0.4f, cheerEnd);     // 0 = 슬픔, 1 = 안심하고 웃음
        float touched = Progress(t, cheerEnd - 0.2f, touchedEnd); // 0 → 1 = 감동

        // 1) 오들오들: 안심할수록 떨림이 줄어듦
        //    감동받으면 개구리 쪽(-x)으로 살짝 기대고, 숨을 한 번 크게 쉬듯 몸이 부풀었다 돌아옴
        float shiver = 1f - cheer;
        float lean = touched * leanAngle;
        float breath = Mathf.Sin(Mathf.PI * touched) * 0.05f;
        rabbitBody.localPosition = new Vector3(Mathf.Sin(t * 55f) * 0.012f * shiver, 0f, 0f);
        rabbitBody.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 47f) * 1.5f * shiver + lean);
        rabbitBody.localScale = new Vector3(1f + breath, 1f + breath * 0.6f, 1f + breath);

        // 2) 귀: 처진 각도 → 스르륵 천천히 올라옴
        float ear = Mathf.Lerp(earDroopAngle, earUpAngle, cheer);
        earLeft.localRotation  = Quaternion.Euler(0f, 0f,  ear);   // 왼쪽 귀는 +z 회전 = 바깥(-x)으로 눕힘
        earRight.localRotation = Quaternion.Euler(0f, 0f, -ear);

        // 3) 입: 위아래로 뒤집기 (-1 = ∩ 시무룩, 1 = U 웃음)
        rabbitMouth.localScale = new Vector3(1f, Mathf.Lerp(-1f, 1f, cheer), 1f);

        // 4) 눈: 동그란 눈이 위아래로 납작해지고(감음) ^^ 눈이 나타남 (∩ 모양 = y 뒤집기)
        float open = 1f - Progress(t, cheerEnd - 0.2f, cheerEnd + 0.5f);
        float happy = Progress(t, cheerEnd + 0.2f, cheerEnd + 0.7f);
        //    (장면을 예전 버전으로 만들어서 ^^ 눈이 없으면 이 부분은 건너뜀)
        if (eyeLeft != null && eyeRight != null && happyEyeLeft != null && happyEyeRight != null)
        {
            eyeLeft.localScale  = new Vector3(1f, open, 1f);
            eyeRight.localScale = new Vector3(1f, open, 1f);
            happyEyeLeft.localScale  = new Vector3(happy, -happy, happy);
            happyEyeRight.localScale = new Vector3(happy, -happy, happy);
        }

        // 5) 볼터치: 안심하며 작고 연한 분홍 → 감동하며 크고 맑은 붉은색 (몸 색은 그대로)
        float blush = cheer * 0.6f + touched * 0.55f;
        blushLeft.localScale  = Vector3.one * blush;
        blushRight.localScale = Vector3.one * blush;
        Color blushColor = Color.Lerp(blushSoft, blushDeep, touched);
        SetColor(blushRendererLeft, blushColor);
        SetColor(blushRendererRight, blushColor);

        // 6) 개구리가 도착하면 그쪽(-x)으로 몸을 돌리고, 감동받으면 조금 다가감,
        //    마지막에 같이 카메라 쪽으로
        float turn = Progress(t, frogArrive - 0.3f, lookRabbitEnd) * turnAngle;
        turn = Mathf.Lerp(turn, 0f, Progress(t, faceCameraStart, faceCameraEnd));
        rabbit.localRotation = Quaternion.Euler(0f, turn, 0f);
        rabbit.localPosition = rabbitBasePos + new Vector3(-leanShift * touched, 0f, 0f);
    }

    // 머티리얼 파일은 그대로 두고(개구리 볼은 그대로), 이 렌더러에만 색을 덮어씀
    void SetColor(Renderer r, Color c)
    {
        if (r == null) return;
        r.GetPropertyBlock(blushBlock);
        blushBlock.SetColor("_BaseColor", c);
        r.SetPropertyBlock(blushBlock);
    }
}
