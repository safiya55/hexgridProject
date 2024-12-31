Shader "Custom/River"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Include URP libraries
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Texture and Property Declarations
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _Color;

            struct Attributes
            {
                float3 positionOS : POSITION;   // Object space position
                float2 uv : TEXCOORD0;         // UV coordinates
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;  // Homogeneous clip-space position
                float2 uv : TEXCOORD0;            // Pass UVs to the fragment shader
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS); // Transform position to clip space
                OUT.uv = IN.uv; // Pass UV
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Use UV coordinates for Albedo (encoded in the red and green channels)
                float2 uv = IN.uv;
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                // Encode UV coordinates in the Albedo
                half3 albedo = half3(uv, 0.0); // Pass UVs via red and green channels
                return half4(albedo, texColor.a); // Output albedo and alpha
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/InternalErrorShader"
}
