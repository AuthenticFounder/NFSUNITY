Shader "MoDyEn/Water" {
		Properties{
		_Color("Main Color", Color) = (1,1,1,1)
		_SpecColor("Specular Color", Color) = (0,0,0,1)
		_MainTex("Base (RGB)", 2D) = "white" {}
		_BumpMap("Bump", 2D) = "bump" {}
		_BumpMap2("Bump Secondary", 2D) = "bump" {}
		_BumpScale("Normal Scale", Float) = 1
		_SecondaryTile("Secondary Tile", Range(1,5)) = 1

		_Reflection("Reflection", Range(0,1)) = 0.5
		_Fresnel("Fresnel", Range(0,1)) = 0.2
		_WaveSpeed("Wave Speed", Range(0,1)) = 0.2
		_Glossiness("Smoothness", Range(0, 1)) = 0.9
		_FadeLength("Edge Fade", Range(0, 2)) = 1
		_ReflectionTex("ReflectionTex", 2D) = "white" {}
	}
	SubShader{
		//Tags{ "RenderType" = "Opaque" "IgnoreProjector" = "True"}
		Tags{ "Queue" = "Transparent-100" "RenderType" = "Transparent" }

		CGPROGRAM

		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows vertex:vert alpha:fade

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0	

		sampler2D _MainTex;
		sampler2D _BumpMap;
		sampler2D _BumpMap2;
		sampler2D _ReflectionTex;
		sampler2D _CameraDepthTexture;

		half4 _Color;
		half _SecondaryTile;
		half _Glossiness;
		half _WaveSpeed;
		half _Fresnel;
		half _Reflection;
		half _BumpScale;
		half _FadeLength;

		struct Input {
			half2 uv_MainTex;
			half3 viewDir;
			half3 worldNormal;
			half4 screenPos;
			half3 worldPos;
			half4 color : COLOR;
			INTERNAL_DATA
		};

		inline fixed4 CalculateFresnel(float3 _eye, float3 _normal, float rindexRatio)
		{
			float R0 = pow(1.0f - rindexRatio, 2.0f) / pow(1.0f + rindexRatio, 2.0f);
			return saturate(R0 + (1.0f - R0) * pow(1.0f - saturate(dot(_eye, _normal)), 5.0f));
		}

		void vert(inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input,o);
			o.worldNormal = mul((float3x3)unity_ObjectToWorld, SCALED_NORMAL);
			o.color = v.color;
		}

		void surf(Input IN, inout SurfaceOutputStandard o)
		//void surf(Input IN, inout SurfaceOutput o)
		{		
			
			// Scrolling UV's
			fixed2 scrolledUV1 = IN.uv_MainTex;
			fixed2 scrolledUV2 = IN.uv_MainTex;
			fixed xScroll = (_WaveSpeed * _Time);
			fixed yScroll = (_WaveSpeed * _Time);
			scrolledUV1 += fixed2(xScroll, yScroll);
			scrolledUV2 -= fixed2(xScroll, yScroll);

			// Diffuse color
			half4 c1 = tex2D(_MainTex, scrolledUV1) * _Color;
			half4 c2 = tex2D(_MainTex, scrolledUV2*_SecondaryTile) * _Color;
			half4 c = lerp(c1, c2, 0.5);
		
			// Bumpmaps
			half3 n1 = UnpackScaleNormal(tex2D(_BumpMap, scrolledUV1), _BumpScale);
			half3 n2 = UnpackScaleNormal(tex2D(_BumpMap, scrolledUV2*_SecondaryTile), _BumpScale);
			fixed3 localN = lerp(normalize(n1),normalize(n2),0.5);
			o.Normal = localN;

			// Fresnel
			fixed3 localE = normalize(IN.viewDir);
			fixed fresnel = CalculateFresnel(localE, localN, _Fresnel) * _Reflection;

			// Planar reflection
			half4 screenUVsAndNormal;
			screenUVsAndNormal.xy = IN.screenPos.xy / IN.screenPos.w;
			screenUVsAndNormal.zw = WorldNormalVector(IN, o.Normal).xz;
			float mip = ((1-_Glossiness) * 10) * fresnel;
			half4 lookup = half4(screenUVsAndNormal.x, screenUVsAndNormal.y, 0, mip);
			lookup.xy += clamp(-screenUVsAndNormal.zw * 1, -0.01f, 0.01f);

			half3 reflection = tex2Dbias(_ReflectionTex, lookup).rgb;
			
			// Depth
			half sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(IN.screenPos)));
			half surfZ = -mul(UNITY_MATRIX_V, float4(IN.worldPos.xyz, 1)).z;
			half edgeFade = saturate((sceneZ - surfZ) / _FadeLength);

			o.Albedo = c.rgb * IN.color.rgb * edgeFade;
			o.Emission = (reflection * _SpecColor.rgb * fresnel) * edgeFade;
			o.Alpha = c.a * edgeFade;

			//o.Emission = reflection * _SpecColor.rgb * fresnel;
			//o.Albedo = c.rgb * IN.color.rgb; // vertex RGB
		}
		ENDCG
	}
	FallBack "Diffuse"
}
