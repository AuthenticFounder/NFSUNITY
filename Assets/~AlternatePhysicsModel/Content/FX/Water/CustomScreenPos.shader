    Shader "Custom/SurfaceScreenPos" {
        Properties {
            _FadeLength("Fade Length", Range(0, 2)) = 2.0
        }
        SubShader {
            Tags { "Queue"="Transparent" "RenderType"="Transparent" }
            LOD 200
     
            CGPROGRAM
            // Physically based Standard lighting model, and enable shadows on all light types
            #pragma surface surf Standard fullforwardshadows alpha:fade
     
            // Use shader model 3.0 target, to get nicer looking lighting
            #pragma target 3.0
     
            struct Input {
                float4 screenPos;
                float3 worldPos;
            };
     
            sampler2D _CameraDepthTexture;
            float _FadeLength;
     
            void surf (Input IN, inout SurfaceOutputStandard o) {
                float sceneZ = LinearEyeDepth (SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(IN.screenPos)));
                float surfZ = -mul(UNITY_MATRIX_V, float4(IN.worldPos.xyz, 1)).z;
     
                o.Emission = saturate((sceneZ - surfZ) / _FadeLength);
     
                o.Albedo = 0;
                o.Alpha = 1;
            }
            ENDCG
        }
        FallBack "Diffuse"
    }
