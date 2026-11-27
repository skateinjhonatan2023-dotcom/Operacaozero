Shader "Operation Zero/Coastal Ground"
{
    Properties
    {
        _BaseMap("Grass", 2D) = "white" {}
        _SoilMap("Soil", 2D) = "white" {}
        [Normal] _GroundNormal("Ground normal", 2D) = "bump" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_SoilMap); SAMPLER(sampler_SoilMap);
            TEXTURE2D(_GroundNormal); SAMPLER(sampler_GroundNormal);
            struct Input { float4 pos:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; half4 paint:COLOR; };
            struct Output { float4 pos:SV_POSITION; float3 world:TEXCOORD0; half3 normal:TEXCOORD1; float2 uv:TEXCOORD2; half3 paintFog:TEXCOORD3; };
            Output Vert(Input v)
            {
                Output o;
                o.world=TransformObjectToWorld(v.pos.xyz);
                o.pos=TransformWorldToHClip(o.world);
                o.normal=TransformObjectToWorldNormal(v.normal);
                o.uv=v.uv;
                o.paintFog=half3(v.paint.rg,ComputeFogFactor(o.pos.z));
                return o;
            }
            half4 Frag(Output i):SV_Target
            {
                half3 grass=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb;
                half3 soil=SAMPLE_TEXTURE2D(_SoilMap,sampler_SoilMap,i.uv*.78).rgb*half3(.73,.64,.47);
                half3 albedo=lerp(grass,soil,i.paintFog.x)*lerp(.82,1.10,i.paintFog.y);
                half3 n=normalize(i.normal);
                half3 tangent=normalize(half3(n.y,-n.x,0));
                half3 detail=UnpackNormal(SAMPLE_TEXTURE2D(_GroundNormal,sampler_GroundNormal,i.uv));
                n=normalize(n+.32*(tangent*detail.x+cross(tangent,n)*detail.y));
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    float4 shadowCoord=ComputeScreenPos(TransformWorldToHClip(i.world));
                #else
                    float4 shadowCoord=TransformWorldToShadowCoord(i.world);
                #endif
                Light sun=GetMainLight(shadowCoord);
                half3 lighting=SampleSH(n)+sun.color*(saturate(dot(n,sun.direction))*sun.shadowAttenuation);
                return half4(MixFog(albedo*lighting,i.paintFog.z),1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
    Fallback "Universal Render Pipeline/Lit"
}
