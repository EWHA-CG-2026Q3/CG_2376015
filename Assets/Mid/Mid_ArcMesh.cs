// Mid_ArcMesh.cs
// 중간 프로젝트 — 입 모양 메시 (활처럼 휜 띠)
//
// 원의 아래쪽 호(arc)를 따라 점을 반복문으로 놓고, 안쪽/바깥쪽 두 줄을 삼각형으로 이어 띠를 만듦.
//   - 그대로 두면 U 모양 = 웃는 입
//   - localScale.y 를 -1로 뒤집으면 ∩ 모양 = 시무룩한 입
// 앞뒤 어느 쪽에서 봐도 보이도록 양면으로 만듦 (그래서 y를 뒤집어도 사라지지 않음).
// 원점은 입 모양의 한가운데라서 뒤집어도 제자리에 있음.
using UnityEngine;

[ExecuteAlways]
public class Mid_ArcMesh : Mid_ProceduralMesh
{
    [Header("입 모양")]
    [Range(2, 32)] public int segments = 12;
    public float radius = 0.08f;                     // 호의 반지름 (입의 크기)
    public float thickness = 0.02f;                  // 띠의 두께 (입술 굵기)
    [Range(10f, 300f)] public float arcAngle = 140f; // 호가 얼마나 둥근지 (도)

    void Reset()
    {
        baseColor = new Color(0.25f, 0.12f, 0.1f);
        smoothness = 0.3f;
    }

    protected override void BuildShape()
    {
        float start = 270f - arcAngle * 0.5f;     // 270도 = 원의 맨 아래
        float rIn  = radius - thickness * 0.5f;
        float rOut = radius + thickness * 0.5f;

        // 호의 위쪽 끝과 아래쪽 끝의 가운데가 원점에 오도록 y를 옮김
        float topY = Mathf.Sin(start * Mathf.Deg2Rad) * radius;
        float bottomY = -radius;
        float shiftY = -(topY + bottomY) * 0.5f;

        // 앞면과 뒷면은 정점을 따로 만들어야 법선이 서로 반대 방향으로 제대로 계산됨
        for (int side = 0; side < 2; side++)
        {
            int startIndex = vertices.Count;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (start + arcAngle * i / segments) * Mathf.Deg2Rad;
                float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
                AddVertex(new Vector3(c * rIn,  s * rIn  + shiftY, 0f));   // 짝수: 안쪽
                AddVertex(new Vector3(c * rOut, s * rOut + shiftY, 0f));   // 홀수: 바깥쪽
            }

            for (int i = 0; i < segments; i++)
            {
                int in0 = startIndex + i * 2, out0 = in0 + 1;
                int in1 = in0 + 2,            out1 = in0 + 3;
                if (side == 0)
                {
                    AddTriangle(in0, out0, in1);
                    AddTriangle(in1, out0, out1);
                }
                else
                {
                    AddTriangle(in0, in1, out0);
                    AddTriangle(in1, out1, out0);
                }
            }
        }
    }
}
