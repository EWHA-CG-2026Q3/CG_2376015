// Mid_LotusUmbrella.cs
// 중간 프로젝트 — 연잎 우산 메시 (필수 ①: 반복문으로 정점을 만든 새 형태의 메시)
//
// 구조
//   - 줄기(손잡이): 원기둥. 원 위에 점 n개를 아래/위 두 층으로 놓고 옆면을 삼각형으로 이음
//   - 잎(우산 천): 가운데 꼭짓점 + 동심원 여러 층.
//                  바깥으로 갈수록 반지름에 sin 물결을 더해 연잎 가장자리처럼 구불구불하게 만듦
//   - 잎은 위에서 봐도, 아래(우산 안쪽)에서 봐도 보이도록 앞면/뒷면을 따로 만듦
//
// 피벗(원점) = 줄기 맨 아래 = 손으로 잡는 곳.
// 그래서 이 오브젝트를 캐릭터의 "손" 오브젝트의 자식으로 두면, 손을 회전시킬 때 우산이 손을 축으로 기울어짐.
using UnityEngine;

[ExecuteAlways]
public class Mid_LotusUmbrella : Mid_ProceduralMesh
{
    [Header("잎 (우산 천)")]
    [Range(8, 64)] public int segments = 32;      // 원 위의 점 개수 n
    [Range(1, 8)]  public int rings = 4;          // 가운데에서 가장자리까지 동심원 층 수
    public float leafRadius = 1.1f;               // 잎 반지름
    public float domeHeight = 0.35f;              // 가운데가 가장자리보다 얼마나 높은지 (볼록한 정도)
    [Range(0, 12)] public int waveCount = 7;      // 가장자리 물결 개수
    public float waveRadius = 0.08f;              // 물결이 안팎으로 출렁이는 정도
    public float waveHeight = 0.06f;              // 물결이 위아래로 출렁이는 정도

    [Header("줄기 (손잡이)")]
    public float stemHeight = 1.3f;               // 손잡이 길이 (잎 가운데까지)
    public float stemRadius = 0.035f;

    void Reset()
    {
        baseColor = new Color(0.35f, 0.65f, 0.25f);
        smoothness = 0.6f;   // 젖은 잎처럼 살짝 반짝이게
    }

    protected override void BuildShape()
    {
        BuildStem();
        BuildLeaf(true);   // 윗면 (하늘 쪽)
        BuildLeaf(false);  // 아랫면 (우산 안쪽)
    }

    // ---------- 줄기: 원기둥 옆면 ----------
    void BuildStem()
    {
        int bottomStart = vertices.Count;
        for (int i = 0; i < segments; i++)            // 아래 원
        {
            float angle = 360f / segments * i * Mathf.Deg2Rad;
            AddVertex(new Vector3(Mathf.Cos(angle) * stemRadius, 0f, Mathf.Sin(angle) * stemRadius));
        }
        int topStart = vertices.Count;
        for (int i = 0; i < segments; i++)            // 위 원
        {
            float angle = 360f / segments * i * Mathf.Deg2Rad;
            AddVertex(new Vector3(Mathf.Cos(angle) * stemRadius, stemHeight, Mathf.Sin(angle) * stemRadius));
        }

        for (int i = 0; i < segments; i++)            // 옆면: 사각형 하나 = 삼각형 2개
        {
            int next = (i + 1) % segments;
            int b0 = bottomStart + i, b1 = bottomStart + next;
            int t0 = topStart + i,    t1 = topStart + next;
            AddTriangle(b0, t0, b1);
            AddTriangle(b1, t0, t1);
        }
    }

    // ---------- 잎: 가운데 꼭짓점 + 물결치는 동심원 ----------
    void BuildLeaf(bool topSide)
    {
        // 가운데 꼭짓점 (줄기 꼭대기와 같은 높이)
        int center = AddVertex(new Vector3(0f, stemHeight, 0f));

        // 동심원 정점: k번째 층, i번째 점
        int ringStart = vertices.Count;
        for (int k = 1; k <= rings; k++)
        {
            float t = (float)k / rings;                       // 0(가운데) ~ 1(가장자리)
            for (int i = 0; i < segments; i++)
            {
                float angle = 360f / segments * i * Mathf.Deg2Rad;
                float wave = Mathf.Sin(waveCount * angle);    // -1 ~ 1, 물결 모양

                // 바깥으로 갈수록 물결이 커짐 (t*t)
                float r = leafRadius * t + waveRadius * wave * t * t;
                // 가운데가 높고 가장자리로 갈수록 둥글게 내려감 + 가장자리 물결
                float y = stemHeight - domeHeight * t * t + waveHeight * wave * t * t;

                AddVertex(new Vector3(Mathf.Cos(angle) * r, y, Mathf.Sin(angle) * r));
            }
        }

        // 1) 가운데 점 + 첫 번째 원의 이웃한 두 점 → 부채꼴 삼각형
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            AddLeafTriangle(topSide, center, ringStart + next, ringStart + i);
        }

        // 2) 안쪽 원과 바깥 원 사이 → 사각형 하나 = 삼각형 2개
        for (int k = 0; k < rings - 1; k++)
        {
            int inner = ringStart + k * segments;
            int outer = ringStart + (k + 1) * segments;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                AddLeafTriangle(topSide, inner + i, outer + next, outer + i);
                AddLeafTriangle(topSide, inner + i, inner + next, outer + next);
            }
        }
    }

    // 윗면은 그대로, 아랫면은 정점 순서를 뒤집어서 반대쪽을 보게 함
    void AddLeafTriangle(bool topSide, int a, int b, int c)
    {
        if (topSide) AddTriangle(a, b, c);
        else         AddTriangle(a, c, b);
    }
}
