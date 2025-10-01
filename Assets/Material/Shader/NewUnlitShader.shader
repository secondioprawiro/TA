// Shader untuk membuat bagian depan bertekstur dan bagian belakang solid
Shader "Custom/UI-FrontAndBack"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture (Front)", 2D) = "white" {}
        _Color ("Tint (Front)", Color) = (1,1,1,1)
        _BackColor ("Back Face Color", Color) = (0,0,0,1) // Properti untuk warna belakang
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        // Pass 1: Menggambar sisi belakang dengan warna solid
        Pass
        {
            Cull Front // Hanya gambar sisi belakang

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
            };
            
            float4 _BackColor;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return _BackColor; // Kembalikan warna solid untuk sisi belakang
            }
            ENDHLSL
        }

        // Pass 2: Menggambar sisi depan dengan tekstur
        Pass
        {
            Cull Back // Hanya gambar sisi depan (default)
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            sampler2D _MainTex;
            float4 _Color;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 color = tex2D(_MainTex, IN.uv) * IN.color;
                clip(color.a - 0.01);
                return color;
            }
            ENDHLSL
        }
    }
}