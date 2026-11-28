using UnityEngine;

/// <summary>
/// 실드 이름표에 재질의 현재 Opacity 값을 함께 보여 준다 (녹화 영상에서 Opacity 변화를 확인하기 위한 표시용).
/// </summary>
public class S25_OpacityLabel : MonoBehaviour
{
    public Renderer target;      // Opacity를 읽을 실드
    public string title;         // 이름표 첫 줄

    TextMesh text;

    void Start() { text = GetComponent<TextMesh>(); }

    void Update()
    {
        if (text == null || target == null) return;
        float o = target.sharedMaterial.GetFloat("_Opacity");
        text.text = title + "\nOpacity " + o.ToString("0.00");
    }
}
