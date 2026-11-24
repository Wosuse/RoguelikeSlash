Shader "RoguelikeSlash/BladeOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0.025,0.035,0.06,1)
        _OutlinePixels ("Outline Pixels", Range(0,6)) = 1.5
        _AlphaThreshold ("Alpha Threshold", Range(0.01,0.9)) = 0.2
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="False" }
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment BladeFrag
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            float4 _MainTex_TexelSize;
            fixed4 _OutlineColor;
            float _OutlinePixels, _AlphaThreshold;
            float Coverage(float2 uv)
            {
                return smoothstep(_AlphaThreshold, _AlphaThreshold + .035, SampleSpriteTexture(uv).a);
            }
            fixed4 BladeFrag(v2f IN) : SV_Target
            {
                float2 stepUV = _MainTex_TexelSize.xy * _OutlinePixels;
                float fill = Coverage(IN.texcoord);
                float outer = fill;
                outer=max(outer,Coverage(IN.texcoord+float2(stepUV.x,0)));
                outer=max(outer,Coverage(IN.texcoord-float2(stepUV.x,0)));
                outer=max(outer,Coverage(IN.texcoord+float2(0,stepUV.y)));
                outer=max(outer,Coverage(IN.texcoord-float2(0,stepUV.y)));
                outer=max(outer,Coverage(IN.texcoord+stepUV*.7071));
                outer=max(outer,Coverage(IN.texcoord-stepUV*.7071));
                outer=max(outer,Coverage(IN.texcoord+float2(stepUV.x,-stepUV.y)*.7071));
                outer=max(outer,Coverage(IN.texcoord+float2(-stepUV.x,stepUV.y)*.7071));
                float border=max(0,outer-fill)*_OutlineColor.a;
                fixed4 tex=SampleSpriteTexture(IN.texcoord);
                fixed4 result;
                result.a=(fill+border)*IN.color.a;
                result.rgb=(tex.rgb*IN.color.rgb*fill+_OutlineColor.rgb*border)*IN.color.a;
                return result;
            }
            ENDCG
        }
    }
}
