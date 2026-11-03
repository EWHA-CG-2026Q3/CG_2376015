// Mid_GLColor.shader
// S06_OpenGL_Shader를 복제해 Mid용으로 이름을 바꾸고, 빗줄기가 반투명하게 보이도록 블렌딩만 추가함.
// GL.Color()로 넘긴 정점 색을 그대로 출력함.
Shader "Custom/Mid_GLColor"
{
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float4 color : COLOR; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.color = v.color;
                return o;
            }

            half4 frag (v2f i) : SV_Target { return i.color; }
            ENDHLSL
        }
    }
}
