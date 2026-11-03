// Mid_BlobMesh.cs
// 중간 프로젝트 — 동글동글한 몸통 메시 (선택 ①: 앞의 연잎과 모양이 다른 메시 하나 더)
//
// 만드는 방법: 지구본의 위도/경도처럼
//   - 위도(아래 → 위)로 원을 여러 층(stacks) 쌓고
//   - 각 층의 원 위에 점을 segments개 고르게 놓은 뒤
//   - 이웃한 두 층 사이를 사각형(삼각형 2개)으로, 맨 위/맨 아래는 꼭짓점과 부채꼴로 이음
//
// 아래쪽 반은 bottomSquash만큼 납작하게 눌러서 바닥에 앉은 찹쌀떡 같은 모양이 됨.
// 같은 스크립트를 값만 바꿔서 개구리 몸, 토끼 몸, 귀, 눈, 볼터치, 발에 모두 씀.
using UnityEngine;

[ExecuteAlways]
public class Mid_BlobMesh : Mid_ProceduralMesh
{
    [Header("모양")]
    [Range(6, 64)] public int segments = 24;            // 한 층의 원 위 점 개수
    [Range(3, 32)] public int stacks = 12;              // 아래 꼭짓점 ~ 위 꼭짓점 사이 층 수
    public float radiusX = 0.5f;                        // 좌우 반지름
    public float radiusZ = 0.5f;                        // 앞뒤 반지름
    public float topHeight = 0.5f;                      // 가운데 → 꼭대기 높이
    [Range(0.05f, 1f)] public float bottomSquash = 0.6f; // 아래쪽 반의 높이 비율 (작을수록 납작)
    public bool pivotAtBottom = true;                   // true면 원점이 바닥 (바닥에 바로 세우기 좋음)

    public float BottomHeight => topHeight * bottomSquash;
    public float CenterY => pivotAtBottom ? BottomHeight : 0f;
    public float TotalHeight => topHeight + BottomHeight;

    // 위도 lat(-90 아래 ~ 90 위), 경도 lon(도)에 있는 표면 위의 점.  lon = -90 이 앞(-z, 카메라 쪽)
    public Vector3 SurfacePoint(float latDeg, float lonDeg)
    {
        float lat = latDeg * Mathf.Deg2Rad;
        float lon = lonDeg * Mathf.Deg2Rad;
        float ring = Mathf.Cos(lat);
        float height = latDeg >= 0f ? topHeight : BottomHeight;
        return new Vector3(radiusX * ring * Mathf.Cos(lon),
                           CenterY + height * Mathf.Sin(lat),
                           radiusZ * ring * Mathf.Sin(lon));
    }

    // 그 점에서 바깥을 향하는 방향 (눈, 입을 표면에 맞춰 붙일 때 씀)
    public Vector3 SurfaceNormal(float latDeg, float lonDeg)
    {
        Vector3 p = SurfacePoint(latDeg, lonDeg);
        float height = latDeg >= 0f ? topHeight : BottomHeight;
        Vector3 n = new Vector3(p.x / (radiusX * radiusX),
                                (p.y - CenterY) / (height * height),
                                p.z / (radiusZ * radiusZ));
        return n.normalized;
    }

    protected override void BuildShape()
    {
        // 맨 아래 꼭짓점
        int bottomPole = AddVertex(SurfacePoint(-90f, 0f));

        // 가운데 층들: k = 1 ~ stacks-1
        int ringStart = vertices.Count;
        for (int k = 1; k < stacks; k++)
        {
            float lat = -90f + 180f * k / stacks;
            for (int i = 0; i < segments; i++)
            {
                float lon = 360f / segments * i;
                AddVertex(SurfacePoint(lat, lon));
            }
        }

        // 맨 위 꼭짓점
        int topPole = AddVertex(SurfacePoint(90f, 0f));

        int ringCount = stacks - 1;

        // 1) 아래 꼭짓점 + 첫 번째 층 → 부채꼴 (아래를 보게)
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            AddTriangle(bottomPole, ringStart + i, ringStart + next);
        }

        // 2) 이웃한 두 층 사이 → 사각형 하나 = 삼각형 2개 (바깥을 보게)
        for (int k = 0; k < ringCount - 1; k++)
        {
            int lower = ringStart + k * segments;
            int upper = ringStart + (k + 1) * segments;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                AddTriangle(lower + i, upper + i, lower + next);
                AddTriangle(lower + next, upper + i, upper + next);
            }
        }

        // 3) 마지막 층 + 위 꼭짓점 → 부채꼴 (위를 보게)
        int lastRing = ringStart + (ringCount - 1) * segments;
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            AddTriangle(topPole, lastRing + next, lastRing + i);
        }
    }
}
