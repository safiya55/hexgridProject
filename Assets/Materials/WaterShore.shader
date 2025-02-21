Shader "Custom/WaterShore" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Standard alpha vertex:vert
		#pragma target 3.0

		#include "Water.cginc"
		#include "HexCellData.hlsl"

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
			float3 worldPos;
			float visibility;
			float2 visibility;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		void vert (inout appdata_full v, out Input data) {
			UNITY_INITIALIZE_OUTPUT(Input, data);

			float4 cell0 = GetCellData(v, 0);
			float4 cell1 = GetCellData(v, 1);
			float4 cell2 = GetCellData(v, 2);

			data.visibility.x =
				cell0.x * v.color.x + cell1.x * v.color.y + cell2.x * v.color.z;
			data.visibility.x = lerp(0.25, 1, data.visibility.x);
			data.visibility.y =
				cell0.y * v.color.x + cell1.y * v.color.y + cell2.y * v.color.z;
		}

		void surf (Input IN, inout SurfaceOutputStandard o) {
			float shore = IN.uv_MainTex.y;
			float foam = Foam(shore, IN.worldPos.xz, _MainTex);
			float waves = Waves(IN.worldPos.xz, _MainTex);
			waves *= 1 - shore;


			fixed4 c = fixed4(IN.uv_MainTex, 1, 1);
			float explored = IN.visibility.y;
			o.Albedo = c.rgb * IN.visibility.x;
			o.Specular = _Specular * explored;
			o.Smoothness = _Glossiness;
			o.Occlusion = explored;
			o.Alpha = c.a * explored;
		}
		ENDCG
	}
	FallBack "Diffuse"
}