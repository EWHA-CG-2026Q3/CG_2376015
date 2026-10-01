using UnityEngine;

// 과제: 다이아몬드를 기울이는(shear) 4×4 행렬 ShearMatrixRaw(float k) 작성
// 점 (x, y, z) → (x + k·y, y, z)



[ExecuteAlways]
[RequireComponent(typeof(DiamondMesh))]
public class S09_Shear : MonoBehaviour
{
    [SerializeField] float k = 1f;  

    DiamondMesh diamondMesh;
    float lastLoggedK = float.NaN;

    void OnEnable()
    {
        diamondMesh = GetComponent<DiamondMesh>();
    }

    void Update()
    {
        if (diamondMesh == null || diamondMesh.BaseVertices == null) return;

        float[,] H = ShearMatrixRaw(k);
        Vector3[] baseVertices = diamondMesh.BaseVertices;
        Vector3[] verts = new Vector3[baseVertices.Length];
        for (int i = 0; i < baseVertices.Length; i++)
            verts[i] = FromHomogeneous(MultiplyMatrixVectorRaw(H, ToHomogeneous(baseVertices[i])));
        diamondMesh.SetVertices(verts);

        // k가 바뀔 때만 꼭대기 정점 (0.5, 1, 0.5)의 결과를 출력
        if (!Mathf.Approximately(k, lastLoggedK))
        {
            Vector3 top = new Vector3(0.5f, 1f, 0.5f);
            Vector3 result = FromHomogeneous(MultiplyMatrixVectorRaw(H, ToHomogeneous(top)));
            Debug.Log($"k = {k} : 꼭대기 {top} → {result}");
            lastLoggedK = k;
        }
    }

    // shear 행렬: e₁, e₃, 원점은 그대로, e₂만 (k, 1, 0, 0)으로 감
    float[,] ShearMatrixRaw(float k)
    {
        return new float[,] {
            { 1f, k,  0f, 0f },
            { 0f, 1f, 0f, 0f },
            { 0f, 0f, 1f, 0f },
            { 0f, 0f, 0f, 1f }
        };
    }

    Vector4 ToHomogeneous(Vector3 v)
    {
        return new Vector4(v.x, v.y, v.z, 1f);
    }

    Vector3 FromHomogeneous(Vector4 h)
    {
        return new Vector3(h.x, h.y, h.z);
    }

    Vector4 MultiplyMatrixVectorRaw(float[,] M, Vector4 v)
    {
        float[] input = { v.x, v.y, v.z, v.w };
        float[] result = new float[4];
        for (int row = 0; row < 4; row++)
            for (int col = 0; col < 4; col++)
                result[row] += M[row, col] * input[col];
        return new Vector4(result[0], result[1], result[2], result[3]);
    }
}