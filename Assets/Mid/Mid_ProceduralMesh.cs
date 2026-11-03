// Mid_ProceduralMesh.cs
// 중간 프로젝트 — 우리 코드로 메시를 만드는 스크립트들의 공통 부분
//
// 하는 일
//   1. 자식 클래스가 BuildShape()에서 vertices(정점)와 triangles(삼각형 인덱스)를 채움
//   2. 그걸로 Mesh를 만들고 RecalculateNormals()로 법선을 계산해 MeshFilter에 넣음
//   3. 재질이 비어 있으면 Base Color로 URP Lit 재질을 자동으로 만들어 입힘
//
// [ExecuteAlways]라서 Play를 누르지 않아도 Scene 창에 모양이 보이고,
// Inspector에서 값을 바꾸면 바로 다시 만들어짐.
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public abstract class Mid_ProceduralMesh : MonoBehaviour
{
    [Header("재질 (비워 두면 Base Color로 URP Lit 재질을 자동으로 만듦)")]
    public Material material;
    public Color baseColor = Color.white;
    [Range(0f, 1f)] public float smoothness = 0.5f;

    protected readonly List<Vector3> vertices = new List<Vector3>();
    protected readonly List<int> triangles = new List<int>();

    Mesh mesh;
    Material autoMaterial;
    bool dirty;

    protected virtual void OnEnable() { Rebuild(); }

    // Inspector에서 값을 바꾸면 다음 프레임에 다시 만듦
    protected virtual void OnValidate() { dirty = true; }

    protected virtual void Update()
    {
        if (!dirty) return;
        dirty = false;
        Rebuild();
    }

    // 자식 클래스가 구현: vertices와 triangles를 채움
    protected abstract void BuildShape();

    public void Rebuild()
    {
        vertices.Clear();
        triangles.Clear();
        BuildShape();

        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = GetType().Name;
            mesh.hideFlags = HideFlags.DontSave;
        }
        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = mesh;

        ApplyMaterial();
    }

    protected int AddVertex(Vector3 v)
    {
        vertices.Add(v);
        return vertices.Count - 1;
    }

    // 정점 세 개(a, b, c)를 시계 방향으로 넘기면 그쪽이 앞면이 됨
    protected void AddTriangle(int a, int b, int c)
    {
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);
    }

    void ApplyMaterial()
    {
        var meshRenderer = GetComponent<MeshRenderer>();
        if (material != null)
        {
            meshRenderer.sharedMaterial = material;
            return;
        }
        if (autoMaterial == null)
        {
            autoMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            autoMaterial.hideFlags = HideFlags.DontSave;
        }
        autoMaterial.SetColor("_BaseColor", baseColor);
        autoMaterial.SetFloat("_Smoothness", smoothness);
        meshRenderer.sharedMaterial = autoMaterial;
    }
}
