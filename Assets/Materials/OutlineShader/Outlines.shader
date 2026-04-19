Shader "Custom/Outlines" {
    Properties {
        _Thickness ("Outline Thickness px", Range(0, 20)) = 1
        _Color("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineMaxDistance ("Outline Max Distance", Float) = 40
    }
    SubShader {
        Tags {
            "RenderType"="Transparent" "Queue" = "Transparent+1"
        }
        Cull Back
        Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // Blit.hlsl will handle the Vert(vertex) shader for us and provides "Varings" structure Input to Fragment shader
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            // === NEW: outline mask texture from the renderer feature ===
            TEXTURE2D(_OutlineMaskTex);
            SAMPLER(sampler_OutlineMaskTex);

            CBUFFER_START(UnityPerMaterial)
            half _Thickness;
            half4 _Color;
            float _OutlineMaxDistance;
            CBUFFER_END

            struct ScharrKernels { float3x3 x, y; };

            //=============================================
            //      Scharr X    ||        Scharr Y        ||
            //    -3,  0,  3    ||      -3, -10, -3       ||
            //    -10, 0, 10    ||       0,   0,  0       ||
            //    -3,  0,  3    ||       3,  10,  3       ||
            //=============================================
            ScharrKernels GetEdgeDetectKernels() {
                ScharrKernels kernels;
                kernels.x = float3x3(-3, -10, -3, 0, 0, 0, 3, 10, 3);
                kernels.y = float3x3(-3, 0, 3, -10, 0, 10, -3, 0, 3);
                return kernels;
            }

            float DepthBasedOutlines(float2 uv, float2 px) {
                ScharrKernels kernels = GetEdgeDetectKernels();
                float gx = 0;
                float gy = 0;
                for(int i = -1; i <= 1; i++) {
                    for(int j = -1; j <= 1; j++) {
                        if (i == 0 && j == 0) continue;
                        float2 offset = float2(i, j) * px;
                        float d = SampleSceneDepth(uv + offset);
                        d = LinearEyeDepth(d, _ZBufferParams); // eye-space depth
                        gx += d * kernels.x[i+1][j+1];
                        gy += d * kernels.y[i+1][j+1];
                    }
                }
                float g = sqrt(gx * gx + gy * gy);
                float outline = step(10, g);
                return outline;
            }

            float NormalBasedOutlines(float2 uv, float2 px) {
                ScharrKernels kernels = GetEdgeDetectKernels();
                float gx = 0;
                float gy = 0;
                float3 cn = SampleSceneNormals(uv);
                for(int i = -1; i <= 1; i++) {
                    for(int j = -1; j <= 1; j++) {
                        if (i == 0 && j == 0) continue;
                        float2 offset = float2(i, j) * px;
                        float3 n = SampleSceneNormals(uv + offset);
                        float d = dot(n, cn);
                        gx += d * kernels.x[i+1][j+1];
                        gy += d * kernels.y[i+1][j+1];
                    }
                }
                float g = sqrt(gx * gx + gy * gy);
                float outline = step(2, g);
                return outline;
            }
            
            half4 frag(Varyings In) : SV_Target
            {
                float2 px = _Thickness / _ScreenSize.xy;

                float outline = NormalBasedOutlines(In.texcoord, px);
                outline += (1.0 - outline) * DepthBasedOutlines(In.texcoord, px);

                // sample mask from the feature
                float mask = SAMPLE_TEXTURE2D(_OutlineMaskTex, sampler_OutlineMaskTex, In.texcoord).r;
                outline *= mask;

                // distance fade
                float rawDepth = SampleSceneDepth(In.texcoord);
                float eyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float fade     = saturate(1.0 - eyeDepth / _OutlineMaxDistance);

                return outline * fade * _Color;
            }
            ENDHLSL
        }
    }
}
