// Pinta um sprite de uma cor so (a _Cor), mantendo o recorte do desenho: e o branco do golpe
// (PiscarAoTomarDano). Usa o vertice dos sprites da Unity, entao o espelhar (flipX) continua valendo.
Shader "Jogo/Silhueta"
{
    Properties
    {
        [PerRendererData] _MainTex ("Desenho", 2D) = "white" {}
        _Cor ("Cor", Color) = (1, 1, 1, 1)
        [HideInInspector] _Color ("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1, 1, 1, 1)
        [HideInInspector] _Flip ("Flip", Vector) = (1, 1, 1, 1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment Frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            fixed4 _Cor;

            fixed4 Frag(v2f IN) : SV_Target
            {
                fixed alfa = SampleSpriteTexture(IN.texcoord).a * IN.color.a * _Cor.a;
                return fixed4(_Cor.rgb * alfa, alfa);
            }
            ENDCG
        }
    }
}
