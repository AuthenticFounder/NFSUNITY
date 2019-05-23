//puppet_master
//https://blog.csdn.net/puppet_master  
//2018.9.10  
//Screen Space Reflection效果，屏幕空间步进方式,DitherRayMarching
Shader "Reflection/ScreenSpaceReflection"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}

		CGINCLUDE

#include "UnityCG.cginc"

		struct appdata
	{
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
	};

	struct v2f
	{
		float4 vertex : SV_POSITION;
		float2 uv : TEXCOORD0;
		float3 viewRay : TEXCOORD1;
	};

	sampler2D _MainTex;
	float4 _MainTex_ST;
	float4 _ScreenSize;//(1 / width, 1 / height, width, height)
	sampler2D _CameraDepthTexture;
	sampler2D _CameraGBufferTexture0;
	sampler2D _CameraGBufferTexture1;
	sampler2D _CameraGBufferTexture2;
	sampler2D _CameraGBufferTexture3;
	sampler2D _CameraReflectionsTexture;

	float4 GetCubeMap(float2 uv) { return tex2D(_CameraReflectionsTexture, uv); }

	sampler2D _ditherMap;
	//使用外部传入的matrix，实际上等同于下面两个unity内置matrix
	//似乎在老版本unity中在pss阶段使用正交相机绘制Quad，矩阵会被替换，2017.3版本测试使用内置矩阵也可以
	float4x4 _InverseProjectionMatrix;//unity_CameraInvProjection
	float4x4 _CameraProjectionMatrix;//unity_CameraProjection
	float4x4 _WorldToCameraMatrix;
	float4x4 _ViewToScreenMatrix;

	float _maxRayMarchingDistance;
	float _maxRayMarchingStep;
	float _maxRayMarchingBinarySearchCount;
	float _rayMarchingStepSize;
	float _depthThickness;

	sampler2D _CameraDepthNormalsTexture;

	void swap(inout float v0, inout float v1)
	{
		float temp = v0;
		v0 = v1;
		v1 = temp;
	}

	float distanceSquared(float2 A, float2 B)
	{
		A -= B;
		return dot(A, A);
	}

	bool screenSpaceRayMarching(float3 rayOri, float3 rayDir, inout float2 hitScreenPos)
	{
		//反方向反射的，本身也看不见，索性直接干掉
		if (rayDir.z > 0.0)
			return false;
		//首先求得视空间终点位置，不超过最大距离
		float magnitude = _maxRayMarchingDistance;
		float end = rayOri.z + rayDir.z * magnitude;
		//如果光线反过来超过了近裁剪面，需要截取到近裁剪面
		if (end > -_ProjectionParams.y)
			magnitude = (-_ProjectionParams.y - rayOri.z) / rayDir.z;
		float3 rayEnd = rayOri + rayDir * magnitude;
		//直接把cliptoscreen与projection矩阵结合，得到齐次坐标系下屏幕位置
		float4 homoRayOri = mul(_ViewToScreenMatrix, float4(rayOri, 1.0));
		float4 homoRayEnd = mul(_ViewToScreenMatrix, float4(rayEnd, 1.0));
		//w
		float kOri = 1.0 / homoRayOri.w;
		float kEnd = 1.0 / homoRayEnd.w;
		//屏幕空间位置
		float2 screenRayOri = homoRayOri.xy * kOri;
		float2 screenRayEnd = homoRayEnd.xy * kEnd;
		screenRayEnd = (distanceSquared(screenRayEnd, screenRayOri) < 0.0001) ? screenRayOri + float2(0.01, 0.01) : screenRayEnd;

		float3 QOri = rayOri * kOri;
		float3 QEnd = rayEnd * kEnd;

		float2 displacement = screenRayEnd - screenRayOri;
		bool permute = false;
		if (abs(displacement.x) < abs(displacement.y))
		{
			permute = true;

			displacement = displacement.yx;
			screenRayOri.xy = screenRayOri.yx;
			screenRayEnd.xy = screenRayEnd.yx;
		}
		float dir = sign(displacement.x);
		float invdx = dir / displacement.x;
		//float stride = 2.0 - min(1.0, -rayOri * 0.01);
		float stride = _rayMarchingStepSize;

		float2 dp = float2(dir, invdx * displacement.y) * stride;
		float3 dq = (QEnd - QOri) * invdx * stride;
		float  dk = (kEnd - kOri) * invdx * stride;
		float rayZmin = rayOri.z;
		float rayZmax = rayOri.z;
		float preZ = rayOri.z;

		float2 screenPoint = screenRayOri;
		float3 Q = QOri;
		float k = kOri;

		float2 offsetUV = (fmod(floor(screenRayOri), 4.0));
		float ditherValue = tex2D(_ditherMap, offsetUV / 4.0).a;

		screenPoint += dp * ditherValue;
		Q.z += dq.z * ditherValue;
		k += dk * ditherValue;

		UNITY_LOOP
			for (int i = 0; i < _maxRayMarchingStep; i++)
			{
				//向前步进一个单位
				screenPoint += dp;
				Q.z += dq.z;
				k += dk;

				//得到步进前后两点的深度
				rayZmin = preZ;
				rayZmax = (dq.z * 0.5 + Q.z) / (dk * 0.5 + k);
				preZ = rayZmax;
				if (rayZmin > rayZmax)
				{
					swap(rayZmin, rayZmax);
				}

				//得到当前屏幕空间位置，交换过的xy换回来，并且根据像素宽度还原回（0,1）区间而不是屏幕区间
				hitScreenPos = permute ? screenPoint.yx : screenPoint;
				hitScreenPos *= _ScreenSize.xy;

				//转换回屏幕（0,1）区间，剔除出屏幕的反射
				if (any(hitScreenPos.xy < 0.0) || any(hitScreenPos.xy > 1.0))
					return false;

				//采样当前点深度图，转化为视空间的深度（负值）
				float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, hitScreenPos);
				depth = -LinearEyeDepth(depth);

				bool isBehand = (rayZmin <= depth);
				bool intersecting = isBehand && (rayZmax >= depth - _depthThickness);

				if (intersecting)
					return true;
				//if (isBehand) return false;
			}
		return false;
	}

	v2f vert_raymarching(appdata v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = TRANSFORM_TEX(v.uv, _MainTex);

		float4 clipPos = float4(v.uv * 2 - 1.0, 1.0, 1.0);
		float4 viewRay = mul(_InverseProjectionMatrix, clipPos);
		o.viewRay = viewRay.xyz / viewRay.w;
		return o;
	}

	fixed4 frag_raymarching(v2f i) : SV_Target
	{
		float4 z = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
		float linear01Depth = Linear01Depth(z);

		float3 worldNormal = tex2D(_CameraGBufferTexture2, i.uv).rgb * 2.0 - 1.0;
		float3 viewNormal = mul((float3x3)(_WorldToCameraMatrix), worldNormal);

		//重建视空间坐标
		float3 viewPos = linear01Depth * i.viewRay;
		float3 viewDir = normalize(viewPos);
		viewNormal = normalize(viewNormal);
		//视空间方向反射方向
		float3 reflectDir = reflect(viewDir, viewNormal);
		float2 hitScreenPos = float2(-1,-1);
		fixed4 reflection = fixed4(0,0,0,1);
		//fixed4 color = fixed4(tex2D(_MainTex, i.uv).rgb, 1);
		fixed4 sceneColor = fixed4(tex2D(_MainTex, i.uv).rgb, 1);
		fixed4 color = fixed4(0, 0, 0, 1);

		
		//从该点开始RayMarching
		reflection.a = 0;
		if (screenSpaceRayMarching(viewPos, reflectDir, hitScreenPos))
		{
			float4 reflectTex = tex2D(_MainTex, hitScreenPos);
			reflection.rgb = reflectTex.rgb;
			reflection.a = 1;
		}
		//float4 cubemap = GetCubeMap(i.uv);
		//color.rgb = max(1e-5, color.rgb - cubemap.rgb);
		//float s = tex2D(_CameraGBufferTexture1, i.uv).a;
		return (reflection);
	}


		//用于blur
		struct v2f_blur
	{
		float4 pos : SV_POSITION;
		float2 uv  : TEXCOORD0;
		float4 uv01 : TEXCOORD1;
		float4 uv23 : TEXCOORD2;
		float4 uv45 : TEXCOORD3;
	};

	//用于叠加
	struct v2f_add
	{
		float4 pos : SV_POSITION;
		float2 uv  : TEXCOORD0;
		float2 uv1 : TEXCOORD1;
	};

	float4 _MainTex_TexelSize;
	float4 _offsets;

	//高斯模糊 vert shader
	v2f_blur vert_blur(appdata_img v)
	{
		v2f_blur o;
		_offsets *= _MainTex_TexelSize.xyxy;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = v.texcoord.xy;

		o.uv01 = v.texcoord.xyxy + _offsets.xyxy * float4(1, 1, -1, -1);
		o.uv23 = v.texcoord.xyxy + _offsets.xyxy * float4(1, 1, -1, -1) * 2.0;
		o.uv45 = v.texcoord.xyxy + _offsets.xyxy * float4(1, 1, -1, -1) * 3.0;

		return o;
	}

	//高斯模糊 pixel shader
	fixed4 frag_blur(v2f_blur i) : SV_Target
	{
		fixed4 color = fixed4(0,0,0,0);
		color += 0.40 * tex2D(_MainTex, i.uv);
		color += 0.15 * tex2D(_MainTex, i.uv01.xy);
		color += 0.15 * tex2D(_MainTex, i.uv01.zw);
		color += 0.10 * tex2D(_MainTex, i.uv23.xy);
		color += 0.10 * tex2D(_MainTex, i.uv23.zw);
		color += 0.05 * tex2D(_MainTex, i.uv45.xy);
		color += 0.05 * tex2D(_MainTex, i.uv45.zw);
		return color;
	}

	sampler2D _ReflectTex;

	fixed4 frag_add(v2f_add i) : SV_Target
	{
		fixed4 sceneColor = tex2D(_MainTex, i.uv);
		//fixed4 sceneColor = fixed4(0,0,0,1);

	float s = tex2D(_CameraGBufferTexture1, i.uv).a;
		fixed4 reflect = tex2D(_ReflectTex, i.uv) * s;

		//float mask = reflect * s;
		float4 cubemap = GetCubeMap(i.uv);

		//sceneColor.rgb = max(1e-5, (sceneColor.rgb - cubemap.rgb));

		return sceneColor * (1-reflect.a) + reflect;// *s;
	}


		ENDCG

		SubShader
	{
		//Pass0 : RayMarching
		Pass
		{
			ZTest Off
			Cull Off
			ZWrite Off
			Fog{ Mode Off }
			CGPROGRAM
			#pragma vertex vert_raymarching
			#pragma fragment frag_raymarching
			ENDCG

		}

			//Pass1 : Blur
			Pass
		{
			ZTest Off
			Cull Off
			ZWrite Off
			Fog{ Mode Off }

			CGPROGRAM
			#pragma vertex vert_blur
			#pragma fragment frag_blur
			ENDCG
		}
			
			//pass 1: Add
			Pass
		{

			ZTest Off
			Cull Off
			ZWrite Off
			Fog{ Mode Off }

			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag_add
			ENDCG
		}

	}
}