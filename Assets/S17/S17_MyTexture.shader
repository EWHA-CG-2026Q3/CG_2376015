Shader "CG/S17_MyTexture"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            sampler2D _BaseMap;

            void vert(float3 positionOS : POSITION,
                      float2 uv : TEXCOORD0,
                      out float2 uvOut : TEXCOORD0,
                      out float4 positionCS : SV_POSITION)
            {
                uvOut = uv;
                positionCS = TransformObjectToHClip(positionOS);
            }

            float4 frag(float2 uv : TEXCOORD0) : SV_Target
            {
                float2 p = uv;

                // 미션 1: B2 한 칸만 표면 전체에
                //   B2 = 위에서 둘째 줄, 왼쪽에서 둘째 칸 -> u 0.25~0.5, v 0.5~0.75
                // p = uv * 0.25 + float2(0.25, 0.5);

                // 미션 2: 그림 전체를 표면의 왼쪽 아래 1/4에
                // p = uv * 2;
                // if (p.x > 1 || p.y > 1) return float4(0.8, 0.8, 0.8, 1);

                // 과제 3: D3 한 칸만 표면 전체에
                //   D3 = 아래에서 첫째 줄, 왼쪽에서 셋째 칸 -> u 0.5~0.75, v 0~0.25
                //   0.25 = 칸 하나의 크기, (0.5, 0) = D3 칸의 왼쪽 아래 모서리
                p = uv * 0.25 + float2(0.5, 0);

                return tex2D(_BaseMap, p);
            }
            ENDHLSL
        }
    }
}
