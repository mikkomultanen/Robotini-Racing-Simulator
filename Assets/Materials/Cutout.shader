// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Effects/Cutout"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
        _Cutoff ("Cutoff", Range(0,1)) = 0.5
        _EdgeRadius ("Edge Radius", Float) = 0.2
	}
	SubShader
	{
		// No culling or depth// inside SubShader
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" }

		Pass
		{
            Cull Off 
            ZTest Always
            ZWrite Off 
            Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
                float4 color : COLOR;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
                float4 color : COLOR;
			};
            
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
                o.color = v.color;
				return o;
			}
			
			sampler2D _MainTex;

            float _Cutoff;
            float _EdgeRadius;


			fixed4 frag (v2f i) : SV_Target
			{
                 fixed4 col = tex2D(_MainTex, i.uv);

                 _Cutoff = (_Cutoff - _EdgeRadius) * _Cutoff;

                 if (col.a < _Cutoff - _EdgeRadius)
                    col.a = 0;
                 else if (col.a >= _EdgeRadius && col.a < _Cutoff + _EdgeRadius)
                    col.a = (col.a - (_Cutoff - _EdgeRadius)) / (_EdgeRadius*2);
                 else
                    col.a = 1;

                 return float4(i.color.rgb, col.a);
			}
			ENDCG
		}
	}
}
