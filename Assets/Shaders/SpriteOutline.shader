Shader "Custom/SpriteOutlineAndGlow"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1, 0, 0, 1)
        _OutlineThickness ("Outline Thickness", Range(0, 0.1)) = 0.01
        _GlowColor ("Glow Color", Color) = (1, 1, 0, 0.5) // Yellow glow
        _GlowIntensity ("Glow Intensity", Range(0, 1)) = 0.2
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float4 _OutlineColor;
            float _OutlineThickness;
            float4 _GlowColor;
            float _GlowIntensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                if (col.a > 0.5) return col;

                fixed4 outline = fixed4(0, 0, 0, 0);
                fixed4 glow = fixed4(0, 0, 0, 0);
                float2 offsets[4] = {
                    float2(-_OutlineThickness, 0),
                    float2(_OutlineThickness, 0),
                    float2(0, -_OutlineThickness),
                    float2(0, _OutlineThickness)
                };

                for (int j = 0; j < 4; j++)
                {
                    fixed4 neighbor = tex2D(_MainTex, i.uv + offsets[j]);
                    if (neighbor.a > 0.5)
                    {
                        outline = _OutlineColor;
                        glow = _GlowColor * _GlowIntensity;
                        break;
                    }
                }

                return outline.a > 0 ? outline : glow;
            }
            ENDCG
        }
    }
}