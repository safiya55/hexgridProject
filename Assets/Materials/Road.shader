Shader "Custom/Road"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        LOD 200

        Pass
        {
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariables.hlsl"
            
            // Properties for the shader
            sampler2D _MainTex;
            float4 _Color;
            half _Glossiness;
            half _Metallic;

            // Input structure
            struct Attributes
            {
                float3 position : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct FragmentInput
            {
                float2 uv : TEXCOORD0;
            };

            // Fragment shader output structure
            struct FragmentOutput
            {
                float4 color : SV_Target;
            };

            // Surface shader equivalent in URP
            void surf (FragmentInput i, inout FragmentOutput o)
            {
                // Sample the texture and multiply by the color
                float4 c = tex2D(_MainTex, i.uv) * _Color;
                o.color = c;
            }

            // Define the vertex shader
            void vert(inout Attributes v)
            {
                // Transform vertex position (standard for URP)
                v.position = TransformObjectToHClip(v.position);
            }

            // Assign the fragment shader to the pass
            FragmentOutput frag(FragmentInput i) : SV_Target
            {
                FragmentOutput o;
                surf(i, o);
                return o;
            }

            // Set up the shaders
            SubShader
            {
                Tags { "RenderType"="Opaque" }

                Pass
                {
                    Tags { "LightMode"="UniversalForward" }

                    HLSLPROGRAM
                    // More URP-specific setup if needed
                    ENDHLSL
                }

                Fallback "Diffuse"
            }
        }
    }
}
