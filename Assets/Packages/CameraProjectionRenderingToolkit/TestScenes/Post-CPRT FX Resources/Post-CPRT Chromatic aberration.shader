Shader "Hidden/Post-CPRT Noise"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		Bias ("Bias", Range(-0.02, 0.02)) = 0.002
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

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

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;
			float Bias;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed2 ga=tex2D(_MainTex,i.uv).yw;
				fixed r=tex2D(_MainTex,i.uv+float2(-Bias,0.0f)).x;
				fixed b=tex2D(_MainTex,i.uv+float2(Bias,0.0f)).z;

				return fixed4(r,ga.x,b,ga.y);
			}
			ENDCG
		}
	}
}
