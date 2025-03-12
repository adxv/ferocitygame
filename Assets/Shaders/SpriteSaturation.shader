Shader "Custom/SpriteSaturation"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Saturation ("Saturation", Range(0, 2)) = 1.0 // 0 = grayscale, 1 = normal, 2 = oversaturated
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

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Saturation;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the sprite’s color
                fixed4 col = tex2D(_MainTex, i.uv);

                // Convert to grayscale (luminance)
                float gray = dot(col.rgb, float3(0.299, 0.587, 0.114));

                // Lerp between grayscale and original color based on saturation
                fixed3 saturatedColor = lerp(fixed3(gray, gray, gray), col.rgb, _Saturation);

                return fixed4(saturatedColor, col.a);
            }
            ENDCG
        }
    }
}