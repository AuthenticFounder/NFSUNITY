Shader "Custom/Standard Blend" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_SpecColor("Specular Color", Color) = (.2,.2,.2,1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_SpecGlossMap("Specular", 2D) = "white" {}
		_Metallic("Metallic", Range(0,1)) = 0.0
	}
		SubShader{
			Tags{
				"RenderType" = "Opaque"
				"IgnoreProjector" = "True"
				"Queue" = "Geometry+1"
				"ForceNoShadowCasting" = "True"}
			LOD 200

			CGPROGRAM
			// Physically based Standard lighting model, and enable shadows on all light types
			#pragma surface surf Standard fullforwardshadows decal:blend vertex:vert

			// Use shader model 3.0 target, to get nicer looking lighting
			#pragma target 3.0

			sampler2D _MainTex;
			sampler2D _SpecGlossMap;

			struct Input {
				float2 uv_MainTex;
				fixed4 vertexColor;
			};

			half _Glossiness;
			half _Metallic;
			fixed4 _Color;

			// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
			// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
			// #pragma instancing_options assumeuniformscaling
			UNITY_INSTANCING_CBUFFER_START(Props)
				// put more per-instance properties here
			UNITY_INSTANCING_CBUFFER_END

			void vert(inout appdata_full v, out Input o)
			{
				UNITY_INITIALIZE_OUTPUT(Input, o);
				o.vertexColor = v.color;
			}

			void surf(Input IN, inout SurfaceOutputStandard o) {
				// Albedo comes from a texture tinted by color
				fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
				fixed4 g = tex2D(_SpecGlossMap, IN.uv_MainTex) * _SpecColor;

				o.Albedo = c.rgb;
				// Metallic and smoothness come from slider variables
				o.Metallic = g.rgb * _Metallic;
				o.Smoothness = g.a * _Glossiness;
				o.Alpha = c.a * IN.vertexColor.a;
			}
			ENDCG
		}
			FallBack "Diffuse"
}
