Shader "Skybox/Night Sky"
{
    Properties
    {
        [Header(Sky)]
        _ZenithColor ("Zenith Color", Color) = (0.01, 0.015, 0.04, 1)
        _HorizonColor ("Horizon Color", Color) = (0.06, 0.08, 0.14, 1)
        _GroundColor ("Ground Color", Color) = (0.02, 0.02, 0.025, 1)
        _GradientExponent ("Gradient Exponent", Range(0.1, 4)) = 0.5

        [Header(Stars)]
        _StarDensity ("Star Density", Range(0.25, 3)) = 1
        _StarBrightness ("Star Brightness", Range(0, 10)) = 1.5
        _StarColorVariation ("Star Color Variation", Range(0, 1)) = 0.5
        _TwinkleSpeed ("Twinkle Speed", Range(0, 10)) = 3
        _TwinkleAmount ("Twinkle Amount", Range(0, 1)) = 0.3

        [Header(Light Pollution)]
        _LightPollution ("Light Pollution", Range(0, 1)) = 0
        _PollutionColor ("Pollution Glow Color", Color) = (1, 0.55, 0.25, 1)
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ZenithColor;
                float4 _HorizonColor;
                float4 _GroundColor;
                float _GradientExponent;
                float _StarDensity;
                float _StarBrightness;
                float _StarColorVariation;
                float _TwinkleSpeed;
                float _TwinkleAmount;
                float _LightPollution;
                float4 _PollutionColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 Hash43(float3 p)
            {
                float4 p4 = frac(p.xyzx * float4(0.1031, 0.1030, 0.0973, 0.1099));
                p4 += dot(p4, p4.wzxy + 33.33);
                return frac((p4.xxyz + p4.yzzw) * p4.zywx);
            }

            float2 CubeFaceUV(float3 dir, out float face)
            {
                float3 a = abs(dir);
                float2 uv;
                if (a.x >= a.y && a.x >= a.z) { face = dir.x > 0 ? 0 : 1; uv = dir.zy / a.x; }
                else if (a.y >= a.z)          { face = dir.y > 0 ? 2 : 3; uv = dir.xz / a.y; }
                else                          { face = dir.z > 0 ? 4 : 5; uv = dir.xy / a.z; }
                return atan(uv) * (2.0 / PI) + 0.5;
            }

            float3 StarLayer(float3 dir, float cellsPerFace, float probability, float maxBrightness, float pixelAngle)
            {
                float cells = floor(cellsPerFace * _StarDensity);
                float face;
                float2 uv = CubeFaceUV(dir, face) * cells;
                float2 cell = floor(uv);
                float seed = face * 1000 + cells;

                float4 rnd = Hash43(float3(cell, seed));
                if (rnd.w > probability)
                    return 0;
                float4 rnd2 = Hash43(float3(cell, seed + 500));

                float brightness = max(maxBrightness * rnd.z * rnd.z * rnd.z - _LightPollution, 0);
                float twinkle = 1 - _TwinkleAmount * (0.5 + 0.5 * sin(_Time.y * _TwinkleSpeed * (0.5 + rnd2.x) + rnd2.y * TWO_PI));
                float pixel = pixelAngle * cells / (0.5 * PI);
                float radius = min(pixel * (1 + brightness), 0.2);
                float2 starPos = cell + 0.2 + rnd.xy * 0.6;
                float d = length(uv - starPos) / radius;

                float3 tint = lerp(float3(1.0, 0.75, 0.55), float3(0.7, 0.82, 1.0), rnd2.z);
                tint = lerp(float3(1, 1, 1), tint, _StarColorVariation);

                return tint * brightness * twinkle * exp(-3 * d * d);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.direction);
                float height = dir.y;

                float3 sky = lerp(_HorizonColor.rgb, _ZenithColor.rgb, pow(saturate(height), _GradientExponent));

                float glow = exp(-saturate(height) * 5) * 0.5 + 0.08;
                sky += _PollutionColor.rgb * _LightPollution * glow;

                float pixelAngle = max(length(ddx(dir)), length(ddy(dir)));
                float3 stars = StarLayer(dir, 200, 0.2, 0.4, pixelAngle)
                             + StarLayer(dir, 50, 0.3, 1.0, pixelAngle);
                sky += stars * _StarBrightness * smoothstep(0, 0.15, height);

                sky = lerp(sky, _GroundColor.rgb, 1 - smoothstep(-0.02, 0, height));
                return float4(sky, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
