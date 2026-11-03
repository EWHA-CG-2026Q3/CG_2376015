// Mid_TerraceMesh.cs
// 중간 프로젝트 — 배경의 층층이 언덕 (선택 ①: 메시 하나 더)
//
// 앞쪽 가장자리가 sin 물결로 구불구불한 넓은 언덕을 반복문으로 만듦.
// 같은 스크립트를 두 오브젝트에 붙여서 part 값만 다르게 씀:
//   - Cliff : 앞쪽 절벽 면 (흙색). 정점마다 앞뒤로 살짝 울퉁불퉁하게 해서 바위 느낌
//   - Top   : 위쪽 잔디 면 (초록) + 앞쪽으로 살짝 튀어나와 아래로 늘어진 잔디 가장자리
//
// 원점 = 언덕 바닥 가운데. 앞쪽 = -z (카메라 쪽)
using UnityEngine;

[ExecuteAlways]
public class Mid_TerraceMesh : Mid_ProceduralMesh
{
    public enum Part { Cliff, Top }

    [Header("언덕")]
    public Part part = Part.Cliff;
    [Range(8, 200)] public int segments = 120;   // 좌우로 나누는 칸 수
    public float width = 30f;                    // 좌우 길이
    public float depth = 6f;                     // 앞뒤 길이 (잔디 면)
    public float height = 0.9f;                  // 절벽 높이
    public float waveAmount = 0.5f;              // 앞쪽 가장자리가 앞뒤로 구불구불한 정도
    public float waveLength = 5f;                // 구불구불한 한 번의 길이
    public float bumpiness = 0.05f;              // 절벽 면의 울퉁불퉁한 정도

    [Header("잔디 가장자리")]
    public float lipOut = 0.07f;                 // 잔디가 절벽보다 앞으로 튀어나온 길이
    public float lipDrop = 0.12f;                // 잔디가 절벽 위로 늘어진 길이

    // x 위치의 앞쪽 가장자리 z (두 개의 sin을 섞어 자연스럽게)
    float FrontZ(float x)
    {
        float k = 2f * Mathf.PI / waveLength;
        return waveAmount * (Mathf.Sin(k * x) * 0.7f + Mathf.Sin(k * 0.43f * x + 1.3f) * 0.3f);
    }

    protected override void BuildShape()
    {
        if (part == Part.Cliff) BuildCliff();
        else                    BuildTop();
    }

    float XAt(int i) { return -width * 0.5f + width * i / segments; }

    // 앞쪽 절벽: 아래 줄과 위 줄 정점을 놓고 사각형(삼각형 2개)으로 이음, 앞(-z)을 보게
    void BuildCliff()
    {
        int start = vertices.Count;
        for (int i = 0; i <= segments; i++)
        {
            float x = XAt(i);
            float bump = Mathf.Sin(x * 7.3f) * Mathf.Sin(x * 2.1f + 0.5f) * bumpiness;
            float z = FrontZ(x) + bump;
            AddVertex(new Vector3(x, 0f, z));          // 짝수: 아래
            AddVertex(new Vector3(x, height, z));      // 홀수: 위
        }
        AddStrip(start);
    }

    // 위쪽 잔디 면 + 앞으로 늘어진 잔디 가장자리
    void BuildTop()
    {
        // 1) 잔디 면: 앞 가장자리 줄과 뒤 줄, 위를 보게
        int start = vertices.Count;
        for (int i = 0; i <= segments; i++)
        {
            float x = XAt(i);
            AddVertex(new Vector3(x, height, FrontZ(x) - lipOut));   // 짝수: 앞
            AddVertex(new Vector3(x, height, depth));                // 홀수: 뒤
        }
        for (int i = 0; i < segments; i++)
        {
            int f0 = start + i * 2, k0 = f0 + 1, f1 = f0 + 2, k1 = f0 + 3;
            AddTriangle(f0, k0, f1);
            AddTriangle(f1, k0, k1);
        }

        // 2) 잔디 가장자리: 앞 가장자리에서 아래로 늘어진 띠, 앞을 보게
        int lip = vertices.Count;
        for (int i = 0; i <= segments; i++)
        {
            float x = XAt(i);
            float z = FrontZ(x) - lipOut;
            float drop = lipDrop * (0.75f + 0.25f * Mathf.Sin(x * 5.1f));   // 늘어진 길이도 조금씩 다르게
            AddVertex(new Vector3(x, height - drop, z));
            AddVertex(new Vector3(x, height, z));
        }
        AddStrip(lip);
    }

    // (아래, 위) 정점 쌍이 줄지어 있을 때 사각형으로 이어 앞(-z)을 보는 띠를 만듦
    void AddStrip(int start)
    {
        for (int i = 0; i < segments; i++)
        {
            int b0 = start + i * 2, t0 = b0 + 1, b1 = b0 + 2, t1 = b0 + 3;
            AddTriangle(b0, t0, b1);
            AddTriangle(b1, t0, t1);
        }
    }
}
