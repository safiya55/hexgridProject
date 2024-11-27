Shader "Custom/VertexColors"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1) // This is the default color of the material (white).
        _MainTex ("Albedo (RGB)", 2D) = "white" {} // Default texture, set to white.
        _Glossiness ("Smoothness", Range(0,1)) = 0.5 // Smoothness of the material.
        _Metallic ("Metallic", Range(0,1)) = 0.0 // Metallic value of the material.
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex; // The albedo texture (which will be multiplied by vertex color).

        // Input struct for the shader.
        struct Input
        {
            float2 uv_MainTex; // The UV coordinates for the texture.
            float4 color : COLOR; // The vertex color that we want to use.
        };

        half _Glossiness; // Glossiness of the material.
        half _Metallic; // Metallic value of the material.
        fixed4 _Color; // The default color of the material.

        // The surface shader function.
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Fetch the albedo texture color and multiply it by the default color.
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            // Multiply the albedo color by the vertex color to apply vertex coloring.
            o.Albedo = c.rgb * IN.color.rgb; // Use only RGB channels for the vertex color.
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }

    FallBack "Diffuse" // Use the default diffuse shader as a fallback.
}
