// Mid_RainGL.cs
// 중간 프로젝트 — 빗줄기 (선택 ③: GL 클래스로 선 그리기, S06 방식)
//
// 빗방울 위치를 배열로 들고 있다가
//   - Update(): 매 프레임 아래로(+바람 방향으로) 떨어뜨리고, 땅에 닿으면 다시 하늘 위로 보냄
//   - OnRenderObject(): GL.Begin(GL.LINES)로 빗방울마다 짧은 선 하나씩 직접 그림
// 우산(LeafCenter) 아래로 들어온 빗방울은 바로 하늘로 돌려보내서, 우산 밑에는 비가 안 내리게 함.
using UnityEngine;

[ExecuteAlways]
public class Mid_RainGL : MonoBehaviour
{
    [Header("GL로 그릴 때 쓸 재질 (Mid_GLColor 셰이더)")]
    public Material glMaterial;

    [Header("비")]
    public int dropCount = 450;
    public Vector3 areaSize = new Vector3(10f, 6f, 5f);   // 비가 내리는 상자 크기 (이 오브젝트 위치가 바닥 가운데)
    public float fallSpeed = 7f;
    public float dropLength = 0.28f;
    public Vector3 wind = new Vector3(-0.8f, 0f, 0f);
    public Color rainColor = new Color(0.78f, 0.85f, 1f, 0.45f);

    [Header("우산 아래에는 비가 안 내리게")]
    public Transform umbrellaLeafCenter;
    public float shelterRadius = 1.0f;

    Vector3[] drops;

    void OnEnable() { InitDrops(); }

    void InitDrops()
    {
        drops = new Vector3[dropCount];
        for (int i = 0; i < dropCount; i++)
            drops[i] = RandomPoint(Random.Range(0f, areaSize.y));
    }

    Vector3 RandomPoint(float height)
    {
        Vector3 basePos = transform.position;
        return new Vector3(basePos.x + Random.Range(-0.5f, 0.5f) * areaSize.x,
                           basePos.y + height,
                           basePos.z + Random.Range(-0.5f, 0.5f) * areaSize.z);
    }

    bool UnderUmbrella(Vector3 p)
    {
        if (umbrellaLeafCenter == null) return false;
        Vector3 c = umbrellaLeafCenter.position;
        float dx = p.x - c.x, dz = p.z - c.z;
        return p.y < c.y && dx * dx + dz * dz < shelterRadius * shelterRadius;
    }

    void Update()
    {
        if (drops == null || drops.Length != dropCount) InitDrops();
        if (!Application.isPlaying) return;   // 편집 중에는 멈춰 있는 비만 보여 줌

        Vector3 velocity = Vector3.down * fallSpeed + wind;
        float groundY = transform.position.y;
        for (int i = 0; i < drops.Length; i++)
        {
            drops[i] += velocity * Time.deltaTime;
            if (drops[i].y < groundY || UnderUmbrella(drops[i]))
                drops[i] = RandomPoint(areaSize.y + Random.Range(0f, 1f));
        }
    }

    void OnRenderObject()
    {
        if (glMaterial == null || drops == null) return;

        Vector3 dir = (Vector3.down * fallSpeed + wind).normalized;   // 빗방울이 떨어지는 방향
        Vector3 tail = -dir * dropLength;                             // 머리에서 꼬리까지

        glMaterial.SetPass(0);
        GL.Begin(GL.LINES);
        GL.Color(rainColor);
        for (int i = 0; i < drops.Length; i++)
        {
            if (UnderUmbrella(drops[i])) continue;
            GL.Vertex(drops[i]);          // 선의 시작 (빗방울 머리)
            GL.Vertex(drops[i] + tail);   // 선의 끝 (빗방울 꼬리)
        }
        GL.End();
    }
}
