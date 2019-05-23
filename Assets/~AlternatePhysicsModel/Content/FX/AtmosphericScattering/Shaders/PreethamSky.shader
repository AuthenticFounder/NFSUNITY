// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// source: http://www.simonwallner.at/projects/atmospheric-scattering

Shader "Custom/Preetham_Sky" 
{
Properties {
	reileighCoefficient ("Rayleigh", Range(0.0,0.02)) = 0.01
	mieCoefficient ("Mie", Range(0.0,0.025)) = 0.001
	mieDirectionalG("Mie G", Range(0.0,0.999)) = 0.76
	turbidity("Turbidity", Range(0,20)) = 1.0
}
SubShader 
	{
		Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox"}
		Cull Off ZWrite Off  

		CGINCLUDE
		
		#define sunDirection _WorldSpaceLightPos0.xyz
		
		uniform float turbidity;
		uniform float reileighCoefficient ;//= reileigh;
		uniform float mieCoefficient ; // 0.011;
		uniform float mieDirectionalG; // 0.75;

		// constants for atmospheric scattering
		static const float e = 2.71828;
		static const float pi = 3.141592;

		static const float n = 1.0003;		// refractive index of air
		static const float N = 2.545E25;	// number of molecules per unit volume for air at
		                        	// 288.15K and 1013mb (sea level -45 celsius)
		static const float pn = 0.035;		// depolatization factor for standard air

		// wavelength of used primaries, according to preetham
		static const float3 lambda = float3(680E-9, 550E-9, 450E-9);

		// mie stuff
		// K coefficient for the primaries
		static const float3 K = float3(0.686, 0.678, 0.666);
		static const float v = 4.0;

		// optical length at zenith for molecules
		static const float rayleighZenithLength = 8.4E3;
		static const float mieZenithLength = 1.25E3;
//		static const float3 up = float3(0.0, 1.0, 0.0);

		static const float E = 1000.0;
		static const float sunAngularDiameterCos = 0.9999;

		// earth shadow hack
		static const float cutoffAngle =   pi/2.0;
		static const float steepness = 0.5;

		float3 totalRayleigh(float3 lambda)
		{
//		    return (8.0 * pow(pi, 3.0) * pow(pow(n, 2.0) - 1.0, 2.0) * (6.0 + 3.0 * pn)) / (3.0 * N * pow(lambda, 4.0) * (6.0 - 7.0 * pn));

		   // HACK: above formula is correct, however PC can not get the final value, but it works fine on Mac
		   return float3(5.8E-6,13.5E-6,33.1E-6);
		}

		float rayleighPhase(float cosTheta)
		{
			/**
			 * NOTE: There are a few scale factors for the phase funtion
			 * (1) as given bei Preetham, normalized over the sphere with 4pi sr
			 * (2) normalized to integral = 1
			 * (3) nasa: integrates to 9pi / 4, looks best
			 */
			 
//			return (3.0 / (16.0*pi)) * (1.0 + pow(cosTheta, 2));
//			return (1.0 / (3.0*pi)) * (1.0 + pow(cosTheta, 2));
			return (3.0 / 4.0) * (1.0 + pow(cosTheta, 2));
		}

		float3 totalMie(float3 lambda, float3 K, float T)
		{
		    float c = (0.2 * T ) * 10E-18;
		    return 0.434 * c * pi * pow((2.0 * pi) / lambda, (v - 2.0)) * K;
		}

		float hgPhase(float cosTheta, float g)
		{
		    return (1.0 / (4.0*pi)) * ((1.0 - pow(g, 2.0)) / pow(1.0 - 2.0*g*cosTheta + pow(g, 2.0), 1.5));
		}

		float sunIntensity(float zenithAngleCos)
		{
		    return E * max(0.0, 1.0 - exp(-((cutoffAngle - acos(zenithAngleCos))/steepness)));
		}

		struct appdata_t {
			float4 vertex 		: POSITION;
		};
		
		struct v2f 
		{
			float4	pos 		: SV_POSITION;
			float3	worldPos 	: TEXCOORD0;
			float3	bR			: TEXCOORD1;
			float3	bM			: TEXCOORD2;

		};
		
		v2f vert(appdata_t v)
		{
			v2f OUT;
			OUT.pos = UnityObjectToClipPos(v.vertex);
			OUT.worldPos = normalize(mul((float3x3)unity_ObjectToWorld, v.vertex.xyz));

			OUT.bR = totalRayleigh(lambda) * reileighCoefficient;
			OUT.bM = totalMie(lambda, K, turbidity) * mieCoefficient;	

			return OUT;
		}
			
		float4 frag(v2f IN) : SV_Target
		{
		    float3 wPos = normalize( IN.worldPos );

		    float sunE = sunIntensity(sunDirection.y);

		    // extinction (absorbtion + out scattering)
		    // rayleigh coefficients
		    float3 betaR = IN.bR; // code moved to vertex

		    // mie coefficients
		    float3 betaM = IN.bM; // code moved to vertex

		    // optical length
		    // cutoff angle at 90 to avoid singularity in next formula.
		    float zenithAngle = acos(max(0.0, wPos.y));
		    float sR = rayleighZenithLength / (cos(zenithAngle) + 0.15 * pow(93.885 - ((zenithAngle * 180.0) / pi), -1.253));
		    float sM = mieZenithLength / (cos(zenithAngle) + 0.15 * pow(93.885 - ((zenithAngle * 180.0) / pi), -1.253));
					
		    // combined extinction factor    
		    float3 Fex = exp(-(betaR * sR + betaM * sM));

			// in scattering
		    float cosTheta = dot(wPos, sunDirection);

		    float rPhase = rayleighPhase(cosTheta);
		    float3 betaRTheta = betaR * rPhase;

		    float mPhase = hgPhase(cosTheta, mieDirectionalG);
		    float3 betaMTheta = betaM * mPhase;

		    float3 Lin = sunE * ((betaRTheta + betaMTheta) / (betaR + betaM)) * (1.0 - Fex);

			// composition + solar disc
			if (cosTheta > sunAngularDiameterCos)
				Lin += sunE * Fex;
// --------------------------------------------------------------------------------		

			// tonemapping
 			Lin = 1 - exp( -Lin );	

			// color correction
//			Lin = pow(Lin , 1/2.2); // for Gamma space
			
			return float4(Lin, 1); 

		}

		ENDCG
// --------------------------------------------------------------------------------			
	Pass{	
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			ENDCG
    	}
	}
	
	Fallback Off
}